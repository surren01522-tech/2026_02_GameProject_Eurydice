using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MapRegionData))]
public class MapRegionDataEditor : Editor
{
    private const string PrefPrefix = "MinimapEditor_";

    private int cullingMask;
    private Color clearColor;
    private int bakeMaxResolution;
    private float boundsPadding;
    private float tileWorldSize;

    private void OnEnable()
    {
        cullingMask = EditorPrefs.GetInt(PrefPrefix + "Mask", ~(1 << LayerMask.NameToLayer("UI")));
        bakeMaxResolution = EditorPrefs.GetInt(PrefPrefix + "MaxRes", 1024);
        boundsPadding = EditorPrefs.GetFloat(PrefPrefix + "Pad", 2f);
        tileWorldSize = EditorPrefs.GetFloat(PrefPrefix + "TileSize", 0f);
        ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(PrefPrefix + "Clear", "1F232BFF"), out clearColor);
    }

    public override void OnInspectorGUI()
    {
        MapRegionData data = (MapRegionData)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dynamic Bounds & Bake Tools", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        cullingMask = LayerMaskField("Culling Mask", cullingMask);
        clearColor = EditorGUILayout.ColorField("Bake Clear Color", clearColor);
        bakeMaxResolution = EditorGUILayout.IntSlider("Bake Max Resolution", bakeMaxResolution, 256, 4096);
        boundsPadding = EditorGUILayout.FloatField("Bounds Padding (m)", Mathf.Max(0f, boundsPadding));
        tileWorldSize = EditorGUILayout.FloatField("Tile Grid Size (0 = Single)", Mathf.Max(0f, tileWorldSize));
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetInt(PrefPrefix + "Mask", cullingMask);
            EditorPrefs.SetInt(PrefPrefix + "MaxRes", bakeMaxResolution);
            EditorPrefs.SetFloat(PrefPrefix + "Pad", boundsPadding);
            EditorPrefs.SetFloat(PrefPrefix + "TileSize", tileWorldSize);
            EditorPrefs.SetString(PrefPrefix + "Clear", ColorUtility.ToHtmlStringRGBA(clearColor));
        }

        DrawTilePreviewInfo(data);
        EditorGUILayout.Space(6);

        if (GUILayout.Button("1. Compute Dynamic Bounds (No Default Size)"))
            ComputeBoundsAndLayout(data, boundsPadding, tileWorldSize, cullingMask);
        if (GUILayout.Button("2. Bake Top-View PNG(s) (Aspect-Preserving)"))
            BakeRegionTextures(data, cullingMask, clearColor, bakeMaxResolution, boundsPadding, tileWorldSize);
        if (GUILayout.Button("3. Fit Tile UV Rects to Texture Aspect"))
            FitAllTileUvRects(data);
        if (GUILayout.Button("4. Scan Scene MapMarkers -> StaticMarkers"))
            ScanMarkers(data);
    }

    [MenuItem("Tools/Minimap/Recompute Bounds & Bake All Regions")]
    public static void BakeAllRegions()
    {
        int mask = ~(1 << LayerMask.NameToLayer("UI"));
        Color bg = new Color(0.12f, 0.14f, 0.18f, 1f);
        foreach (string guid in AssetDatabase.FindAssets("t:MapRegionData"))
        {
            MapRegionData region = AssetDatabase.LoadAssetAtPath<MapRegionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (region == null) continue;
            BakeRegionTextures(region, mask, bg, 1024, 2f, 0f);
            ScanMarkers(region);
        }
    }

    private static void DrawTilePreviewInfo(MapRegionData data)
    {
        if (data.tiles == null || data.tiles.Count == 0)
        {
            EditorGUILayout.HelpBox("No tiles configured. Use the tool buttons below to compute bounds and bake.", MessageType.Info);
            return;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < data.tiles.Count; i++)
        {
            MapTileData t = data.tiles[i];
            if (t == null) continue;
            Vector2Int fog = data.GetTileFogDimensions(t);
            string tex = t.mapTexture != null ? $"{t.mapTexture.width}x{t.mapTexture.height}" : "None";
            sb.Append($"{t.tileId}: {t.worldSize.x:F1}m x {t.worldSize.y:F1}m | Tex={tex} | Fog={fog.x}x{fog.y}");
            if (i < data.tiles.Count - 1) sb.AppendLine();
        }
        EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
    }

    public static void ComputeBoundsAndLayout(MapRegionData data, float padding, float tileSplitSize, int layerMask = ~0)
    {
        ExecuteWithScenes(data.sceneNames, () =>
        {
            if (!TryCalculateBounds(data, padding, layerMask, out Vector2 center, out Vector2 size))
            {
                Debug.LogWarning($"[MapRegionDataEditor] Could not calculate bounds for '{data.mapId}'.");
                return;
            }
            Undo.RecordObject(data, "Compute Dynamic Map Bounds");
            ApplyBoundsLayout(data, center, size, tileSplitSize);
            SaveAsset(data);
        });
    }

    public static void BakeRegionTextures(MapRegionData data, int mask, Color bgColor, int maxRes, float padding, float tileSplitSize)
    {
        ExecuteWithScenes(data.sceneNames, () =>
        {
            if (TryCalculateBounds(data, padding, mask, out Vector2 center, out Vector2 size))
                ApplyBoundsLayout(data, center, size, tileSplitSize);

            string assetPath = AssetDatabase.GetAssetPath(data);
            if (!data.HasValidTiles || string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"[MapRegionDataEditor] Cannot bake '{data.mapId}'.", data);
                return;
            }

            Undo.RecordObject(data, "Bake Map Region Textures");
            string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string baseName = string.IsNullOrEmpty(data.mapId) ? data.name : data.mapId;

            foreach (MapTileData tile in data.tiles)
            {
                if (tile == null || !tile.IsValid) continue;
                Texture2D rendered = RenderTileSnapshot(tile, data.bakeCameraY, data.bakeDepth, mask, bgColor, maxRes);
                tile.mapTexture = SavePngAsset(rendered, $"{folder}/{baseName}_{tile.tileId}.png", maxRes);
                tile.uvRect = new Rect(0f, 0f, 1f, 1f);
                DestroyImmediate(rendered);
            }
            SaveAsset(data);
        });
    }

    public static void FitAllTileUvRects(MapRegionData data)
    {
        if (data.tiles == null) return;
        Undo.RecordObject(data, "Fit Tile UV Rects");
        foreach (MapTileData t in data.tiles)
        {
            if (t == null || t.mapTexture == null || !t.IsValid) continue;
            float texAspect = (float)t.mapTexture.width / Mathf.Max(1, t.mapTexture.height);
            float worldAspect = t.worldSize.x / Mathf.Max(0.001f, t.worldSize.y);
            if (texAspect > worldAspect)
            {
                float w = worldAspect / texAspect;
                t.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
            {
                float h = texAspect / worldAspect;
                t.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
        }
        SaveAsset(data);
    }

    public static void ScanMarkers(MapRegionData data)
    {
        ExecuteWithScenes(data.sceneNames, () =>
        {
            Undo.RecordObject(data, "Scan Scene MapMarkers");
            var ordered = new List<MapStaticMarkerData>();
            var indexById = new Dictionary<string, int>();

            if (data.staticMarkers != null)
            {
                foreach (MapStaticMarkerData m in data.staticMarkers)
                {
                    if (m == null || string.IsNullOrEmpty(m.markerId) || indexById.ContainsKey(m.markerId)) continue;
                    indexById[m.markerId] = ordered.Count;
                    ordered.Add(m);
                }
            }

            foreach (MapMarker marker in FindObjectsByType<MapMarker>(FindObjectsSortMode.None))
            {
                if (marker == null || (data.sceneNames != null && data.sceneNames.Count > 0 && !data.MatchesScene(marker.SceneName)))
                    continue;
                MapStaticMarkerData snap = marker.ToStaticData();
                if (string.IsNullOrEmpty(snap.markerId)) continue;

                if (indexById.TryGetValue(snap.markerId, out int idx)) ordered[idx] = snap;
                else { indexById[snap.markerId] = ordered.Count; ordered.Add(snap); }
            }

            data.staticMarkers = ordered;
            SaveAsset(data);
        });
    }

    private static void ApplyBoundsLayout(MapRegionData data, Vector2 center, Vector2 size, float tileSplitSize)
    {
        data.worldCenter = center;
        data.worldSize = size;

        var previous = new Dictionary<string, MapTileData>();
        if (data.tiles != null)
            foreach (MapTileData old in data.tiles)
                if (old != null && !string.IsNullOrEmpty(old.tileId)) previous[old.tileId] = old;

        int cols = tileSplitSize > 0.001f ? Mathf.Max(1, Mathf.CeilToInt(size.x / tileSplitSize)) : 1;
        int rows = tileSplitSize > 0.001f ? Mathf.Max(1, Mathf.CeilToInt(size.y / tileSplitSize)) : 1;
        Vector2 cell = new Vector2(size.x / cols, size.y / rows);
        Vector2 min = center - size * 0.5f;

        data.tiles = new List<MapTileData>(cols * rows);
        for (int z = 0; z < rows; z++)
            for (int x = 0; x < cols; x++)
            {
                string id = cols == 1 && rows == 1 ? "main" : $"{x}_{z}";
                previous.TryGetValue(id, out MapTileData prev);
                data.tiles.Add(new MapTileData
                {
                    tileId = id,
                    worldCenter = new Vector2(min.x + (x + 0.5f) * cell.x, min.y + (z + 0.5f) * cell.y),
                    worldSize = cell,
                    mapTexture = prev?.mapTexture,
                    uvRect = prev != null ? prev.uvRect : new Rect(0f, 0f, 1f, 1f),
                    fogWidth = prev != null ? prev.fogWidth : 0,
                    fogHeight = prev != null ? prev.fogHeight : 0
                });
            }
    }

    private static bool TryCalculateBounds(MapRegionData data, float padding, int layerMask, out Vector2 center, out Vector2 size)
    {
        float maxY = data.bakeCameraY - 0.1f;
        float minY = data.bakeCameraY - Mathf.Max(1f, data.bakeDepth);
        Bounds combined = default;
        bool hasBounds = false;

        void Add(Bounds b)
        {
            if (b.max.y < minY || b.min.y > maxY) return;
            if (!hasBounds) { combined = b; hasBounds = true; }
            else combined.Encapsulate(b);
        }

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded || (data.sceneNames != null && data.sceneNames.Count > 0 && !data.sceneNames.Contains(scene.name)))
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || !root.activeInHierarchy || root.CompareTag("Player") || root.CompareTag("MainCamera") || root.GetComponent<PlayerController>() != null)
                    continue;
                foreach (Terrain t in root.GetComponentsInChildren<Terrain>(false))
                    if (t != null && t.enabled && t.terrainData != null && (layerMask & (1 << t.gameObject.layer)) != 0)
                        Add(new Bounds(t.transform.position + t.terrainData.size * 0.5f, t.terrainData.size));
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (r == null || !r.enabled || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                    if ((layerMask & (1 << r.gameObject.layer)) == 0 || r is ParticleSystemRenderer or TrailRenderer or LineRenderer) continue;
                    if (r.GetComponentInParent<PlayerController>() != null || r.GetComponentInParent<Camera>() != null) continue;
                    Add(r.bounds);
                }
            }
        }

        float pad = Mathf.Max(0f, padding) * 2f;
        if (!hasBounds || combined.size.x + pad <= 0.001f || combined.size.z + pad <= 0.001f)
        {
            center = size = Vector2.zero;
            return false;
        }
        center = new Vector2(combined.center.x, combined.center.z);
        size = new Vector2(combined.size.x + pad, combined.size.z + pad);
        return true;
    }

    private static Texture2D RenderTileSnapshot(MapTileData tile, float bakeCameraY, float bakeDepth, int mask, Color bgColor, int maxRes)
    {
        float maxSide = Mathf.Max(tile.worldSize.x, tile.worldSize.y);
        int texW = Mathf.Max(16, Mathf.RoundToInt(maxRes * (tile.worldSize.x / maxSide)));
        int texH = Mathf.Max(16, Mathf.RoundToInt(maxRes * (tile.worldSize.y / maxSide)));

        GameObject camGo = new GameObject("TempMapBakeCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.aspect = (float)texW / texH;
        cam.orthographicSize = tile.worldSize.y * 0.5f;
        cam.cullingMask = mask;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = Mathf.Max(1f, bakeDepth);
        cam.transform.SetPositionAndRotation(new Vector3(tile.worldCenter.x, bakeCameraY, tile.worldCenter.y), Quaternion.Euler(90f, 0f, 0f));

        RenderTexture rt = new RenderTexture(texW, texH, 24, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, texW, texH), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        cam.targetTexture = null;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(camGo);
        return tex;
    }

    private static Texture2D SavePngAsset(Texture2D tex, string assetPath, int maxRes)
    {
        string dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(512, maxRes));
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void ExecuteWithScenes(IReadOnlyList<string> sceneNames, Action action)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[MapRegionDataEditor] Exit Play Mode before running editor bake tools.");
            return;
        }
        if (sceneNames == null || sceneNames.Count == 0) { action?.Invoke(); return; }

        var paths = new List<string>();
        foreach (string s in sceneNames)
        {
            string p = FindScenePath(s);
            if (!string.IsNullOrEmpty(p)) paths.Add(p);
        }
        if (paths.Count == 0) return;

        bool needSwitch = SceneManager.sceneCount != paths.Count || paths.Exists(p => !SceneManager.GetSceneByPath(p).isLoaded);
        if (!needSwitch) { action?.Invoke(); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        SceneSetup[] prevSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            for (int i = 0; i < paths.Count; i++)
                EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
            action?.Invoke();
        }
        finally
        {
            if (prevSetup != null && prevSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(prevSetup);
        }
    }

    private static string FindScenePath(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        foreach (string guid in AssetDatabase.FindAssets($"t:Scene {sceneName}"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(Path.GetFileNameWithoutExtension(p), sceneName, StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    private static void SaveAsset(MapRegionData data)
    {
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private static int LayerMaskField(string label, int selected)
    {
        var layers = new List<int>();
        var names = new List<string>();
        int mask = 0;
        for (int i = 0; i < 32; i++)
        {
            string n = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(n)) continue;
            if ((selected & (1 << i)) != 0) mask |= 1 << layers.Count;
            layers.Add(i);
            names.Add(n);
        }
        mask = EditorGUILayout.MaskField(label, mask, names.ToArray());
        int result = 0;
        for (int i = 0; i < layers.Count; i++)
            if ((mask & (1 << i)) != 0) result |= 1 << layers[i];
        return result;
    }
}
