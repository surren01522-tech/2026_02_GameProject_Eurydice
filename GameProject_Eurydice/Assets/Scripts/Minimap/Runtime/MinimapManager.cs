using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private string resourcesMapsFolder = "Maps";
    [SerializeField] private MapMarkerStyleSet defaultStyleSet;

    [Header("Fog Reveal")]
    [SerializeField] private float revealInterval = 0.1f;
    [SerializeField] private float revealMoveThreshold = 0.75f;
    [SerializeField] private float fogSaveInterval = 2.0f;

    public MapRegionData ActiveRegion { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public MapMarkerStyleSet DefaultStyleSet => defaultStyleSet;
    public IReadOnlyList<MapRegionData> AllRegions => regionCatalog;

    public event Action<MapRegionData> OnActiveRegionChanged;
    public event Action<string> OnFogUpdated;
    public event Action OnMapStateReset;

    private readonly List<MapRegionData> regionCatalog = new();
    private readonly List<MapRegionZone> activeZones = new();
    private readonly Dictionary<(string mapId, string tileId), MinimapFog> fogCache = new();

    private float nextRevealTime;
    private float fogFlushDeadline = -1f;
    private Vector3 lastRevealPos;
    private bool hasLastRevealPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (defaultStyleSet == null)
            Debug.LogError("[MinimapManager] defaultStyleSet is not assigned.", this);

        LoadRegionCatalog();

        SceneEnvironmentManager.OnActiveSceneChanged += HandleEnvSceneChanged;
        SceneManager.activeSceneChanged += HandleUnityActiveSceneChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SaveManager.OnBeforeFlush += HandleBeforeSaveFlush;
        SaveManager.OnSaveDataDeleted += HandleSaveDataDeleted;
    }

    private void Start()
    {
        AcquirePlayer();
        RefreshActiveRegion();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneEnvironmentManager.OnActiveSceneChanged -= HandleEnvSceneChanged;
        SceneManager.activeSceneChanged -= HandleUnityActiveSceneChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SaveManager.OnBeforeFlush -= HandleBeforeSaveFlush;
        SaveManager.OnSaveDataDeleted -= HandleSaveDataDeleted;

        FlushDirtyFogs(deferSave: false, flushDisk: true);
        DisposeAllFogs();
        Instance = null;
    }

    private void Update()
    {
        if (PlayerTransform != null && ActiveRegion != null && Time.unscaledTime >= nextRevealTime)
        {
            nextRevealTime = Time.unscaledTime + Mathf.Max(0.02f, revealInterval);
            RevealAtWorldPosition(PlayerTransform.position);
        }

        if (fogFlushDeadline > 0f && Time.unscaledTime >= fogFlushDeadline)
        {
            fogFlushDeadline = -1f;
            FlushDirtyFogs(deferSave: true, flushDisk: false);
        }
    }

    public void EnterZone(MapRegionZone zone)
    {
        if (zone == null || activeZones.Contains(zone)) return;
        activeZones.Add(zone);
        RefreshActiveRegion();
    }

    public void ExitZone(MapRegionZone zone)
    {
        if (zone != null && activeZones.Remove(zone))
            RefreshActiveRegion();
    }

    public MapRegionData FindRegionById(string mapId)
    {
        if (string.IsNullOrEmpty(mapId)) return null;
        return regionCatalog.Find(r => r != null && r.mapId == mapId);
    }

    public List<MapRegionData> GetDiscoveredRegions()
    {
        var result = new List<MapRegionData>();
        foreach (MapRegionData r in regionCatalog)
        {
            if (r != null && (r == ActiveRegion || SaveManager.IsMapRegionDiscovered(r.mapId)))
                result.Add(r);
        }
        return result;
    }

    public Texture2D GetTileFogTexture(MapRegionData region, MapTileData tile)
    {
        return GetOrLoadTileFog(region, tile, createIfMissing: true)?.Texture;
    }

    public bool IsWorldPositionRevealed(MapRegionData region, Vector3 worldPos)
    {
        if (region == null || region.tiles == null) return false;
        Vector2 xz = new Vector2(worldPos.x, worldPos.z);

        foreach (MapTileData tile in region.tiles)
        {
            if (tile == null || !tile.IsValid) continue;
            Vector2 min = tile.WorldMin;
            Vector2 max = tile.WorldMax;
            if (xz.x < min.x || xz.x > max.x || xz.y < min.y || xz.y > max.y) continue;

            MinimapFog fog = GetOrLoadTileFog(region, tile, createIfMissing: false);
            if (fog != null && fog.IsWorldRevealed(min, tile.worldSize, xz))
                return true;
        }
        return false;
    }

    public void RevealAtWorldPosition(Vector3 worldPos, bool force = false)
    {
        if (ActiveRegion == null || !ActiveRegion.HasValidTiles) return;

        if (!force && hasLastRevealPos)
        {
            Vector3 delta = worldPos - lastRevealPos;
            delta.y = 0f;
            if (delta.sqrMagnitude < revealMoveThreshold * revealMoveThreshold) return;
        }

        lastRevealPos = worldPos;
        hasLastRevealPos = true;

        Vector2 xz = new Vector2(worldPos.x, worldPos.z);
        bool anyChanged = false;

        foreach (MapTileData tile in ActiveRegion.tiles)
        {
            if (tile == null || !tile.IsValid) continue;
            MinimapFog fog = GetOrLoadTileFog(ActiveRegion, tile, createIfMissing: true);
            if (fog != null && fog.RevealWorld(tile.WorldMin, tile.worldSize, xz, ActiveRegion.revealRadius, ActiveRegion.innerRevealPercent))
                anyChanged = true;
        }

        if (!anyChanged) return;
        if (fogFlushDeadline < 0f)
            fogFlushDeadline = Time.unscaledTime + Mathf.Max(0.2f, fogSaveInterval);
        OnFogUpdated?.Invoke(ActiveRegion.mapId);
    }

    public void FlushDirtyFogs(bool deferSave = true) => FlushDirtyFogs(deferSave, !deferSave);

    private void FlushDirtyFogs(bool deferSave, bool flushDisk)
    {
        bool anyStaged = false;
        foreach (var kvp in fogCache)
        {
            MinimapFog fog = kvp.Value;
            if (fog == null || !fog.IsDirty) continue;
            SaveManager.SaveMapTileFog(kvp.Key.mapId, kvp.Key.tileId, fog.Width, fog.Height, fog.GetAlpha(), deferSave);
            fog.ClearDirty();
            anyStaged = true;
        }
        if (anyStaged && flushDisk)
            SaveManager.FlushSave();
    }

    public void RefreshActiveRegion()
    {
        MapRegionData resolved = ResolveCurrentRegion();
        if (resolved == ActiveRegion) return;

        if (ActiveRegion != null)
            FlushDirtyFogs(deferSave: true, flushDisk: false);

        ActiveRegion = resolved;
        hasLastRevealPos = false;

        if (ActiveRegion != null)
        {
            if (!ActiveRegion.HasValidTiles)
                Debug.LogError($"[MinimapManager] Region '{ActiveRegion.mapId}' has invalid bounds or no tiles configured.", ActiveRegion);

            SaveManager.MarkMapRegionDiscovered(ActiveRegion.mapId, deferSave: true);
            AcquirePlayer();
            if (PlayerTransform != null)
                RevealAtWorldPosition(PlayerTransform.position, force: true);
        }

        OnActiveRegionChanged?.Invoke(ActiveRegion);
    }

    private void LoadRegionCatalog()
    {
        regionCatalog.Clear();
        MapRegionData[] loaded = Resources.LoadAll<MapRegionData>(resourcesMapsFolder);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"[MinimapManager] No MapRegionData assets found in Resources/{resourcesMapsFolder}.", this);
            return;
        }

        foreach (MapRegionData r in loaded)
        {
            if (r == null) continue;
            if (FindRegionById(r.mapId) != null)
            {
                Debug.LogError($"[MinimapManager] Duplicate mapId '{r.mapId}'.", r);
                continue;
            }
            regionCatalog.Add(r);
        }
    }

    private MapRegionData ResolveCurrentRegion()
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            MapRegionZone zone = activeZones[i];
            if (zone == null || string.IsNullOrEmpty(zone.MapId)) continue;
            MapRegionData zRegion = FindRegionById(zone.MapId);
            if (zRegion != null) return zRegion;
        }

        string envScene = SceneEnvironmentManager.ActiveSceneName;
        MapRegionData byEnv = string.IsNullOrEmpty(envScene) ? null : regionCatalog.Find(r => r.MatchesScene(envScene));
        if (byEnv != null) return byEnv;

        string activeName = SceneManager.GetActiveScene().name;
        return string.IsNullOrEmpty(activeName) ? null : regionCatalog.Find(r => r.MatchesScene(activeName));
    }

    private MinimapFog GetOrLoadTileFog(MapRegionData region, MapTileData tile, bool createIfMissing)
    {
        if (region == null || tile == null || !tile.IsValid) return null;

        var key = (region.mapId, tile.tileId);
        if (fogCache.TryGetValue(key, out MinimapFog cached) && cached != null)
            return cached;

        Vector2Int dims = region.GetTileFogDimensions(tile);
        if (dims.x <= 0 || dims.y <= 0) return null;

        byte[] savedAlpha = SaveManager.LoadMapTileFog(region.mapId, tile.tileId, dims.x, dims.y);
        if (savedAlpha == null && !createIfMissing) return null;

        MinimapFog fog = new MinimapFog(dims.x, dims.y);
        if (savedAlpha != null) fog.SetAlpha(savedAlpha);

        fogCache[key] = fog;
        return fog;
    }

    private void AcquirePlayer()
    {
        if (PlayerTransform != null) return;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) PlayerTransform = player.transform;
    }

    private void HandleEnvSceneChanged(string _) => RefreshActiveRegion();
    private void HandleUnityActiveSceneChanged(Scene _, Scene __) => RefreshActiveRegion();
    private void HandleBeforeSaveFlush() => FlushDirtyFogs(deferSave: false, flushDisk: false);

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        bool hadPlayer = PlayerTransform != null;
        AcquirePlayer();
        if (!hadPlayer && PlayerTransform != null && ActiveRegion != null)
            RevealAtWorldPosition(PlayerTransform.position, force: true);
        if (ActiveRegion == null)
            RefreshActiveRegion();
    }

    private void HandleSaveDataDeleted()
    {
        DisposeAllFogs();
        hasLastRevealPos = false;
        fogFlushDeadline = -1f;

        if (ActiveRegion != null)
        {
            SaveManager.MarkMapRegionDiscovered(ActiveRegion.mapId, deferSave: false);
            if (PlayerTransform != null)
                RevealAtWorldPosition(PlayerTransform.position, force: true);
        }

        OnMapStateReset?.Invoke();
    }

    private void DisposeAllFogs()
    {
        foreach (MinimapFog fog in fogCache.Values) fog?.Dispose();
        fogCache.Clear();
    }
}
