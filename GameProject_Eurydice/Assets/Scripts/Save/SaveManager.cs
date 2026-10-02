using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class SaveManager
{
    private static readonly string SettingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
    private static readonly string SaveDataPath = Path.Combine(Application.persistentDataPath, "savedata.json");

    private static SaveData currentSaveData;

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
