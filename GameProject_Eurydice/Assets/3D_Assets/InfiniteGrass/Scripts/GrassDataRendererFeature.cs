using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GrassDataRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private LayerMask heightMapLayer;
    [SerializeField] private Material heightMapMat;
    [SerializeField] private ComputeShader computeShader;

    private GrassDataPass grassDataPass;

    private static readonly List<GrassDataPass> ActivePasses = new List<GrassDataPass>();

    public static void MarkRenderersDirty()
    {
        for (int i = 0; i < ActivePasses.Count; i++)
            ActivePasses[i]?.MarkRenderersDirty();
    }

    public override void Create()
    {
        DisposePass();
        grassDataPass = new GrassDataPass(heightMapLayer, heightMapMat, computeShader)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
        };
        ActivePasses.Add(grassDataPass);

        SceneManager.sceneLoaded -= OnSceneChanged;
        SceneManager.sceneLoaded += OnSceneChanged;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (grassDataPass != null)
            renderer.EnqueuePass(grassDataPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SceneManager.sceneLoaded -= OnSceneChanged;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            DisposePass();
        }
    }

    private void DisposePass()
    {
        if (grassDataPass == null)
            return;

        ActivePasses.Remove(grassDataPass);
        grassDataPass.Dispose();
        grassDataPass = null;
    }

    private static void OnSceneChanged(Scene scene, LoadSceneMode mode) => MarkRenderersDirty();
    private static void OnSceneUnloaded(Scene scene) => MarkRenderersDirty();

    private class GrassDataPass : ScriptableRenderPass
    {
        private struct ModifierEntry
        {
            public Renderer renderer;
            public Material material;
            public int submeshIndex;
        }

        private const int DataTextureSize = 2048;
        private const int LocalSlopeTextureSize = 512;
        private const int MaxVisibleTiles = 65535;
        private const int TileW = 16;
        private const int TileH = 32;

        private static readonly Color NeutralSlopeColor = new Color(0.5f, 0.5f, 0f, 0f);
        private static readonly uint[] InitialIndirectArgs = { 0u, 1u, 1u };

        private RTHandle heightRT;
        private RTHandle heightDepthRT;
        private RTHandle maskRT;
        private RTHandle colorRT;
        private RTHandle slopeRT;
        private RTHandle localSlopeRT;

        private readonly LayerMask heightMapLayer;
        private readonly Material heightMapMat;
        private readonly ComputeShader computeShader;
        private readonly int tileCullKernel = -1;
        private readonly int spawnDenseKernel = -1;
        private readonly int spawnSparseKernel = -1;

        private ComputeBuffer grassPositionsBuffer;
        private ComputeBuffer denseTilesBuffer;
        private ComputeBuffer sparseTilesBuffer;
        private ComputeBuffer denseArgsBuffer;
        private ComputeBuffer sparseArgsBuffer;

        private readonly List<Renderer> heightRenderers = new List<Renderer>(16);
        private readonly List<ModifierEntry> maskModifiers = new List<ModifierEntry>(16);
        private readonly List<ModifierEntry> colorModifiers = new List<ModifierEntry>(16);
        private readonly List<ModifierEntry> slopeModifiers = new List<ModifierEntry>(16);
        private readonly List<Material> tempMaterials = new List<Material>(8);
        private readonly Plane[] tempPlanes = new Plane[6];

        private bool renderersDirty = true;
        private float lastEditorRefreshTime = -10f;

        private bool hasStaticCache;
        private int cachedCamId;
        private int cachedGrassRendererId;
        private Vector2 cachedCenterPos;
        private float cachedDataRadius;
        private Vector2 cachedBoundsYMinMax;
        private bool maskClearedOnce;
        private bool colorClearedOnce;
        private bool globalSlopeHasContent = true;
        private bool localSlopeHasContent = true;

        public GrassDataPass(LayerMask heightMapLayer, Material heightMapMat, ComputeShader computeShader)
        {
            this.heightMapLayer = heightMapLayer;
            this.heightMapMat = heightMapMat;
            this.computeShader = computeShader;

            if (computeShader != null)
            {
                tileCullKernel = computeShader.FindKernel("TileCull");
                spawnDenseKernel = computeShader.FindKernel("SpawnDense");
                spawnSparseKernel = computeShader.FindKernel("SpawnSparse");
            }
        }

        public void MarkRenderersDirty() => renderersDirty = true;

        private void EnsureRTHandles()
        {
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref heightRT,
                new RenderTextureDescriptor(DataTextureSize, DataTextureSize, GraphicsFormat.R16G16_UNorm, GraphicsFormat.None),
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GrassHeightMapRT"
            );

            var depthDesc = new RenderTextureDescriptor(DataTextureSize, DataTextureSize, RenderTextureFormat.Depth, 32)
            {
                graphicsFormat = GraphicsFormat.None
            };
            RenderingUtils.ReAllocateHandleIfNeeded(ref heightDepthRT, depthDesc, FilterMode.Point, TextureWrapMode.Clamp, name: "_GrassHeightDepthRT");

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref maskRT,
                new RenderTextureDescriptor(DataTextureSize, DataTextureSize, GraphicsFormat.R8_UNorm, GraphicsFormat.None),
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GrassMaskMapRT"
            );

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref colorRT,
                new RenderTextureDescriptor(DataTextureSize, DataTextureSize, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None),
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GrassColorRT"
            );

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref slopeRT,
                new RenderTextureDescriptor(DataTextureSize, DataTextureSize, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None),
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GrassSlopeRT"
            );

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref localSlopeRT,
                new RenderTextureDescriptor(LocalSlopeTextureSize, LocalSlopeTextureSize, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None),
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GrassLocalSlopeRT"
            );
        }

        private void EnsureComputeBuffers(float maxBufferCount)
        {
            int targetCapacity = Mathf.Max(1024, (int)(1000000f * maxBufferCount));
            if (grassPositionsBuffer == null || grassPositionsBuffer.count != targetCapacity)
            {
                grassPositionsBuffer?.Release();
                grassPositionsBuffer = new ComputeBuffer(targetCapacity, sizeof(float) * 3, ComputeBufferType.Append);
            }

            if (denseTilesBuffer == null)
                denseTilesBuffer = new ComputeBuffer(MaxVisibleTiles, sizeof(int) * 2, ComputeBufferType.Append);

            if (sparseTilesBuffer == null)
                sparseTilesBuffer = new ComputeBuffer(MaxVisibleTiles, sizeof(int) * 3, ComputeBufferType.Append);

            if (denseArgsBuffer == null)
            {
                denseArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);
                denseArgsBuffer.SetData(InitialIndirectArgs);
            }

            if (sparseArgsBuffer == null)
            {
                sparseArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);
                sparseArgsBuffer.SetData(InitialIndirectArgs);
            }
        }

        private bool RefreshSceneRenderersIfNeeded()
        {
            if (!Application.isPlaying && Time.realtimeSinceStartup - lastEditorRefreshTime > 0.5f)
                renderersDirty = true;

            if (!renderersDirty)
                return false;

            renderersDirty = false;
            lastEditorRefreshTime = Time.realtimeSinceStartup;

            heightRenderers.Clear();
            maskModifiers.Clear();
            colorModifiers.Clear();
            slopeModifiers.Clear();

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r == null || !r.gameObject.activeInHierarchy)
                    continue;

                if ((heightMapLayer.value & (1 << r.gameObject.layer)) != 0)
                    heightRenderers.Add(r);

                tempMaterials.Clear();
                r.GetSharedMaterials(tempMaterials);
                for (int s = 0; s < tempMaterials.Count; s++)
                {
                    Material mat = tempMaterials[s];
                    if (mat == null)
                        continue;

                    string tag = mat.GetTag("LightMode", false);
                    if (tag == "GrassMask")
                        maskModifiers.Add(new ModifierEntry { renderer = r, material = mat, submeshIndex = s });
                    else if (tag == "GrassColor")
                        colorModifiers.Add(new ModifierEntry { renderer = r, material = mat, submeshIndex = s });
                    else if (tag == "GrassSlope")
                        slopeModifiers.Add(new ModifierEntry { renderer = r, material = mat, submeshIndex = s });
                }
            }

            return true;
        }

        private Vector2 ComputeTerrainBoundsY(Bounds fallbackBounds, Vector2 center, float radius)
        {
            float minX = center.x - radius;
            float maxX = center.x + radius;
            float minZ = center.y - radius;
            float maxZ = center.y + radius;

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < heightRenderers.Count; i++)
            {
                Renderer r = heightRenderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                Bounds b = r.bounds;
                if (b.max.x < minX || b.min.x > maxX || b.max.z < minZ || b.min.z > maxZ)
                    continue;

                if (b.min.y < minY) minY = b.min.y;
                if (b.max.y > maxY) maxY = b.max.y;
            }

            if (minY > maxY)
            {
                minY = fallbackBounds.min.y;
                maxY = fallbackBounds.max.y;
            }
            else
            {
                minY -= 2f;
                maxY += 2f;
            }

            if (maxY - minY < 1f)
                maxY = minY + 1f;

            return new Vector2(minY, maxY);
        }

        private static bool HasModifierOutsideBounds(List<ModifierEntry> modifiers, Vector2 center, float radius)
        {
            float minX = center.x - radius;
            float maxX = center.x + radius;
            float minZ = center.y - radius;
            float maxZ = center.y + radius;

            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierEntry entry = modifiers[i];
                Renderer r = entry.renderer;
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                Bounds b = r.bounds;
                if (b.min.x < minX || b.max.x > maxX || b.min.z < minZ || b.max.z > maxZ)
                    return true;
            }

            return false;
        }

        private class PassData
        {
            public TextureHandle heightTexture;
            public TextureHandle heightDepthTexture;
            public TextureHandle maskTexture;
            public TextureHandle colorTexture;
            public TextureHandle slopeTexture;
            public TextureHandle localSlopeTexture;

            public RTHandle heightRTHandle;
            public RTHandle maskRTHandle;

            public Material heightMapMat;
            public ComputeShader computeShader;
            public int tileCullKernel;
            public int spawnDenseKernel;
            public int spawnSparseKernel;

            public Matrix4x4 viewMatrix;
            public Matrix4x4 projectionMatrix;
            public Matrix4x4 localViewMatrix;
            public Matrix4x4 localProjectionMatrix;
            public Matrix4x4 originalViewMatrix;
            public Matrix4x4 originalProjectionMatrix;
            public bool isTargetFlipped;

            public Vector2 centerPos;
            public float dataRadius;
            public Vector2 localCenterPos;
            public float localSlopeRadius;
            public float slopeVerticalTolerance;
            public Vector2 boundsYMinMax;

            public float spacing;
            public float fullDensityDistance;
            public float drawDistance;
            public Vector2Int tileStartIndex;
            public Vector2Int tileGridSize;
            public Matrix4x4 cameraVPMatrix;
            public Vector3 cameraPosition;

            public Vector4 plane0, plane1, plane2, plane3, plane4, plane5;

            public ComputeBuffer grassPositionsBuffer;
            public ComputeBuffer denseTilesBuffer;
            public ComputeBuffer sparseTilesBuffer;
            public ComputeBuffer denseArgsBuffer;
            public ComputeBuffer sparseArgsBuffer;
            public ComputeBuffer argsBuffer;
            public ComputeBuffer tBuffer;
            public bool previewVisibleGrassCount;

            public bool updateHeight;
            public bool updateMask;
            public bool updateColor;
            public bool hasColor;
            public bool updateGlobalSlope;
            public bool hasGlobalSlope;
            public bool updateLocalSlope;
            public bool hasLocalSlope;

            public List<Renderer> heightRenderers;
            public List<ModifierEntry> maskModifiers;
            public List<ModifierEntry> colorModifiers;
            public List<ModifierEntry> slopeModifiers;
            public GrassDataPass owner;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            InfiniteGrassRenderer grass = InfiniteGrassRenderer.GetActiveForCurrentScene();
            if (grass == null || heightMapMat == null || computeShader == null ||
                tileCullKernel < 0 || spawnDenseKernel < 0 || spawnSparseKernel < 0)
                return;

            if (grass.spacing <= 0f || grass.grassMaterial == null)
                return;

            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            Camera cam = cameraData.camera;
            if (!grass.ShouldRenderCamera(cam, cameraData.cameraType))
                return;

            grass.EnsureBuffers();
            EnsureRTHandles();
            EnsureComputeBuffers(grass.maxBufferCount);

            bool refreshed = RefreshSceneRenderersIfNeeded();

            float spacing = grass.spacing;
            float fullDensityDistance = grass.fullDensityDistance;
            float drawDistance = grass.drawDistance;
            float threshold = Mathf.Max(0.01f, grass.textureUpdateThreshold);
            float dataRadius = drawDistance + threshold;

            Vector2 centerPos = new Vector2(
                Mathf.Floor(cam.transform.position.x / threshold) * threshold,
                Mathf.Floor(cam.transform.position.z / threshold) * threshold
            );

            Bounds cameraBounds = grass.CalculateCameraBounds(cam, drawDistance);
            Vector2 boundsY = ComputeTerrainBoundsY(cameraBounds, centerPos, dataRadius);

            float localRadius = Mathf.Clamp(grass.localSlopeRadius, 5f, dataRadius);
            Vector3 focusPos = PlayerTracker.Instance != null ? PlayerTracker.Instance.FootPosition : cam.transform.position;
            Vector2 localCenterPos = new Vector2(focusPos.x, focusPos.z);

            float orthoDepth = (boundsY.y - boundsY.x) + 10f;
            float orthoTopY = boundsY.y + 5f;
            Quaternion topDownRot = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Vector3 orthoScale = new Vector3(1f, 1f, -1f);

            Matrix4x4 viewMatrix = Matrix4x4.TRS(new Vector3(centerPos.x, orthoTopY, centerPos.y), topDownRot, orthoScale).inverse;
            Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-dataRadius, dataRadius, -dataRadius, dataRadius, 0f, orthoDepth);

            Matrix4x4 localViewMatrix = Matrix4x4.TRS(new Vector3(localCenterPos.x, orthoTopY, localCenterPos.y), topDownRot, orthoScale).inverse;
            Matrix4x4 localProjMatrix = Matrix4x4.Ortho(-localRadius, localRadius, -localRadius, localRadius, 0f, orthoDepth);

            int gridMinX = Mathf.FloorToInt(cameraBounds.min.x / spacing);
            int gridMinZ = Mathf.FloorToInt(cameraBounds.min.z / spacing);
            int gridMaxX = Mathf.CeilToInt(cameraBounds.max.x / spacing);
            int gridMaxZ = Mathf.CeilToInt(cameraBounds.max.z / spacing);

            Vector2Int tileStartIndex = new Vector2Int(
                Mathf.FloorToInt(gridMinX / (float)TileW),
                Mathf.FloorToInt(gridMinZ / (float)TileH)
            );
            Vector2Int tileEndIndex = new Vector2Int(
                Mathf.CeilToInt((gridMaxX + 1) / (float)TileW),
                Mathf.CeilToInt((gridMaxZ + 1) / (float)TileH)
            );
            Vector2Int tileGridSize = new Vector2Int(
                Mathf.Max(1, tileEndIndex.x - tileStartIndex.x),
                Mathf.Max(1, tileEndIndex.y - tileStartIndex.y)
            );

            int camId = cam.GetInstanceID();
            int grassId = grass.GetInstanceID();
            bool needStaticUpdate = !hasStaticCache ||
                                    !Application.isPlaying ||
                                    refreshed ||
                                    cachedCamId != camId ||
                                    cachedGrassRendererId != grassId ||
                                    cachedCenterPos != centerPos ||
                                    !Mathf.Approximately(cachedDataRadius, dataRadius) ||
                                    cachedBoundsYMinMax != boundsY;

            if (needStaticUpdate)
            {
                hasStaticCache = true;
                cachedCamId = camId;
                cachedGrassRendererId = grassId;
                cachedCenterPos = centerPos;
                cachedDataRadius = dataRadius;
                cachedBoundsYMinMax = boundsY;
            }

            bool hasMask = maskModifiers.Count > 0;
            bool hasColor = colorModifiers.Count > 0;
            bool needMaskUpdate = (needStaticUpdate && hasMask) || !maskClearedOnce;
            bool needColorUpdate = (needStaticUpdate && hasColor) || !colorClearedOnce;

            bool hasAnySlope = slopeModifiers.Count > 0;
            bool hasGlobalSlope = hasAnySlope && HasModifierOutsideBounds(slopeModifiers, localCenterPos, localRadius);
            bool needGlobalSlopeUpdate = hasGlobalSlope || globalSlopeHasContent;
            bool needLocalSlopeUpdate = hasAnySlope || localSlopeHasContent;

            Matrix4x4 camView = cameraData.GetViewMatrix();
            Matrix4x4 camProj = cameraData.GetProjectionMatrix();
            Matrix4x4 camVP = camProj * camView;
            GeometryUtility.CalculateFrustumPlanes(camVP, tempPlanes);

            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddUnsafePass<PassData>("Grass Data Pass", out var passData))
            {
                passData.heightTexture = renderGraph.ImportTexture(heightRT);
                passData.heightDepthTexture = renderGraph.ImportTexture(heightDepthRT);
                passData.maskTexture = renderGraph.ImportTexture(maskRT);
                passData.colorTexture = renderGraph.ImportTexture(colorRT);
                passData.slopeTexture = renderGraph.ImportTexture(slopeRT);
                passData.localSlopeTexture = renderGraph.ImportTexture(localSlopeRT);

                builder.UseTexture(passData.heightTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.heightDepthTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.maskTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.colorTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.slopeTexture, AccessFlags.ReadWrite);
                builder.UseTexture(passData.localSlopeTexture, AccessFlags.ReadWrite);

                passData.heightRTHandle = heightRT;
                passData.maskRTHandle = maskRT;

                passData.heightMapMat = heightMapMat;
                passData.computeShader = computeShader;
                passData.tileCullKernel = tileCullKernel;
                passData.spawnDenseKernel = spawnDenseKernel;
                passData.spawnSparseKernel = spawnSparseKernel;

                passData.viewMatrix = viewMatrix;
                passData.projectionMatrix = projectionMatrix;
                passData.localViewMatrix = localViewMatrix;
                passData.localProjectionMatrix = localProjMatrix;
                passData.originalViewMatrix = camView;
                passData.originalProjectionMatrix = camProj;
                passData.isTargetFlipped = cameraData.cameraType == CameraType.SceneView ||
                                           cameraData.targetTexture != null ||
                                           !resourceData.isActiveTargetBackBuffer;

                passData.centerPos = centerPos;
                passData.dataRadius = dataRadius;
                passData.localCenterPos = localCenterPos;
                passData.localSlopeRadius = localRadius;
                passData.slopeVerticalTolerance = grass.slopeVerticalTolerance;
                passData.boundsYMinMax = boundsY;

                passData.spacing = spacing;
                passData.fullDensityDistance = fullDensityDistance;
                passData.drawDistance = drawDistance;
                passData.tileStartIndex = tileStartIndex;
                passData.tileGridSize = tileGridSize;
                passData.cameraVPMatrix = camVP;
                passData.cameraPosition = cam.transform.position;

                passData.plane0 = PlaneToVector4(tempPlanes[0]);
                passData.plane1 = PlaneToVector4(tempPlanes[1]);
                passData.plane2 = PlaneToVector4(tempPlanes[2]);
                passData.plane3 = PlaneToVector4(tempPlanes[3]);
                passData.plane4 = PlaneToVector4(tempPlanes[4]);
                passData.plane5 = PlaneToVector4(tempPlanes[5]);

                passData.grassPositionsBuffer = grassPositionsBuffer;
                passData.denseTilesBuffer = denseTilesBuffer;
                passData.sparseTilesBuffer = sparseTilesBuffer;
                passData.denseArgsBuffer = denseArgsBuffer;
                passData.sparseArgsBuffer = sparseArgsBuffer;
                passData.argsBuffer = grass.argsBuffer;
                passData.tBuffer = grass.tBuffer;
                passData.previewVisibleGrassCount = grass.previewVisibleGrassCount;

                passData.updateHeight = needStaticUpdate;
                passData.updateMask = needMaskUpdate;
                passData.updateColor = needColorUpdate;
                passData.hasColor = hasColor;
                passData.updateGlobalSlope = needGlobalSlopeUpdate;
                passData.hasGlobalSlope = hasGlobalSlope;
                passData.updateLocalSlope = needLocalSlopeUpdate;
                passData.hasLocalSlope = hasAnySlope;

                passData.heightRenderers = heightRenderers;
                passData.maskModifiers = maskModifiers;
                passData.colorModifiers = colorModifiers;
                passData.slopeModifiers = slopeModifiers;
                passData.owner = this;

                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private static Vector4 PlaneToVector4(Plane p) => new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);

        private static void SetAllViewProjMatrices(CommandBuffer cmd, Matrix4x4 view, Matrix4x4 proj, bool renderIntoTexture)
        {
            cmd.SetViewProjectionMatrices(view, proj);
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, renderIntoTexture);
            Matrix4x4 vp = gpuProj * view;
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewMatrix, view);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewMatrix, view.inverse);
            cmd.SetGlobalMatrix(ShaderPropertyId.ProjectionMatrix, gpuProj);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseProjectionMatrix, gpuProj.inverse);
            cmd.SetGlobalMatrix(ShaderPropertyId.ViewAndProjectionMatrix, vp);
            cmd.SetGlobalMatrix(ShaderPropertyId.InverseViewAndProjectionMatrix, vp.inverse);
        }

        private static void DrawHeightRenderers(CommandBuffer cmd, List<Renderer> renderers, Material overrideMat, Vector2 center, float radius)
        {
            float minX = center.x - radius;
            float maxX = center.x + radius;
            float minZ = center.y - radius;
            float maxZ = center.y + radius;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                Bounds b = r.bounds;
                if (b.max.x < minX || b.min.x > maxX || b.max.z < minZ || b.min.z > maxZ)
                    continue;

                int subMeshCount = 1;
                if (r is MeshRenderer)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        subMeshCount = mf.sharedMesh.subMeshCount;
                }
                else if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                {
                    subMeshCount = smr.sharedMesh.subMeshCount;
                }

                for (int s = 0; s < subMeshCount; s++)
                    cmd.DrawRenderer(r, overrideMat, s, 0);
            }
        }

        private static void DrawModifiers(CommandBuffer cmd, List<ModifierEntry> modifiers, Vector2 center, float radius)
        {
            float minX = center.x - radius;
            float maxX = center.x + radius;
            float minZ = center.y - radius;
            float maxZ = center.y + radius;

            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierEntry entry = modifiers[i];
                Renderer r = entry.renderer;
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || entry.material == null)
                    continue;

                Bounds b = r.bounds;
                if (b.max.x < minX || b.min.x > maxX || b.max.z < minZ || b.min.z > maxZ)
                    continue;

                cmd.DrawRenderer(r, entry.material, entry.submeshIndex, -1);
            }
        }

        private static readonly Vector4[] ScratchFrustumPlanes = new Vector4[6];

        private static void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            bool renderedToCustomRT = data.updateHeight || data.updateMask || data.updateColor ||
                                      data.updateGlobalSlope || data.updateLocalSlope;

            cmd.SetGlobalVector(ShaderPropertyId.BoundsYMinMax, data.boundsYMinMax);
            cmd.SetGlobalVector(ShaderPropertyId.SlopeBoundsYMinMax, data.boundsYMinMax);

            if (data.updateHeight || data.updateMask || data.updateColor)
            {
                SetAllViewProjMatrices(cmd, data.viewMatrix, data.projectionMatrix, true);

                if (data.updateHeight)
                {
                    cmd.SetRenderTarget(data.heightTexture, data.heightDepthTexture);
                    cmd.ClearRenderTarget(true, true, Color.black);
                    DrawHeightRenderers(cmd, data.heightRenderers, data.heightMapMat, data.centerPos, data.dataRadius);
                }

                if (data.updateMask)
                {
                    cmd.SetRenderTarget(data.maskTexture);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    DrawModifiers(cmd, data.maskModifiers, data.centerPos, data.dataRadius);
                    data.owner.maskClearedOnce = true;
                }

                if (data.updateColor)
                {
                    cmd.SetRenderTarget(data.colorTexture);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    DrawModifiers(cmd, data.colorModifiers, data.centerPos, data.dataRadius);
                    data.owner.colorClearedOnce = true;
                }
            }

            if (data.updateGlobalSlope)
            {
                SetAllViewProjMatrices(cmd, data.viewMatrix, data.projectionMatrix, true);
                cmd.SetRenderTarget(data.slopeTexture);
                cmd.ClearRenderTarget(false, true, NeutralSlopeColor);
                if (data.hasGlobalSlope)
                    DrawModifiers(cmd, data.slopeModifiers, data.centerPos, data.dataRadius);
                data.owner.globalSlopeHasContent = data.hasGlobalSlope;
            }

            if (data.updateLocalSlope)
            {
                SetAllViewProjMatrices(cmd, data.localViewMatrix, data.localProjectionMatrix, true);
                cmd.SetRenderTarget(data.localSlopeTexture);
                cmd.ClearRenderTarget(false, true, NeutralSlopeColor);
                if (data.hasLocalSlope)
                    DrawModifiers(cmd, data.slopeModifiers, data.localCenterPos, data.localSlopeRadius);
                data.owner.localSlopeHasContent = data.hasLocalSlope;
            }

            if (renderedToCustomRT)
                SetAllViewProjMatrices(cmd, data.originalViewMatrix, data.originalProjectionMatrix, data.isTargetFlipped);

            cmd.SetGlobalVector(ShaderPropertyId.CenterPos, data.centerPos);
            cmd.SetGlobalFloat(ShaderPropertyId.GrassDataRadius, data.dataRadius);
            cmd.SetGlobalFloat(ShaderPropertyId.GrassDrawDistance, data.drawDistance);
            cmd.SetGlobalVector(ShaderPropertyId.LocalSlopeCenterPos, data.localCenterPos);
            cmd.SetGlobalFloat(ShaderPropertyId.LocalSlopeRadius, data.localSlopeRadius);
            cmd.SetGlobalFloat(ShaderPropertyId.HasLocalSlopeRT, data.hasLocalSlope ? 1f : 0f);
            cmd.SetGlobalFloat(ShaderPropertyId.HasGlobalSlopeRT, data.hasGlobalSlope ? 1f : 0f);
            cmd.SetGlobalFloat(ShaderPropertyId.HasColorRT, data.hasColor ? 1f : 0f);
            cmd.SetGlobalFloat(ShaderPropertyId.SlopeVerticalTolerance, data.slopeVerticalTolerance);

            cmd.SetGlobalTexture(ShaderPropertyId.GrassColorRT, data.colorTexture);
            cmd.SetGlobalTexture(ShaderPropertyId.GrassSlopeRT, data.slopeTexture);
            cmd.SetGlobalTexture(ShaderPropertyId.GrassLocalSlopeRT, data.localSlopeTexture);

            ComputeShader cs = data.computeShader;
            ScratchFrustumPlanes[0] = data.plane0;
            ScratchFrustumPlanes[1] = data.plane1;
            ScratchFrustumPlanes[2] = data.plane2;
            ScratchFrustumPlanes[3] = data.plane3;
            ScratchFrustumPlanes[4] = data.plane4;
            ScratchFrustumPlanes[5] = data.plane5;

            cmd.SetComputeFloatParam(cs, ShaderPropertyId.Spacing, data.spacing);
            cmd.SetComputeFloatParam(cs, ShaderPropertyId.DrawDistance, data.drawDistance);
            cmd.SetComputeFloatParam(cs, ShaderPropertyId.GrassDataRadius, data.dataRadius);
            cmd.SetComputeFloatParam(cs, ShaderPropertyId.FullDensityDistance, data.fullDensityDistance);
            cmd.SetComputeIntParams(cs, ShaderPropertyId.TileStartIndex, data.tileStartIndex.x, data.tileStartIndex.y);
            cmd.SetComputeIntParams(cs, ShaderPropertyId.TileGridSize, data.tileGridSize.x, data.tileGridSize.y);
            cmd.SetComputeVectorParam(cs, ShaderPropertyId.CameraPosition, data.cameraPosition);
            cmd.SetComputeVectorParam(cs, ShaderPropertyId.CenterPos, data.centerPos);
            cmd.SetComputeVectorParam(cs, ShaderPropertyId.BoundsMin, new Vector3(0f, data.boundsYMinMax.x, 0f));
            cmd.SetComputeVectorParam(cs, ShaderPropertyId.BoundsMax, new Vector3(0f, data.boundsYMinMax.y, 0f));
            cmd.SetComputeMatrixParam(cs, ShaderPropertyId.VPMatrix, data.cameraVPMatrix);
            cmd.SetComputeVectorArrayParam(cs, ShaderPropertyId.FrustumPlanes, ScratchFrustumPlanes);

            cmd.SetBufferCounterValue(data.denseTilesBuffer, 0);
            cmd.SetBufferCounterValue(data.sparseTilesBuffer, 0);
            cmd.SetBufferCounterValue(data.grassPositionsBuffer, 0);

            cmd.SetComputeBufferParam(cs, data.tileCullKernel, ShaderPropertyId.DenseTiles, data.denseTilesBuffer);
            cmd.SetComputeBufferParam(cs, data.tileCullKernel, ShaderPropertyId.SparseTiles, data.sparseTilesBuffer);
            cmd.DispatchCompute(
                cs,
                data.tileCullKernel,
                Mathf.CeilToInt(data.tileGridSize.x / 8f),
                Mathf.CeilToInt(data.tileGridSize.y / 8f),
                1
            );

            cmd.CopyCounterValue(data.denseTilesBuffer, data.denseArgsBuffer, 0);
            cmd.CopyCounterValue(data.sparseTilesBuffer, data.sparseArgsBuffer, 0);

            cmd.SetComputeBufferParam(cs, data.spawnDenseKernel, ShaderPropertyId.DenseTilesRO, data.denseTilesBuffer);
            cmd.SetComputeBufferParam(cs, data.spawnDenseKernel, ShaderPropertyId.GrassPositions, data.grassPositionsBuffer);
            cmd.SetComputeTextureParam(cs, data.spawnDenseKernel, ShaderPropertyId.GrassHeightMapRT, data.heightRTHandle);
            cmd.SetComputeTextureParam(cs, data.spawnDenseKernel, ShaderPropertyId.GrassMaskMapRT, data.maskRTHandle);
            cmd.DispatchCompute(cs, data.spawnDenseKernel, data.denseArgsBuffer, 0);

            cmd.SetComputeBufferParam(cs, data.spawnSparseKernel, ShaderPropertyId.SparseTilesRO, data.sparseTilesBuffer);
            cmd.SetComputeBufferParam(cs, data.spawnSparseKernel, ShaderPropertyId.GrassPositions, data.grassPositionsBuffer);
            cmd.SetComputeTextureParam(cs, data.spawnSparseKernel, ShaderPropertyId.GrassHeightMapRT, data.heightRTHandle);
            cmd.SetComputeTextureParam(cs, data.spawnSparseKernel, ShaderPropertyId.GrassMaskMapRT, data.maskRTHandle);
            cmd.DispatchCompute(cs, data.spawnSparseKernel, data.sparseArgsBuffer, 0);

            cmd.SetGlobalBuffer(ShaderPropertyId.GrassPositions, data.grassPositionsBuffer);

            if (data.argsBuffer != null)
                cmd.CopyCounterValue(data.grassPositionsBuffer, data.argsBuffer, 4);

            if (data.previewVisibleGrassCount && data.tBuffer != null)
                cmd.CopyCounterValue(data.grassPositionsBuffer, data.tBuffer, 0);
        }

        public void Dispose()
        {
            heightRT?.Release();
            heightDepthRT?.Release();
            maskRT?.Release();
            colorRT?.Release();
            slopeRT?.Release();
            localSlopeRT?.Release();

            grassPositionsBuffer?.Release();
            grassPositionsBuffer = null;
            denseTilesBuffer?.Release();
            denseTilesBuffer = null;
            sparseTilesBuffer?.Release();
            sparseTilesBuffer = null;
            denseArgsBuffer?.Release();
            denseArgsBuffer = null;
            sparseArgsBuffer?.Release();
            sparseArgsBuffer = null;

            hasStaticCache = false;
            maskClearedOnce = false;
            colorClearedOnce = false;
        }

        private static class ShaderPropertyId
        {
            public static readonly int ViewMatrix = Shader.PropertyToID("unity_MatrixV");
            public static readonly int InverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
            public static readonly int ProjectionMatrix = Shader.PropertyToID("unity_MatrixP");
            public static readonly int InverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
            public static readonly int ViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");
            public static readonly int InverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");

            public static readonly int BoundsYMinMax = Shader.PropertyToID("_BoundsYMinMax");
            public static readonly int SlopeBoundsYMinMax = Shader.PropertyToID("_SlopeBoundsYMinMax");
            public static readonly int SlopeVerticalTolerance = Shader.PropertyToID("_SlopeVerticalTolerance");

            public static readonly int CenterPos = Shader.PropertyToID("_CenterPos");
            public static readonly int GrassDataRadius = Shader.PropertyToID("_GrassDataRadius");
            public static readonly int GrassDrawDistance = Shader.PropertyToID("_GrassDrawDistance");
            public static readonly int LocalSlopeCenterPos = Shader.PropertyToID("_LocalSlopeCenterPos");
            public static readonly int LocalSlopeRadius = Shader.PropertyToID("_LocalSlopeRadius");
            public static readonly int HasLocalSlopeRT = Shader.PropertyToID("_HasLocalSlopeRT");
            public static readonly int HasGlobalSlopeRT = Shader.PropertyToID("_HasGlobalSlopeRT");
            public static readonly int HasColorRT = Shader.PropertyToID("_HasColorRT");

            public static readonly int GrassColorRT = Shader.PropertyToID("_GrassColorRT");
            public static readonly int GrassSlopeRT = Shader.PropertyToID("_GrassSlopeRT");
            public static readonly int GrassLocalSlopeRT = Shader.PropertyToID("_GrassLocalSlopeRT");

            public static readonly int Spacing = Shader.PropertyToID("_Spacing");
            public static readonly int DrawDistance = Shader.PropertyToID("_DrawDistance");
            public static readonly int FullDensityDistance = Shader.PropertyToID("_FullDensityDistance");
            public static readonly int TileStartIndex = Shader.PropertyToID("_TileStartIndex");
            public static readonly int TileGridSize = Shader.PropertyToID("_TileGridSize");
            public static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
            public static readonly int BoundsMin = Shader.PropertyToID("_BoundsMin");
            public static readonly int BoundsMax = Shader.PropertyToID("_BoundsMax");
            public static readonly int VPMatrix = Shader.PropertyToID("_VPMatrix");
            public static readonly int FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");

            public static readonly int DenseTiles = Shader.PropertyToID("_DenseTiles");
            public static readonly int SparseTiles = Shader.PropertyToID("_SparseTiles");
            public static readonly int DenseTilesRO = Shader.PropertyToID("_DenseTilesRO");
            public static readonly int SparseTilesRO = Shader.PropertyToID("_SparseTilesRO");
            public static readonly int GrassPositions = Shader.PropertyToID("_GrassPositions");
            public static readonly int GrassHeightMapRT = Shader.PropertyToID("_GrassHeightMapRT");
            public static readonly int GrassMaskMapRT = Shader.PropertyToID("_GrassMaskMapRT");
        }
    }
}
