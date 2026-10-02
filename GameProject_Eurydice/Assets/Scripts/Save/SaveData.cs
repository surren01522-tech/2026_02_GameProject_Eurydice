using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public InventorySaveData inventory = new();
    public List<MinimapFogSaveData> fogRegions = new();
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
