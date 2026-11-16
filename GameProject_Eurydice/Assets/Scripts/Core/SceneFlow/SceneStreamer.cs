using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SceneStreamer : MonoBehaviour
{
    public static SceneStreamer Instance { get; private set; }
    public static bool IsReady => Instance == null || Instance.isReady;

    [SerializeField] private string defaultEntryScene = "";
    [SerializeField] private List<string> alwaysLoadedScenes = new List<string>();
    [SerializeField] private float unloadDelay = 4f;
    [SerializeField] private ThreadPriority streamingLoadPriority = ThreadPriority.BelowNormal;
    [SerializeField] private bool warnIfOutsideZone;

    private static readonly WaitForSeconds UnloadPollWait = new WaitForSeconds(0.5f);

    public string DefaultEntryScene => defaultEntryScene;
    public bool IsUpdating => isUpdating;
    private string currentAnchorScene;

    private readonly HashSet<SceneStreamZone> activeZones = new HashSet<SceneStreamZone>();
    private readonly Dictionary<string, int> pinnedSceneCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, SceneInstance> loadedSceneInstances = new Dictionary<string, SceneInstance>();
    private readonly Dictionary<string, float> pendingUnloads = new Dictionary<string, float>();

    private readonly HashSet<string> targetScenesBuffer = new HashSet<string>();
    private readonly List<string> toUnloadBuffer = new List<string>();
    private readonly List<(string sceneName, AsyncOperationHandle<SceneInstance> handle)> pendingLoadHandles = new List<(string, AsyncOperationHandle<SceneInstance>)>();

    private bool isReady;
    private bool isUpdating;
    private bool pendingUpdate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SceneEnvironmentManager.OnActiveSceneChanged += HandleActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneEnvironmentManager.OnActiveSceneChanged -= HandleActiveSceneChanged;
            Instance = null;
        }
        loadedSceneInstances.Clear();
        pinnedSceneCounts.Clear();
    }

    private IEnumerator Start()
    {
        Application.backgroundLoadingPriority = ThreadPriority.High;

        var player = FindFirstObjectByType<PlayerController>();
        CharacterController cc = null;
        if (player != null && player.TryGetComponent<CharacterController>(out cc))
            cc.enabled = false;

        var initHandle = Addressables.InitializeAsync();
        if (!initHandle.IsDone)
            yield return initHandle;
        else
            yield return null;

        string savedScene = SaveManager.GetLastActiveScene();
        if (!string.IsNullOrEmpty(defaultEntryScene))
        {
            string candidate = !string.IsNullOrEmpty(savedScene) && savedScene != gameObject.scene.name
                ? savedScene
                : defaultEntryScene;

            if (candidate != defaultEntryScene)
            {
                var locHandle = Addressables.LoadResourceLocationsAsync(candidate);
                if (!locHandle.IsDone) yield return locHandle;
                bool validLoc = locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result != null && locHandle.Result.Count > 0;
                Addressables.Release(locHandle);
                if (!validLoc)
                {
                    Debug.LogWarning($"[SceneStreamer] Saved scene '{candidate}' not found in Addressables; using '{defaultEntryScene}'.");
                    candidate = defaultEntryScene;
                }
            }

            currentAnchorScene = candidate;
        }

        Physics.SyncTransforms();

        if (player != null)
        {
            var zones = SceneStreamZone.AllZones;
            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone != null && zone.Contains(player.transform.position))
                    activeZones.Add(zone);
            }

            if (warnIfOutsideZone && activeZones.Count == 0 && alwaysLoadedScenes.Count == 0 && string.IsNullOrEmpty(currentAnchorScene))
                Debug.LogWarning("[SceneStreamer] Starting position is not within any zone: " + player.transform.position);
        }

        yield return UpdateStream(immediateUnload: true);

        if (!string.IsNullOrEmpty(currentAnchorScene) && IsSceneLoaded(currentAnchorScene))
            SceneEnvironmentManager.SwitchEnvironment(SceneManager.GetActiveScene().name, currentAnchorScene, saveScene: false);
        else if (!string.IsNullOrEmpty(savedScene) && IsSceneLoaded(savedScene))
            SceneEnvironmentManager.SwitchEnvironment(SceneManager.GetActiveScene().name, savedScene, saveScene: false);
        else
            SceneEnvironmentManager.InitializeCurrentScene(SceneManager.GetActiveScene().name);

        Physics.SyncTransforms();
        player?.RestorePlayerTransform();
        if (player != null)
        {
            RefreshZonesForPosition(player.transform.position);
            if (pendingUpdate)
                yield return UpdateStream(immediateUnload: true);
            player.EnsureGroundSafetyOnSpawn();
        }

        PortalManager.Prewarm();
        Application.backgroundLoadingPriority = streamingLoadPriority;

        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();

        isReady = true;
        player?.EnableControl();
    }

    private void HandleActiveSceneChanged(string newActiveScene)
    {
        if (!isReady || string.IsNullOrEmpty(defaultEntryScene)) return;
        if (string.IsNullOrEmpty(newActiveScene) || newActiveScene == gameObject.scene.name) return;
        if (currentAnchorScene == newActiveScene || !loadedSceneInstances.ContainsKey(newActiveScene)) return;

        currentAnchorScene = newActiveScene;
        RequestUpdate();
    }

    public void OnZoneEntered(SceneStreamZone zone)
    {
        if (activeZones.Add(zone)) RequestUpdate();
    }

    public void OnZoneExited(SceneStreamZone zone)
    {
        if (activeZones.Remove(zone)) RequestUpdate();
    }

    /// <summary>
    /// 포탈 개방 등으로 특정 서브 씬을 강제 로드 및 유지(Pin)하도록 요청합니다.
    /// </summary>
    public void PinScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        pinnedSceneCounts.TryGetValue(sceneName, out int count);
        pinnedSceneCounts[sceneName] = count + 1;
        pendingUnloads.Remove(sceneName);
        RequestUpdate();
    }

    /// <summary>
    /// 포탈 닫힘 등으로 특정 서브 씬의 강제 유지(Pin)를 해제합니다.
    /// </summary>
    public void UnpinScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!pinnedSceneCounts.TryGetValue(sceneName, out int count)) return;

        if (count <= 1)
            pinnedSceneCounts.Remove(sceneName);
        else
            pinnedSceneCounts[sceneName] = count - 1;

        RequestUpdate();
    }

    /// <summary>
    /// 해당 서브 씬이 현재 로드 완료된 상태인지 반환합니다.
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return true;
        if (loadedSceneInstances.ContainsKey(sceneName)) return true;

        Scene s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }

    /// <summary>
    /// 플레이어 순간이동 직후 새로운 월드 좌표를 기준으로 활성 스트리밍 존을 즉시 재계산합니다.
    /// </summary>
    public void RefreshZonesForPosition(Vector3 worldPos)
    {
        Physics.SyncTransforms();
        activeZones.RemoveWhere(z => z == null);

        var zones = SceneStreamZone.AllZones;
        bool changed = false;

        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            if (zone == null) continue;

            if (zone.Contains(worldPos))
            {
                if (activeZones.Add(zone)) changed = true;
            }
            else
            {
                if (activeZones.Remove(zone)) changed = true;
            }
        }

        if (changed)
            RequestUpdate();
    }

    private void RequestUpdate()
    {
        pendingUpdate = true;
        if (isReady && !isUpdating) StartCoroutine(UpdateStream(immediateUnload: false));
    }

    private IEnumerator UpdateStream(bool immediateUnload)
    {
        isUpdating = true;
        do
        {
            pendingUpdate = false;

            targetScenesBuffer.Clear();
            if (!string.IsNullOrEmpty(currentAnchorScene))
                targetScenesBuffer.Add(currentAnchorScene);

            for (int i = 0; i < alwaysLoadedScenes.Count; i++)
                targetScenesBuffer.Add(alwaysLoadedScenes[i]);

            foreach (var zone in activeZones)
            {
                if (zone == null) continue;
                var scenes = zone.Scenes;
                for (int i = 0; i < scenes.Count; i++)
                    targetScenesBuffer.Add(scenes[i]);
            }

            foreach (var kvp in pinnedSceneCounts)
            {
                if (kvp.Value > 0 && !string.IsNullOrEmpty(kvp.Key))
                    targetScenesBuffer.Add(kvp.Key);
            }

            pendingLoadHandles.Clear();
            foreach (var sceneName in targetScenesBuffer)
            {
                pendingUnloads.Remove(sceneName);

                if (IsSceneLoaded(sceneName)) continue;

                var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                pendingLoadHandles.Add((sceneName, handle));
            }

            for (int i = 0; i < pendingLoadHandles.Count; i++)
            {
                var (sceneName, handle) = pendingLoadHandles[i];
                if (!handle.IsDone)
                    yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                    loadedSceneInstances[sceneName] = handle.Result;
                else
                    Debug.LogError("[SceneStreamer] Failed to load addressable scene: " + sceneName);
            }
            pendingLoadHandles.Clear();

            foreach (var sceneName in loadedSceneInstances.Keys)
            {
                if (!targetScenesBuffer.Contains(sceneName) && !pendingUnloads.ContainsKey(sceneName))
                    pendingUnloads[sceneName] = immediateUnload ? 0f : Time.time + unloadDelay;
            }

            toUnloadBuffer.Clear();
            foreach (var kvp in pendingUnloads)
            {
                if (Time.time >= kvp.Value) toUnloadBuffer.Add(kvp.Key);
            }

            for (int i = 0; i < toUnloadBuffer.Count; i++)
            {
                string sceneName = toUnloadBuffer[i];
                pendingUnloads.Remove(sceneName);

                if (loadedSceneInstances.TryGetValue(sceneName, out var instance))
                {
                    loadedSceneInstances.Remove(sceneName);
                    yield return Addressables.UnloadSceneAsync(instance);
                }
            }
            toUnloadBuffer.Clear();

            if (pendingUnloads.Count > 0)
            {
                yield return UnloadPollWait;
                pendingUpdate = true;
            }
        }
        while (pendingUpdate);
        isUpdating = false;
    }
}
