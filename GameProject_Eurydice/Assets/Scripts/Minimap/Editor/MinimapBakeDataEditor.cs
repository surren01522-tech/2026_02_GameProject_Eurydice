using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MinimapBakeData))]
public class MinimapBakeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (GUILayout.Button("Bake World Minimap", GUILayout.Height(30)))
        {
            Bake((MinimapBakeData)target);
        }
    }

    private static void Bake(MinimapBakeData data)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var openedScenes = new List<Scene>();
        var targetScenes = new List<Scene>();

        if (data.scenes.Count == 0)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                targetScenes.Add(SceneManager.GetSceneAt(i));
        }

        foreach (string sceneName in data.scenes)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                string path = FindScenePath(sceneName);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("[MinimapBaker] Scene not found in Build Settings: " + sceneName);
                    continue;
                }
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                openedScenes.Add(scene);
            }
            targetScenes.Add(scene);
        }

        try
        {
            if (!TryGetBounds(targetScenes, data.cullingMask, out Bounds bounds))
            {
                Debug.LogWarning("[MinimapBaker] No renderers found to bake.");
                return;
            }

            float side = Mathf.Max(bounds.size.x, bounds.size.z) + data.padding * 2f;
            data.worldCenter = new Vector2(bounds.center.x, bounds.center.z);
            data.worldSize = new Vector2(side, side);
            data.mapTexture = SaveTexture(data, RenderSnapshot(data, bounds, side));

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[MinimapBaker] Baked: " + AssetDatabase.GetAssetPath(data.mapTexture));
        }
        finally
        {
            for (int i = 0; i < openedScenes.Count; i++)
                EditorSceneManager.CloseScene(openedScenes[i], true);
        }
    }

    private static string FindScenePath(string sceneName)
    {
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (Path.GetFileNameWithoutExtension(s.path) == sceneName)
                return s.path;
        }
        return null;
    }

    private static bool TryGetBounds(List<Scene> scenes, LayerMask mask, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < scenes.Count; i++)
        {
            var roots = scenes[i].GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
            {
                var renderers = roots[j].GetComponentsInChildren<Renderer>();
                for (int k = 0; k < renderers.Length; k++)
                {
                    if (((1 << renderers[k].gameObject.layer) & mask) == 0) continue;

                    if (!hasBounds)
                    {
                        bounds = renderers[k].bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderers[k].bounds);
                    }
                }

                var terrains = roots[j].GetComponentsInChildren<Terrain>();
                for (int k = 0; k < terrains.Length; k++)
                {
                    var t = terrains[k];
                    if (t.terrainData == null || ((1 << t.gameObject.layer) & mask) == 0) continue;

                    Vector3 size = t.terrainData.size;
                    Bounds tBounds = new Bounds(t.transform.position + size * 0.5f, size);

                    if (!hasBounds)
                    {
                        bounds = tBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(tBounds);
                    }
                }
            }
        }

        return hasBounds;
    }

    private static Texture2D RenderSnapshot(MinimapBakeData data, Bounds bounds, float side)
    {
        int res = Mathf.Max(32, data.resolution);
        var rt = RenderTexture.GetTemporary(res, res, 24, RenderTextureFormat.ARGB32);

        var cam = new GameObject("MinimapBakeCamera").AddComponent<Camera>();
        cam.gameObject.hideFlags = HideFlags.HideAndDontSave;
        cam.enabled = false;
        cam.orthographic = true;
        cam.orthographicSize = side * 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = data.clearColor;
        cam.cullingMask = data.cullingMask;
        cam.targetTexture = rt;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = bounds.size.y + 200f;
        cam.transform.SetPositionAndRotation(
            new Vector3(data.worldCenter.x, bounds.max.y + 100f, data.worldCenter.y),
            Quaternion.Euler(90f, 0f, 0f)
        );

        cam.Render();

        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();
        RenderTexture.active = prevRT;

        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(cam.gameObject);
        return tex;
    }

    private static Texture2D SaveTexture(MinimapBakeData data, Texture2D tex)
    {
        string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(data));
        string path = Path.Combine(dir, data.name + "_Map.png").Replace('\\', '/');

        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(data.resolution), 32, 16384);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
