#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PortalShaderWarmupEditor
{
    private const string OutputAssetPath = "Assets/Resources/PortalShaderVariants.shadervariants";

    [MenuItem("Tools/Portal/Collect Portal Shader Variants")]
    public static void CollectPortalShaderVariants()
    {
        string dir = Path.GetDirectoryName(OutputAssetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputAssetPath);
        if (svc == null)
        {
            svc = new ShaderVariantCollection();
            AssetDatabase.CreateAsset(svc, OutputAssetPath);
        }
        else
        {
            svc.Clear();
        }

        int added = 0;

        Shader portalShader = Shader.Find("Shader Graphs/PortalGateway");
        if (portalShader != null)
        {
            added += TryAdd(svc, portalShader, PassType.ScriptableRenderPipeline);
            added += TryAdd(svc, portalShader, PassType.ScriptableRenderPipeline, "_MAIN_LIGHT_SHADOWS_CASCADE");
            added += TryAdd(svc, portalShader, PassType.ScriptableRenderPipeline, "_MAIN_LIGHT_SHADOWS");
        }

        Shader sliceShader = Shader.Find("Hidden/Eurydice/PortalTravellerSlice");
        if (sliceShader != null)
        {
            added += TryAdd(svc, sliceShader, PassType.ScriptableRenderPipeline);
            added += TryAdd(svc, sliceShader, PassType.ScriptableRenderPipeline, "_MAIN_LIGHT_SHADOWS_CASCADE");
            added += TryAdd(svc, sliceShader, PassType.ScriptableRenderPipeline, "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT");
            added += TryAdd(svc, sliceShader, PassType.ShadowCaster);
        }

        Shader fogShader = Shader.Find("Hidden/VolumetricFog");
        if (fogShader != null)
        {
            PassType[] fogPasses = { PassType.ScriptableRenderPipeline, PassType.ScriptableRenderPipelineDefaultUnlit, PassType.Normal };
            foreach (PassType pt in fogPasses)
            {
                added += TryAdd(svc, fogShader, pt);
                added += TryAdd(svc, fogShader, pt, "_CLUSTER_LIGHT_LOOP");
                added += TryAdd(svc, fogShader, pt, "_CLUSTER_LIGHT_LOOP", "_ADDITIONAL_LIGHTS_CONTRIBUTION_DISABLED");
                added += TryAdd(svc, fogShader, pt, "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS_CASCADE");
                added += TryAdd(svc, fogShader, pt, "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS_CONTRIBUTION_DISABLED");
                added += TryAdd(svc, fogShader, pt, "_ADDITIONAL_LIGHTS_CONTRIBUTION_DISABLED");
            }
        }

        Shader downsampleShader = Shader.Find("Hidden/DownsampleDepth");
        if (downsampleShader != null)
        {
            added += TryAdd(svc, downsampleShader, PassType.ScriptableRenderPipeline);
            added += TryAdd(svc, downsampleShader, PassType.ScriptableRenderPipelineDefaultUnlit);
            added += TryAdd(svc, downsampleShader, PassType.Normal);
        }

        EditorUtility.SetDirty(svc);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PortalShaderWarmupEditor] Collected {added} shader variants into {OutputAssetPath}");
    }

    private static int TryAdd(ShaderVariantCollection svc, Shader shader, PassType passType, params string[] keywords)
    {
        try
        {
            var variant = keywords != null && keywords.Length > 0
                ? new ShaderVariantCollection.ShaderVariant(shader, passType, keywords)
                : new ShaderVariantCollection.ShaderVariant(shader, passType);
            return svc.Add(variant) ? 1 : 0;
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }
}
#endif
