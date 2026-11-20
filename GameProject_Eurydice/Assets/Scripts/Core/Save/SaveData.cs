using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // 추후 맵/구조가 크게 변경되어 이전 세이브와 호환되지 않을 때 CurrentSaveVersion과 함께 올립니다.
    public const int CurrentSaveVersion = 1;
    public const int MinCompatibleSaveVersion = 0;

    public int saveVersion;
    public InventorySaveData inventory = new();
    public List<MapRegionSaveData> mapRegions = new();
    public string lastActiveScene = string.Empty;
    public List<SceneSaveData> scenes = new();
    public AchievementSaveData achievements = new();

    public static SaveData CreateNew() => new SaveData { saveVersion = CurrentSaveVersion };
}

[Serializable]
public class SceneSaveData
{
    public string sceneName;
    public PlayerSaveData player = null;
    public List<string> pickedItemIds = new();
    public List<SocketSaveData> sockets = new();
    public List<DiscPuzzleSaveData> puzzles = new();
    public List<string> openPortalIds = new();
    public List<string> closedPortalIds = new();
    public string activeLightName;

    public SceneSaveData() { }

    public SceneSaveData(string sceneName)
    {
        this.sceneName = sceneName;
    }
}

[Serializable]
public class PlayerSaveData
{
    public bool hasData;
    public Vector3 position;
    public float yRotation;
    public int controlMode = 1; // 0: FreeLook, 1: Strafe
    public float cameraYaw;
    public float cameraPitch;
    public float cameraZoom = 5f;

    public bool IsValid =>
        hasData ||
        position != Vector3.zero ||
        !Mathf.Approximately(yRotation, 0f) ||
        !Mathf.Approximately(cameraYaw, 0f) ||
        !Mathf.Approximately(cameraPitch, 0f);

    public PlayerSaveData() { }

    public PlayerSaveData(Vector3 position, float yRotation, int controlMode = 1, float cameraYaw = 0f, float cameraPitch = 0f, float cameraZoom = 5f)
    {
        hasData = true;
        this.position = position;
        this.yRotation = yRotation;
        this.controlMode = controlMode;
        this.cameraYaw = cameraYaw;
        this.cameraPitch = cameraPitch;
        this.cameraZoom = cameraZoom;
    }
}

[Serializable]
public class SocketSaveData
{
    public string socketId;
    public bool isPlaced;
    public string placedItemId;
    public int placedAmount;

    public SocketSaveData(string socketId, bool isPlaced, string placedItemId = null, int placedAmount = 0)
    {
        this.socketId = socketId;
        this.isPlaced = isPlaced;
        this.placedItemId = placedItemId;
        this.placedAmount = placedAmount;
    }
}

[Serializable]
public class DiscPuzzleSaveData
{
    public string puzzleId;
    public bool isCompleted;
    public List<float> pieceAngles = new();

    public DiscPuzzleSaveData(string puzzleId, bool isCompleted, List<float> pieceAngles)
    {
        this.puzzleId = puzzleId;
        this.isCompleted = isCompleted;
        this.pieceAngles = pieceAngles ?? new List<float>();
    }
}

[Serializable]
public class MapTileFogSaveData
{
    public string tileId;
    public int width;
    public int height;
    public string fogBase64Data;

    public MapTileFogSaveData(string tileId, int width, int height, string fogBase64Data)
    {
        this.tileId = tileId;
        this.width = width;
        this.height = height;
        this.fogBase64Data = fogBase64Data;
    }
}

[Serializable]
public class MapRegionSaveData
{
    public string mapId;
    public bool isDiscovered;
    public List<MapTileFogSaveData> tiles = new();

    public MapRegionSaveData(string mapId)
    {
        this.mapId = mapId;
    }
}

[Serializable]
public class GameSettingsData
{
    public float masterVolume = 0.8f;
    public float bgmVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float uiVolume = 0.8f;

    public bool masterMute = false;
    public bool bgmMute = false;
    public bool sfxMute = false;
    public bool uiMute = false;

    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
    public int qualityLevel = 2;
    public int vSyncCount = 0;
    public int targetFrameRate = 60;

    public float mouseSensitivity = 1.0f;
    public bool invertY = true;

    public string keyBindingsJson = string.Empty;
}

[Serializable]
public class InventorySaveData
{
    public List<InventorySlotSaveData> slots = new();
}

[Serializable]
public class InventorySlotSaveData
{
    public string itemId;
    public int count;

    public InventorySlotSaveData(string itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
    }
}

[Serializable]
public class AchievementSaveData
{
    public float totalPlayTime;
    public int clearedPuzzleCount;
    public int totalCollectedItemCount;
    public int socketedCount;
    public int clearedSocketGroupCount;
    public List<string> clearedPuzzleIds = new();
    public List<string> clearedSocketIds = new();
    public List<string> clearedSocketGroupIds = new();
    public List<string> unlockedIds = new();
    public List<AchievementProgressSaveData> progresses = new();
}

[Serializable]
public class AchievementProgressSaveData
{
    public string id;
    public float current;

    public AchievementProgressSaveData() { }

    public AchievementProgressSaveData(string id, float current)
    {
        this.id = id;
        this.current = current;
    }
}
