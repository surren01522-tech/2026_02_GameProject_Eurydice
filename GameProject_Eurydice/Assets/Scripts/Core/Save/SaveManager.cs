using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SettingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
    private static readonly string SaveDataPath = Path.Combine(Application.persistentDataPath, "savedata.json");

    private static SaveData currentSaveData;
    private static readonly Dictionary<string, SceneSaveData> sceneLookup = new Dictionary<string, SceneSaveData>();
    private static readonly object FileWriteLock = new object();

    private static bool isDirty;
    private static bool hooksRegistered;
    private static SaveFlushRunner flushRunner;
    private static Task activeWriteTask;

    private sealed class SaveFlushRunner : MonoBehaviour
    {
        private float remainingDelay = -1f;

        public void Schedule(float delay)
        {
            if (remainingDelay < 0f || delay < remainingDelay)
                remainingDelay = delay;
        }

        public void Cancel() => remainingDelay = -1f;

        private void Update()
        {
            if (remainingDelay < 0f) return;
            remainingDelay -= Time.unscaledDeltaTime;
            if (remainingDelay > 0f) return;

            if (activeWriteTask != null && !activeWriteTask.IsCompleted)
            {
                remainingDelay = 0.2f;
                return;
            }

            remainingDelay = -1f;
            FlushAsyncOnMainThread();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushSave();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) FlushSave();
        }
    }

    private static bool suppressProgressLoad;

    public static bool SuppressProgressLoad
    {
        get => suppressProgressLoad;
        set
        {
            if (suppressProgressLoad == value) return;
            suppressProgressLoad = value;
            currentSaveData = null;
            sceneLookup.Clear();
            isDirty = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        if (hooksRegistered)
            Application.quitting -= FlushSave;
        currentSaveData = null;
        sceneLookup.Clear();
        isDirty = false;
        suppressProgressLoad = false;
        hooksRegistered = false;
        flushRunner = null;
        activeWriteTask = null;
        OnBeforeFlush = null;
        OnSaveDataDeleted = null;
    }

    private static void EnsurePersistenceHooks()
    {
        if (!hooksRegistered)
        {
            hooksRegistered = true;
            Application.quitting += FlushSave;
        }

        if (flushRunner == null && Application.isPlaying)
        {
            var go = new GameObject("SaveManagerFlushRunner") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            flushRunner = go.AddComponent<SaveFlushRunner>();
        }
    }

    private static void RebuildSceneLookup(SaveData data)
    {
        sceneLookup.Clear();
        if (data?.scenes == null) return;

        for (int i = 0; i < data.scenes.Count; i++)
        {
            var s = data.scenes[i];
            if (s != null && !string.IsNullOrEmpty(s.sceneName))
                sceneLookup[s.sceneName] = s;
        }
    }

    public static bool HasSaveData() => File.Exists(SaveDataPath);

    public static void SaveSettings(GameSettingsData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSettings 실패: {e.Message}");
        }
    }

    public static GameSettingsData LoadSettings()
    {
        if (!File.Exists(SettingsPath)) return new GameSettingsData();

        try
        {
            string json = File.ReadAllText(SettingsPath);
            return JsonUtility.FromJson<GameSettingsData>(json) ?? new GameSettingsData();
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadSettings 실패: {e.Message}");
            return new GameSettingsData();
        }
    }

    private static void WriteJsonToDisk(string path, string json)
    {
        lock (FileWriteLock)
            File.WriteAllText(path, json);
    }

    public static void SaveGame(SaveData data)
    {
        if (suppressProgressLoad) return;

        try
        {
            flushRunner?.Cancel();
            activeWriteTask?.Wait();
            currentSaveData = data;
            OnBeforeFlush?.Invoke();
            isDirty = false;
            RebuildSceneLookup(data);
            WriteJsonToDisk(SaveDataPath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveGame 실패: {e.Message}");
        }
    }

    public static void SaveDeferred(SaveData data)
    {
        if (suppressProgressLoad) return;

        currentSaveData = data;
        isDirty = true;
        if (!Application.isPlaying)
        {
            FlushSave();
            return;
        }

        EnsurePersistenceHooks();
        flushRunner?.Schedule(0.75f);
    }

    private static void FlushAsyncOnMainThread()
    {
        OnBeforeFlush?.Invoke();
        if (!isDirty || currentSaveData == null) return;
        isDirty = false;

        try
        {
            string json = JsonUtility.ToJson(currentSaveData, true);
            string path = SaveDataPath;
            activeWriteTask = Task.Run(() =>
            {
                try
                {
                    WriteJsonToDisk(path, json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"FlushSave async 실패: {e.Message}");
                }
            });
        }
        catch (Exception e)
        {
            isDirty = true;
            Debug.LogError($"SaveDeferred 직렬화 실패: {e.Message}");
        }
    }

    public static void FlushSave()
    {
        if (suppressProgressLoad) return;

        flushRunner?.Cancel();
        try
        {
            activeWriteTask?.Wait();
        }
        catch (Exception e)
        {
            Debug.LogError($"FlushSave 대기 실패: {e.Message}");
        }

        OnBeforeFlush?.Invoke();
        if (!isDirty || currentSaveData == null) return;
        isDirty = false;

        try
        {
            WriteJsonToDisk(SaveDataPath, JsonUtility.ToJson(currentSaveData, true));
        }
        catch (Exception e)
        {
            isDirty = true;
            Debug.LogError($"FlushSave 실패: {e.Message}");
        }
    }

    public static SaveData LoadGame()
    {
        if (suppressProgressLoad || !File.Exists(SaveDataPath))
        {
            currentSaveData = new SaveData();
            RebuildSceneLookup(currentSaveData);
            return currentSaveData;
        }

        try
        {
            string json = File.ReadAllText(SaveDataPath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            if (currentSaveData.scenes == null) currentSaveData.scenes = new List<SceneSaveData>();
            if (currentSaveData.achievements == null) currentSaveData.achievements = new AchievementSaveData();
            if (currentSaveData.mapRegions == null) currentSaveData.mapRegions = new List<MapRegionSaveData>();
            RebuildSceneLookup(currentSaveData);
            return currentSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadGame 실패: {e.Message}");
            currentSaveData = new SaveData();
            RebuildSceneLookup(currentSaveData);
            return currentSaveData;
        }
    }

    public static void SaveInventory(InventorySaveData inventory)
    {
        var gameData = currentSaveData ?? LoadGame();
        gameData.inventory = inventory;
        SaveGame(gameData);
    }

    public static InventorySaveData LoadInventory()
    {
        var gameData = currentSaveData ?? LoadGame();
        return gameData.inventory;
    }

    public static void SaveAchievements(AchievementSaveData achievements)
    {
        var gameData = currentSaveData ?? LoadGame();
        gameData.achievements = achievements;
        SaveGame(gameData);
    }

    public static AchievementSaveData LoadAchievements()
    {
        var gameData = currentSaveData ?? LoadGame();
        if (gameData.achievements == null) gameData.achievements = new AchievementSaveData();
        return gameData.achievements;
    }

    private static MapRegionSaveData GetOrCreateMapRegionData(SaveData gameData, string mapId)
    {
        gameData.mapRegions ??= new List<MapRegionSaveData>();
        var region = gameData.mapRegions.Find(r => r.mapId == mapId);
        if (region == null)
        {
            region = new MapRegionSaveData(mapId);
            gameData.mapRegions.Add(region);
        }
        region.tiles ??= new List<MapTileFogSaveData>();
        return region;
    }

    public static bool IsMapRegionDiscovered(string mapId)
    {
        if (string.IsNullOrEmpty(mapId)) return false;
        var gameData = currentSaveData ?? LoadGame();
        var region = gameData.mapRegions?.Find(r => r.mapId == mapId);
        return region != null && region.isDiscovered;
    }

    public static void MarkMapRegionDiscovered(string mapId, bool deferSave = true)
    {
        if (string.IsNullOrEmpty(mapId)) return;
        var gameData = currentSaveData ?? LoadGame();
        var region = GetOrCreateMapRegionData(gameData, mapId);
        if (region.isDiscovered) return;

        region.isDiscovered = true;
        if (deferSave) SaveDeferred(gameData);
        else isDirty = true;
    }

    public static void SaveMapTileFog(string mapId, string tileId, int width, int height, byte[] fogBytes, bool deferSave = true)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(tileId) || fogBytes == null) return;

        var gameData = currentSaveData ?? LoadGame();
        var region = GetOrCreateMapRegionData(gameData, mapId);
        region.isDiscovered = true;

        string base64 = Convert.ToBase64String(Compress(fogBytes));
        var tile = region.tiles.Find(t => t.tileId == tileId);
        if (tile != null)
        {
            tile.width = width;
            tile.height = height;
            tile.fogBase64Data = base64;
        }
        else
        {
            region.tiles.Add(new MapTileFogSaveData(tileId, width, height, base64));
        }

        if (deferSave) SaveDeferred(gameData);
        else isDirty = true;
    }

    public static byte[] LoadMapTileFog(string mapId, string tileId, int expectedWidth, int expectedHeight)
    {
        if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(tileId)) return null;

        var gameData = currentSaveData ?? LoadGame();
        var region = gameData.mapRegions?.Find(r => r.mapId == mapId);
        var tile = region?.tiles?.Find(t => t.tileId == tileId);
        if (tile == null || string.IsNullOrEmpty(tile.fogBase64Data)) return null;

        if (tile.width != expectedWidth || tile.height != expectedHeight)
        {
            Debug.LogWarning($"[SaveManager] Fog resolution mismatch ({mapId}/{tileId}): saved {tile.width}x{tile.height}, expected {expectedWidth}x{expectedHeight}");
            return null;
        }

        try
        {
            byte[] bytes = Decompress(Convert.FromBase64String(tile.fogBase64Data));
            if (bytes.Length != expectedWidth * expectedHeight) return null;
            return bytes;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadMapTileFog 실패 ({mapId}/{tileId}): {e.Message}");
            return null;
        }
    }

    public static SceneSaveData GetSceneData(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var gameData = currentSaveData ?? LoadGame();
        if (!sceneLookup.TryGetValue(sceneName, out var sceneData) || sceneData == null)
        {
            sceneData = new SceneSaveData(sceneName);
            gameData.scenes.Add(sceneData);
            sceneLookup[sceneName] = sceneData;
        }
        sceneData.openPortalIds ??= new List<string>();
        sceneData.closedPortalIds ??= new List<string>();
        return sceneData;
    }

    public static bool IsItemPicked(string itemId, string sceneName = null)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        var sceneData = GetSceneData(sceneName);
        return sceneData.pickedItemIds.Contains(itemId);
    }

    public static void MarkItemPicked(string itemId, string sceneName = null)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        var gameData = currentSaveData ?? LoadGame();
        var sceneData = GetSceneData(sceneName);
        if (!sceneData.pickedItemIds.Contains(itemId))
        {
            sceneData.pickedItemIds.Add(itemId);
            SaveGame(gameData);
        }
    }

    public static SocketSaveData GetSocketData(string socketId, string sceneName = null)
    {
        if (string.IsNullOrEmpty(socketId)) return null;
        var sceneData = GetSceneData(sceneName);
        return sceneData.sockets.Find(s => s.socketId == socketId);
    }

    public static bool GetSocketPlaced(string socketId, string sceneName = null)
    {
        var entry = GetSocketData(socketId, sceneName);
        return entry != null && entry.isPlaced;
    }

    private static bool AnySceneMatches(Predicate<SceneSaveData> predicate)
    {
        var gameData = currentSaveData ?? LoadGame();
        if (gameData.scenes == null) return false;
        for (int i = 0; i < gameData.scenes.Count; i++)
        {
            var scene = gameData.scenes[i];
            if (scene != null && predicate(scene)) return true;
        }
        return false;
    }

    /// <summary>
    /// 서브 씬 구분 없이 모든 저장된 씬 데이터에서 특정 소켓 ID의 배치 완료 여부를 조회합니다.
    /// </summary>
    public static bool IsSocketPlacedAnyScene(string socketId)
    {
        if (string.IsNullOrEmpty(socketId)) return false;
        return AnySceneMatches(s => s.sockets != null && s.sockets.Exists(e => e != null && e.socketId == socketId && e.isPlaced));
    }

    public static void SaveSocketState(string socketId, bool isPlaced, string placedItemId, int placedAmount = 0, string sceneName = null)
    {
        if (string.IsNullOrEmpty(socketId)) return;
        var gameData = currentSaveData ?? LoadGame();
        var sceneData = GetSceneData(sceneName);
        var entry = sceneData.sockets.Find(s => s.socketId == socketId);
        if (entry != null)
        {
            entry.isPlaced = isPlaced;
            entry.placedItemId = placedItemId;
            entry.placedAmount = placedAmount;
        }
        else
        {
            sceneData.sockets.Add(new SocketSaveData(socketId, isPlaced, placedItemId, placedAmount));
        }
        SaveGame(gameData);
    }

    public static void SaveSocketPlaced(string socketId, bool isPlaced, string sceneName = null)
    {
        SaveSocketState(socketId, isPlaced, null, 0, sceneName);
    }

    public static DiscPuzzleSaveData GetPuzzleState(string puzzleId, string sceneName = null)
    {
        if (string.IsNullOrEmpty(puzzleId)) return null;
        var sceneData = GetSceneData(sceneName);
        return sceneData.puzzles.Find(p => p.puzzleId == puzzleId);
    }

    public static void SavePuzzleState(string puzzleId, bool isCompleted, List<float> angles, string sceneName = null)
    {
        if (string.IsNullOrEmpty(puzzleId)) return;
        var gameData = currentSaveData ?? LoadGame();
        var sceneData = GetSceneData(sceneName);
        var entry = sceneData.puzzles.Find(p => p.puzzleId == puzzleId);
        if (entry != null)
        {
            entry.isCompleted = isCompleted;
            entry.pieceAngles = angles != null ? new List<float>(angles) : new List<float>();
        }
        else
        {
            sceneData.puzzles.Add(new DiscPuzzleSaveData(puzzleId, isCompleted, angles));
        }
        SaveGame(gameData);
    }

    /// <summary>
    /// 서브 씬 구분 없이 모든 저장된 씬 데이터에서 특정 포탈 ID의 개방 여부를 조회합니다.
    /// </summary>
    public static bool IsPortalOpenAnyScene(string portalId)
    {
        return !string.IsNullOrEmpty(portalId) && AnySceneMatches(s => s.openPortalIds != null && s.openPortalIds.Contains(portalId));
    }

    public static bool IsPortalClosedAnyScene(string portalId)
    {
        return !string.IsNullOrEmpty(portalId) && AnySceneMatches(s => s.closedPortalIds != null && s.closedPortalIds.Contains(portalId));
    }

    /// <summary>
    /// 포탈의 개폐 상태를 세이브 데이터에 기록하고 저장합니다.
    /// </summary>
    public static void SetPortalOpenState(string portalId, bool isOpen, string sceneName = null, bool markConsumedClosed = false)
    {
        if (string.IsNullOrEmpty(portalId)) return;
        var gameData = currentSaveData ?? LoadGame();

        if (isOpen)
        {
            bool changed = false;
            var sceneData = GetSceneData(sceneName);
            if (!sceneData.openPortalIds.Contains(portalId))
            {
                sceneData.openPortalIds.Add(portalId);
                changed = true;
            }
            if (gameData.scenes != null)
            {
                for (int i = 0; i < gameData.scenes.Count; i++)
                {
                    var s = gameData.scenes[i];
                    if (s?.closedPortalIds != null && s.closedPortalIds.Remove(portalId))
                        changed = true;
                }
            }
            if (changed)
                SaveGame(gameData);
        }
        else if (gameData.scenes != null)
        {
            bool changed = false;
            for (int i = 0; i < gameData.scenes.Count; i++)
            {
                var s = gameData.scenes[i];
                if (s?.openPortalIds != null && s.openPortalIds.Remove(portalId))
                    changed = true;
            }
            if (markConsumedClosed)
            {
                var sceneData = GetSceneData(sceneName);
                if (!sceneData.closedPortalIds.Contains(portalId))
                {
                    sceneData.closedPortalIds.Add(portalId);
                    changed = true;
                }
            }
            if (changed)
                SaveGame(gameData);
        }
    }

    public static string GetActiveLight(string sceneName = null)
    {
        var sceneData = GetSceneData(sceneName);
        return sceneData?.activeLightName;
    }

    public static void SaveActiveLight(string lightName, string sceneName = null)
    {
        var gameData = currentSaveData ?? LoadGame();
        var sceneData = GetSceneData(sceneName);
        if (sceneData.activeLightName != lightName)
        {
            sceneData.activeLightName = lightName;
            SaveDeferred(gameData);
        }
    }

    public static string GetLastActiveScene()
    {
        var gameData = currentSaveData ?? LoadGame();
        return gameData.lastActiveScene;
    }

    public static void SaveActiveScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        var gameData = currentSaveData ?? LoadGame();
        if (gameData.lastActiveScene != sceneName)
        {
            gameData.lastActiveScene = sceneName;
            SaveDeferred(gameData);
        }
    }

    private static string ResolvePlayerSceneName(SaveData gameData, string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName)) return sceneName;
        if (!string.IsNullOrEmpty(gameData.lastActiveScene)) return gameData.lastActiveScene;
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    public static PlayerSaveData GetPlayerTransform(string sceneName = null)
    {
        var gameData = currentSaveData ?? LoadGame();
        sceneName = ResolvePlayerSceneName(gameData, sceneName);

        if (sceneLookup.TryGetValue(sceneName, out var sceneData) && sceneData?.player != null)
            return sceneData.player;

        if (gameData.scenes != null)
        {
            for (int i = 0; i < gameData.scenes.Count; i++)
            {
                if (gameData.scenes[i]?.player != null)
                    return gameData.scenes[i].player;
            }
        }
        return null;
    }

    public static void SavePlayerTransform(Vector3 position, float yRotation, string sceneName = null, int controlMode = 1, float cameraYaw = 0f, float cameraPitch = 0f, float cameraZoom = 5f)
    {
        var gameData = currentSaveData ?? LoadGame();
        sceneName = ResolvePlayerSceneName(gameData, sceneName);

        gameData.lastActiveScene = sceneName;
        var sceneData = GetSceneData(sceneName);
        sceneData.player = new PlayerSaveData(position, yRotation, controlMode, cameraYaw, cameraPitch, cameraZoom);
        SaveGame(gameData);
    }

    public static void ClearPlayerTransform(string sceneName = null)
    {
        var gameData = currentSaveData ?? LoadGame();
        var sceneData = GetSceneData(sceneName);
        sceneData.player = null;
        SaveGame(gameData);
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
        {
            dstream.CopyTo(output);
        }
        return output.ToArray();
    }

    public static event Action OnBeforeFlush;
    public static event Action OnSaveDataDeleted;

    /// <summary>
    /// 게임 진행 데이터(savedata.json) 파일을 삭제하고 메모리 데이터를 초기화합니다.
    /// </summary>
    public static void DeleteSaveData()
    {
        try
        {
            flushRunner?.Cancel();
            activeWriteTask?.Wait();
            isDirty = false;

            if (File.Exists(SaveDataPath))
                File.Delete(SaveDataPath);

            currentSaveData = new SaveData();
            RebuildSceneLookup(currentSaveData);
            OnSaveDataDeleted?.Invoke();
            Debug.Log("[SaveManager] Save data deleted successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] DeleteSaveData failed: {e.Message}");
        }
    }

    /// <summary>
    /// 환경설정 데이터(settings.json) 파일을 삭제합니다.
    /// </summary>
    public static void DeleteSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
                File.Delete(SettingsPath);

            Debug.Log("[SaveManager] Settings data deleted successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] DeleteSettings failed: {e.Message}");
        }
    }

    /// <summary>
    /// 세이브 파일과 설정 파일을 모두 삭제합니다.
    /// </summary>
    public static void DeleteAllData()
    {
        DeleteSaveData();
        DeleteSettings();
    }
}
