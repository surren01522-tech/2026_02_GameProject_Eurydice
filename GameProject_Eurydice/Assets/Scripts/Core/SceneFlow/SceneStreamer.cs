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

    [SerializeField] private List<string> alwaysLoadedScenes = new List<string>();
    [SerializeField] private float unloadDelay = 4f;

    private readonly HashSet<SceneStreamZone> activeZones = new HashSet<SceneStreamZone>();
    private readonly Dictionary<string, int> pinnedSceneCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, SceneInstance> loadedSceneInstances = new Dictionary<string, SceneInstance>();
    private readonly Dictionary<string, float> pendingUnloads = new Dictionary<string, float>();

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
        Application.backgroundLoadingPriority = ThreadPriority.Low;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        loadedSceneInstances.Clear();
        pinnedSceneCounts.Clear();
    }

    private IEnumerator Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        CharacterController cc = null;
        if (player != null && player.TryGetComponent<CharacterController>(out cc))
            cc.enabled = false;

        yield return null;

        if (player != null)
        {
            var zones = FindObjectsByType<SceneStreamZone>(FindObjectsSortMode.None);
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(player.transform.position))
                    activeZones.Add(zones[i]);
            }

            if (activeZones.Count == 0 && alwaysLoadedScenes.Count == 0)
                Debug.LogWarning("[SceneStreamer] Starting position is not within any zone: " + player.transform.position);
        }

        yield return UpdateStream(immediateUnload: true);
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        isReady = true;
        player?.EnableControl();
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
        var zones = FindObjectsByType<SceneStreamZone>(FindObjectsSortMode.None);
        bool changed = false;

        for (int i = 0; i < zones.Length; i++)
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

            var targetScenes = new HashSet<string>(alwaysLoadedScenes);
            foreach (var zone in activeZones)
            {
                if (zone == null) continue;
                var scenes = zone.Scenes;
                for (int i = 0; i < scenes.Count; i++)
                    targetScenes.Add(scenes[i]);
            }

            foreach (var kvp in pinnedSceneCounts)
            {
                if (kvp.Value > 0 && !string.IsNullOrEmpty(kvp.Key))
                    targetScenes.Add(kvp.Key);
            }

            foreach (var sceneName in targetScenes)
            {
                pendingUnloads.Remove(sceneName);

                if (IsSceneLoaded(sceneName)) continue;

                var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                    loadedSceneInstances[sceneName] = handle.Result;
                else
                    Debug.LogError("[SceneStreamer] Failed to load addressable scene: " + sceneName);
            }

            foreach (var sceneName in loadedSceneInstances.Keys)
            {
                if (!targetScenes.Contains(sceneName) && !pendingUnloads.ContainsKey(sceneName))
                    pendingUnloads[sceneName] = immediateUnload ? 0f : Time.time + unloadDelay;
            }

            var toUnload = new List<string>();
            foreach (var kvp in pendingUnloads)
            {
                if (Time.time >= kvp.Value) toUnload.Add(kvp.Key);
            }

            for (int i = 0; i < toUnload.Count; i++)
            {
                string sceneName = toUnload[i];
                pendingUnloads.Remove(sceneName);

                if (loadedSceneInstances.TryGetValue(sceneName, out var instance))
                {
                    loadedSceneInstances.Remove(sceneName);
                    yield return Addressables.UnloadSceneAsync(instance);
                }
            }

            if (pendingUnloads.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
                pendingUpdate = true;
            }
        }
        while (pendingUpdate);
        isUpdating = false;
    }
}
