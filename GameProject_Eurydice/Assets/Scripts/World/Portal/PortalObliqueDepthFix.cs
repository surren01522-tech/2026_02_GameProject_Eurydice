using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
#if URP_COMPATIBILITY_MODE
using System.Collections.Generic;
#endif

internal static class PortalObliqueDepthFix
{
    private const string ShaderResourcePath = "Shaders/PortalDepthReproject";
    private static readonly int CameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

#if URP_COMPATIBILITY_MODE
    private sealed class DepthRTPair
    {
        public RTHandle savedOblique;
        public RTHandle linearized;
    }

    private static readonly Dictionary<long, DepthRTPair> RTPool = new();
    private static DepthRTPair s_ActivePair;
#endif

    private static Shader s_Shader;
    private static Material s_Material;
    private static TextureHandle s_SavedRenderGraphDepth;
    private static bool s_HasSavedRenderGraphDepth;
    private static ReprojectDepthPass s_ReprojectPass;
    private static RestoreDepthPass s_RestorePass;

    public static void PrewarmShader()
    {
        EnsureMaterial();
    }

    public static void EnqueuePasses(ScriptableRenderer renderer)
    {
        if (renderer == null || !EnsureMaterial())
            return;

        s_ReprojectPass ??= new ReprojectDepthPass();
        s_RestorePass ??= new RestoreDepthPass();

        renderer.EnqueuePass(s_ReprojectPass);
        renderer.EnqueuePass(s_RestorePass);
    }

    public static void ReleaseResources()
    {
        s_SavedRenderGraphDepth = TextureHandle.nullHandle;
        s_HasSavedRenderGraphDepth = false;

#if URP_COMPATIBILITY_MODE
        s_ActivePair = null;
        foreach (DepthRTPair pair in RTPool.Values)
        {
            pair.savedOblique?.Release();
            pair.linearized?.Release();
        }
        RTPool.Clear();
#endif

        if (s_Material != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(s_Material);
            else UnityEngine.Object.DestroyImmediate(s_Material);
            s_Material = null;
        }
    }

    private static bool EnsureMaterial()
    {
        if (s_Material != null)
            return true;

        if (s_Shader == null)
            s_Shader = Resources.Load<Shader>(ShaderResourcePath);

        if (s_Shader == null || !s_Shader.isSupported)
            return false;

        s_Material = new Material(s_Shader) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }

    private sealed class ReprojectDepthPass : ScriptableRenderPass
    {
        private sealed class ReprojectPassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Material material;
        }

        private static readonly ProfilingSampler PassSampler = new ProfilingSampler("PortalObliqueDepthReproject");

        public ReprojectDepthPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            s_HasSavedRenderGraphDepth = false;
            if (s_Material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            TextureHandle originalDepth = resourceData.cameraDepthTexture;
            if (!originalDepth.IsValid())
                return;

            s_SavedRenderGraphDepth = originalDepth;
            s_HasSavedRenderGraphDepth = true;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.graphicsFormat = GraphicsFormat.R32_SFloat;
            desc.msaaSamples = 1;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;

            TextureHandle linearizedDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_PortalLinearizedDepth", false);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Portal Oblique Depth Reproject", out ReprojectPassData passData, PassSampler))
            {
                passData.source = originalDepth;
                passData.destination = linearizedDepth;
                passData.material = s_Material;

                builder.UseTexture(originalDepth, AccessFlags.Read);
                builder.SetRenderAttachment(linearizedDepth, 0, AccessFlags.WriteAll);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(linearizedDepth, CameraDepthTextureId);

                builder.SetRenderFunc((ReprojectPassData data, RasterGraphContext context) =>
                {
                    RTHandle src = data.source;
                    Vector2 viewportScale = src.useScaling
                        ? new Vector2(src.rtHandleProperties.rtHandleScale.x, src.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;
                    bool yflip = context.GetTextureUVOrigin(in data.source) != context.GetTextureUVOrigin(in data.destination);
                    Vector4 scaleBias = yflip
                        ? new Vector4(viewportScale.x, -viewportScale.y, 0f, viewportScale.y)
                        : new Vector4(viewportScale.x, viewportScale.y, 0f, 0f);

                    Blitter.BlitTexture(context.cmd, data.source, scaleBias, data.material, 1);
                });
            }
        }

#if URP_COMPATIBILITY_MODE
#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.graphicsFormat = GraphicsFormat.R32_SFloat;
            desc.msaaSamples = 1;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;

            long sizeKey = ((long)desc.width << 32) | (uint)desc.height;
            if (!RTPool.TryGetValue(sizeKey, out DepthRTPair pair))
            {
                pair = new DepthRTPair();
                RTPool[sizeKey] = pair;
            }

            RenderingUtils.ReAllocateHandleIfNeeded(ref pair.savedOblique, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_PortalSavedObliqueDepth");
            RenderingUtils.ReAllocateHandleIfNeeded(ref pair.linearized, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_PortalLinearizedDepth");
            s_ActivePair = pair;

            ConfigureTarget(pair.linearized);
        }

#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (s_Material == null || s_ActivePair == null || s_ActivePair.savedOblique == null || s_ActivePair.linearized == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, PassSampler))
            {
                CoreUtils.SetRenderTarget(cmd, s_ActivePair.savedOblique, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None);
                Blitter.BlitTexture(cmd, new Vector4(1f, 1f, 0f, 0f), s_Material, 0);

                Blitter.BlitCameraTexture(cmd, s_ActivePair.savedOblique, s_ActivePair.linearized, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, s_Material, 1);
                cmd.SetGlobalTexture(CameraDepthTextureId, s_ActivePair.linearized);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
#endif
    }

    private sealed class RestoreDepthPass : ScriptableRenderPass
    {
        private sealed class RestorePassData
        {
            public TextureHandle originalDepth;
        }

        private static readonly ProfilingSampler PassSampler = new ProfilingSampler("PortalObliqueDepthRestore");

        public RestoreDepthPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!s_HasSavedRenderGraphDepth || !s_SavedRenderGraphDepth.IsValid())
                return;

            TextureHandle originalDepth = s_SavedRenderGraphDepth;
            s_SavedRenderGraphDepth = TextureHandle.nullHandle;
            s_HasSavedRenderGraphDepth = false;

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Portal Oblique Depth Restore", out RestorePassData passData, PassSampler))
            {
                passData.originalDepth = originalDepth;
                builder.UseTexture(originalDepth, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                builder.SetGlobalTextureAfterPass(originalDepth, CameraDepthTextureId);

                builder.SetRenderFunc((RestorePassData data, RasterGraphContext context) =>
                {
                });
            }
        }

#if URP_COMPATIBILITY_MODE
#if UNITY_6000_0_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (s_ActivePair == null || s_ActivePair.savedOblique == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.SetGlobalTexture(CameraDepthTextureId, s_ActivePair.savedOblique);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
#endif
    }
}
