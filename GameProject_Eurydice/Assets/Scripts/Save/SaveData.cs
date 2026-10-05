using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public InventorySaveData inventory = new();
    public List<MinimapFogSaveData> fogRegions = new();
    public string lastActiveScene = string.Empty;
    public List<SceneSaveData> scenes = new();
}

[Serializable]
public class SceneSaveData
{
    public string sceneName;
    public PlayerSaveData player = null;
    public List<string> pickedItemIds = new();
    public List<SocketSaveData> sockets = new();
    public List<DiscPuzzleSaveData> puzzles = new();

    public SceneSaveData() { }

    public SceneSaveData(string sceneName)
    {
        this.sceneName = sceneName;
    }
}

[Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public float yRotation;
    public int controlMode = 1; // 0: FreeLook, 1: Strafe
    public float cameraYaw;
    public float cameraPitch;
    public float cameraZoom = 5f;

    public PlayerSaveData() { }

    public PlayerSaveData(Vector3 position, float yRotation, int controlMode = 1, float cameraYaw = 0f, float cameraPitch = 0f, float cameraZoom = 5f)
    {
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

    public SocketSaveData(string socketId, bool isPlaced)
    {
        this.socketId = socketId;
        this.isPlaced = isPlaced;
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
public class MinimapFogSaveData
{
    public string mapId;
    public string fogBase64Data;

    public MinimapFogSaveData(string mapId, string fogBase64Data)
    {
        this.mapId = mapId;
        this.fogBase64Data = fogBase64Data;
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
