using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SettingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
    private static readonly string SaveDataPath = Path.Combine(Application.persistentDataPath, "savedata.json");

    private static SaveData currentSaveData;

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

    public static void SaveGame(SaveData data)
    {
        try
        {
            currentSaveData = data;
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveDataPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveGame 실패: {e.Message}");
        }
    }

    public static SaveData LoadGame()
    {
        if (!File.Exists(SaveDataPath))
        {
            currentSaveData = new SaveData();
            return currentSaveData;
        }

        try
        {
            string json = File.ReadAllText(SaveDataPath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            if (currentSaveData.scenes == null) currentSaveData.scenes = new List<SceneSaveData>();
            if (currentSaveData.achievements == null) currentSaveData.achievements = new AchievementSaveData();
            return currentSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadGame 실패: {e.Message}");
            currentSaveData = new SaveData();
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

    public static void SaveMinimapFog(string mapId, byte[] fogBytes)
    {
        if (string.IsNullOrEmpty(mapId) || fogBytes == null) return;

        var gameData = currentSaveData ?? LoadGame();
        byte[] compressed = Compress(fogBytes);
        string base64 = Convert.ToBase64String(compressed);

        var existing = gameData.fogRegions.Find(r => r.mapId == mapId);
        if (existing != null)
        {
            existing.fogBase64Data = base64;
        }
        else
        {
            gameData.fogRegions.Add(new MinimapFogSaveData(mapId, base64));
        }

        SaveGame(gameData);
    }

    public static byte[] LoadMinimapFog(string mapId)
    {
        if (string.IsNullOrEmpty(mapId)) return null;

        var gameData = currentSaveData ?? LoadGame();
        var existing = gameData.fogRegions.Find(r => r.mapId == mapId);
        if (existing == null || string.IsNullOrEmpty(existing.fogBase64Data)) return null;

        try
        {
            byte[] compressed = Convert.FromBase64String(existing.fogBase64Data);
            return Decompress(compressed);
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadMinimapFog 실패: {e.Message}");
            return null;
        }
    }

    public static SceneSaveData GetSceneData(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var gameData = currentSaveData ?? LoadGame();
        var sceneData = gameData.scenes.Find(s => s.sceneName == sceneName);
        if (sceneData == null)
        {
            sceneData = new SceneSaveData(sceneName);
            gameData.scenes.Add(sceneData);
        }
        sceneData.openPortalIds ??= new List<string>();
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

    /// <summary>
    /// 서브 씬 구분 없이 모든 저장된 씬 데이터에서 특정 소켓 ID의 배치 완료 여부를 조회합니다.
    /// </summary>
    public static bool IsSocketPlacedAnyScene(string socketId)
    {
        if (string.IsNullOrEmpty(socketId)) return false;
        var gameData = currentSaveData ?? LoadGame();
        if (gameData.scenes == null) return false;

        for (int i = 0; i < gameData.scenes.Count; i++)
        {
            var scene = gameData.scenes[i];
            if (scene?.sockets == null) continue;
            var entry = scene.sockets.Find(s => s.socketId == socketId);
            if (entry != null && entry.isPlaced)
                return true;
        }
        return false;
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
        if (string.IsNullOrEmpty(portalId)) return false;
        var gameData = currentSaveData ?? LoadGame();
        if (gameData.scenes == null) return false;

        for (int i = 0; i < gameData.scenes.Count; i++)
        {
            var scene = gameData.scenes[i];
            if (scene?.openPortalIds != null && scene.openPortalIds.Contains(portalId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 포탈의 개폐 상태를 세이브 데이터에 기록하고 저장합니다.
    /// </summary>
    public static void SetPortalOpenState(string portalId, bool isOpen, string sceneName = null)
    {
        if (string.IsNullOrEmpty(portalId)) return;
        var gameData = currentSaveData ?? LoadGame();

        if (isOpen)
        {
            var sceneData = GetSceneData(sceneName);
            if (!sceneData.openPortalIds.Contains(portalId))
            {
                sceneData.openPortalIds.Add(portalId);
                SaveGame(gameData);
            }
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
            SaveGame(gameData);
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
            SaveGame(gameData);
        }
    }

    public static PlayerSaveData GetPlayerTransform(string sceneName = null)
    {
        var gameData = currentSaveData ?? LoadGame();
        if (string.IsNullOrEmpty(sceneName))
        {
            if (!string.IsNullOrEmpty(gameData.lastActiveScene))
                sceneName = gameData.lastActiveScene;
            else
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        var sceneData = gameData.scenes.Find(s => s.sceneName == sceneName);
        if (sceneData?.player != null) return sceneData.player;

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
        if (string.IsNullOrEmpty(sceneName))
        {
            if (!string.IsNullOrEmpty(gameData.lastActiveScene))
                sceneName = gameData.lastActiveScene;
            else
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

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

    public static event Action OnSaveDataDeleted;

    /// <summary>
    /// 게임 진행 데이터(savedata.json) 파일을 삭제하고 메모리 데이터를 초기화합니다.
    /// </summary>
    public static void DeleteSaveData()
    {
        try
        {
            if (File.Exists(SaveDataPath))
                File.Delete(SaveDataPath);

            currentSaveData = new SaveData();
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
