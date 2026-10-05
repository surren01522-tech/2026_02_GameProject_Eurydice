using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStreamer : MonoBehaviour
{
    public static SceneStreamer Instance { get; private set; }
    public static bool IsReady => Instance == null || Instance.isReady;

    [SerializeField] private List<string> alwaysLoadedScenes = new List<string>();
    [SerializeField] private float unloadDelay = 4f;

    private readonly HashSet<SceneStreamZone> activeZones = new HashSet<SceneStreamZone>();
    private readonly HashSet<string> loadedScenes = new HashSet<string>();
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
                var scenes = zone.Scenes;
                for (int i = 0; i < scenes.Count; i++)
                    targetScenes.Add(scenes[i]);
            }

            foreach (var sceneName in targetScenes)
            {
                pendingUnloads.Remove(sceneName);

                if (string.IsNullOrEmpty(sceneName) || loadedScenes.Contains(sceneName)) continue;

                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    AsyncOperation op = null;
                    try
                    {
                        op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[SceneStreamer] Failed to load scene: " + sceneName + " -> " + e.Message);
                    }

                    if (op != null) yield return op;
                }

                loadedScenes.Add(sceneName);
            }

            foreach (var sceneName in loadedScenes)
            {
                if (!targetScenes.Contains(sceneName) && !pendingUnloads.ContainsKey(sceneName))
                {
                    pendingUnloads[sceneName] = immediateUnload ? 0f : Time.time + unloadDelay;
                }
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
                loadedScenes.Remove(sceneName);

                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                    yield return SceneManager.UnloadSceneAsync(sceneName);
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
