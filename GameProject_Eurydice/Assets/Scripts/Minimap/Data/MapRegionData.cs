using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapMarkerType
{
    Portal = 0,
    Landmark = 1,
    Objective = 2,
    Collectible = 3,
    Custom = 4
}

[Serializable]
public class MapStaticMarkerData
{
    public string markerId;
    public MapMarkerType markerType;
    public string label;
    public Vector3 worldPosition;
    public bool revealWithFog = true;
    public string targetMapId;
    public Sprite customIcon;
    public bool useCustomColor;
    public Color customColor = Color.white;

    public MapStaticMarkerData Clone() => (MapStaticMarkerData)MemberwiseClone();
}

[Serializable]
public class MapTileData
{
    public string tileId = "main";
    public Vector2 worldCenter;
    public Vector2 worldSize;
    public Texture2D mapTexture;
    public Rect uvRect = new Rect(0f, 0f, 1f, 1f);

    [Header("Optional Explicit Fog Size (0 = Dynamic Aspect)")]
    public int fogWidth;
    public int fogHeight;

    public Vector2 WorldMin => worldCenter - worldSize * 0.5f;
    public Vector2 WorldMax => worldCenter + worldSize * 0.5f;
    public bool IsValid => !string.IsNullOrEmpty(tileId) && worldSize.x > 0.001f && worldSize.y > 0.001f;
}

[CreateAssetMenu(fileName = "MapRegionData", menuName = "Minimap/Region Data")]
public class MapRegionData : ScriptableObject
{
    [Header("Identity")]
    public string mapId;
    public string displayName;
    public List<string> sceneNames = new();

    [Header("World Bounds")]
    public Vector2 worldCenter;
    public Vector2 worldSize;

    [Header("Bake Camera Settings")]
    public float bakeCameraY = 500f;
    [Min(1f)] public float bakeDepth = 2000f;

    [Header("Fog Settings")]
    [Min(1f)] public float revealRadius = 15f;
    [Range(0.05f, 1f)] public float innerRevealPercent = 0.65f;
    [Min(0.25f)] public float fogPixelsPerMeter = 1.5f;
    [Min(16)] public int minFogResolution = 32;
    [Min(32)] public int maxFogResolution = 512;

    [Header("Tiles & Static Markers")]
    public List<MapTileData> tiles = new();
    public List<MapStaticMarkerData> staticMarkers = new();

    public bool HasValidBounds => worldSize.x > 0.001f && worldSize.y > 0.001f;
    public bool HasValidTiles => HasValidBounds && tiles != null && tiles.Count > 0;

    public bool MatchesScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName) && sceneNames != null && sceneNames.Contains(sceneName);
    }

    public Vector2 WorldToRegionNormalized(Vector3 worldPos)
    {
        if (!HasValidBounds) return Vector2.zero;
        Vector2 min = worldCenter - worldSize * 0.5f;
        return new Vector2(
            Mathf.Clamp01((worldPos.x - min.x) / worldSize.x),
            Mathf.Clamp01((worldPos.z - min.y) / worldSize.y));
    }

    public Vector2Int GetTileFogDimensions(MapTileData tile)
    {
        if (tile == null || !tile.IsValid) return Vector2Int.zero;
        if (tile.fogWidth > 0 && tile.fogHeight > 0)
            return new Vector2Int(Mathf.Max(8, tile.fogWidth), Mathf.Max(8, tile.fogHeight));

        float w = Mathf.Max(1f, tile.worldSize.x * fogPixelsPerMeter);
        float h = Mathf.Max(1f, tile.worldSize.y * fogPixelsPerMeter);

        float maxSide = Mathf.Max(w, h);
        if (maxSide > maxFogResolution)
        {
            float scale = maxFogResolution / maxSide;
            w *= scale;
            h *= scale;
        }

        float minSide = Mathf.Min(w, h);
        if (minSide < minFogResolution)
        {
            float scale = Mathf.Min(minFogResolution / Mathf.Max(1f, minSide), maxFogResolution / Mathf.Max(1f, Mathf.Max(w, h)));
            w *= scale;
            h *= scale;
        }

        return new Vector2Int(Mathf.Max(8, Mathf.RoundToInt(w)), Mathf.Max(8, Mathf.RoundToInt(h)));
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(mapId)) mapId = name;
        if (string.IsNullOrEmpty(displayName)) displayName = mapId;
    }
}
