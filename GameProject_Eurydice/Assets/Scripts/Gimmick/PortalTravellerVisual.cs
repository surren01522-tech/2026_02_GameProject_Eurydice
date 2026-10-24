using System.Collections.Generic;
using UnityEngine;

public sealed class PortalTravellerVisual
{
    private sealed class RendererPair
    {
        public Renderer sourceRenderer;
        public SkinnedMeshRenderer sourceSkinned;
        public Transform sourceTransform;
        public Transform cloneTransform;
        public MeshFilter cloneFilter;
        public MeshRenderer cloneRenderer;
        public Mesh bakedMesh;
        public Material[] originalMaterials;
        public Material[] realSliceMaterials;
        public Material[] cloneSliceMaterials;
    }

    private const float ActiveSliceRange = 1.2f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int SliceCenterId = Shader.PropertyToID("_PortalSliceCenter");
    private static readonly int SliceNormalId = Shader.PropertyToID("_PortalSliceNormal");

    private readonly List<RendererPair> pairs = new(4);
    private PlayerController boundPlayer;
    private GameObject cloneRoot;
    private Shader sliceShader;
    private bool isSliceActive;
    private float cachedNearSide = 1f;

    public PortalGateway ClonePortal { get; private set; }

    public void Sync(PlayerController player, PortalGateway splitExit, float splitExitSide, List<PortalGateway> activePortals)
    {
        if (player == null)
        {
            Deactivate();
            return;
        }

        PortalGateway nearPortal = null;
        float nearSide = 1f;

        if (splitExit != null && splitExit.IsOpen && splitExit.LinkedPortal != null)
        {
            nearPortal = splitExit;
            nearSide = splitExitSide;
        }
        else
        {
            Vector3 playerPos = player.transform.position;
            float bestAbsZ = ActiveSliceRange;

            for (int i = 0; i < activePortals.Count; i++)
            {
                var p = activePortals[i];
                if (p == null || !p.IsOpen || p.LinkedPortal == null) continue;

                Vector3 local = p.transform.InverseTransformPoint(playerPos);
                float absZ = Mathf.Abs(local.z);
                if (absZ >= bestAbsZ) continue;

                float halfW = Mathf.Max(0.4f, p.ApertureSize.x * 0.55f);
                float halfH = Mathf.Max(0.6f, p.ApertureSize.y * 0.65f);
                if (Mathf.Abs(local.x) > halfW || Mathf.Abs(local.y) > halfH) continue;

                bestAbsZ = absZ;
                nearPortal = p;
                if (Mathf.Abs(local.z) >= 0.05f)
                    cachedNearSide = Mathf.Sign(local.z);
                nearSide = cachedNearSide;
            }
        }

        if (nearPortal == null)
        {
            Deactivate();
            return;
        }

        EnsureRig(player);
        if (pairs.Count == 0)
        {
            Deactivate();
            return;
        }

        PortalGateway farPortal = nearPortal.LinkedPortal;
        bool mirror = nearPortal.MirrorForward;
        float farSide = mirror ? nearSide : -nearSide;
        ClonePortal = farPortal;

        if (!isSliceActive)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair.sourceRenderer != null)
                    pair.sourceRenderer.sharedMaterials = pair.realSliceMaterials;
            }
            if (cloneRoot != null) cloneRoot.SetActive(true);
            isSliceActive = true;
        }

        const float SliceOverlap = 0.005f;
        Vector3 nearNorm3 = nearSide * nearPortal.transform.forward;
        Vector4 nearCenter = nearPortal.transform.position - nearNorm3 * SliceOverlap;
        Vector4 nearNormal = new Vector4(nearNorm3.x, nearNorm3.y, nearNorm3.z, 1f);

        Vector3 farNorm3 = farSide * farPortal.transform.forward;
        Vector4 farCenter = farPortal.transform.position - farNorm3 * SliceOverlap;
        Vector4 farNormal = new Vector4(farNorm3.x, farNorm3.y, farNorm3.z, 1f);

        Quaternion rotDelta = PortalGateway.GetRotationDelta(nearPortal, farPortal, mirror);

        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            if (pair.sourceRenderer == null || pair.cloneRenderer == null) continue;

            bool active = pair.sourceRenderer.enabled && pair.sourceRenderer.gameObject.activeInHierarchy;
            pair.cloneRenderer.enabled = active;
            if (!active) continue;

            if (pair.sourceSkinned != null)
            {
                pair.sourceSkinned.BakeMesh(pair.bakedMesh, true);
                pair.cloneTransform.localScale = Vector3.one;
            }
            else
            {
                pair.cloneTransform.localScale = pair.sourceTransform.lossyScale;
            }

            Vector3 srcLocal = nearPortal.transform.InverseTransformPoint(pair.sourceTransform.position);
            Vector3 dstWorld = farPortal.transform.TransformPoint(PortalGateway.MapLocal(srcLocal, mirror));
            Quaternion dstRot = rotDelta * pair.sourceTransform.rotation;
            pair.cloneTransform.SetPositionAndRotation(dstWorld, dstRot);

            ApplySliceMaterials(pair.realSliceMaterials, nearCenter, nearNormal);
            ApplySliceMaterials(pair.cloneSliceMaterials, farCenter, farNormal);
        }
    }

    public void SetCloneVisible(bool visible)
    {
        if (cloneRoot != null && isSliceActive)
            cloneRoot.SetActive(visible);
    }

    public void Deactivate()
    {
        ClonePortal = null;
        if (!isSliceActive) return;

        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            if (pair.sourceRenderer != null && pair.originalMaterials != null)
                pair.sourceRenderer.sharedMaterials = pair.originalMaterials;
        }

        if (cloneRoot != null)
            cloneRoot.SetActive(false);

        isSliceActive = false;
        cachedNearSide = 1f;
    }

    public void Dispose()
    {
        Deactivate();

        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            DestroyMaterials(pair.realSliceMaterials);
            DestroyMaterials(pair.cloneSliceMaterials);
            if (pair.bakedMesh != null)
                Object.Destroy(pair.bakedMesh);
        }
        pairs.Clear();

        if (cloneRoot != null)
        {
            Object.Destroy(cloneRoot);
            cloneRoot = null;
        }

        boundPlayer = null;
    }

    private void EnsureRig(PlayerController player)
    {
        if (boundPlayer == player && cloneRoot != null && pairs.Count > 0) return;

        Dispose();

        if (sliceShader == null)
            sliceShader = Resources.Load<Shader>("PortalTravellerSlice");
        if (sliceShader == null) return;

        boundPlayer = player;
        cloneRoot = new GameObject("PortalPlayerClone") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(cloneRoot);
        cloneRoot.SetActive(false);

        var renderers = player.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var src = renderers[i];
            if (src == null) continue;

            Mesh sharedMesh = null;
            var skinned = src as SkinnedMeshRenderer;
            if (skinned != null)
            {
                sharedMesh = skinned.sharedMesh;
            }
            else if (src is MeshRenderer && src.TryGetComponent<MeshFilter>(out var mf))
            {
                sharedMesh = mf.sharedMesh;
            }

            if (sharedMesh == null) continue;

            var child = new GameObject($"Clone_{src.name}")
            {
                layer = src.gameObject.layer,
                hideFlags = HideFlags.HideAndDontSave
            };
            child.transform.SetParent(cloneRoot.transform, false);

            var cloneMf = child.AddComponent<MeshFilter>();
            var cloneMr = child.AddComponent<MeshRenderer>();
            cloneMr.shadowCastingMode = src.shadowCastingMode;
            cloneMr.receiveShadows = src.receiveShadows;
            cloneMr.lightProbeUsage = src.lightProbeUsage;

            Mesh baked = null;
            if (skinned != null)
            {
                baked = new Mesh { name = $"Baked_{src.name}", hideFlags = HideFlags.DontSave };
                cloneMf.sharedMesh = baked;
            }
            else
            {
                cloneMf.sharedMesh = sharedMesh;
            }

            Material[] origMats = src.sharedMaterials;
            Material[] realMats = CreateSliceMaterials(origMats, sliceShader);
            Material[] cloneMats = CreateSliceMaterials(origMats, sliceShader);
            cloneMr.sharedMaterials = cloneMats;

            pairs.Add(new RendererPair
            {
                sourceRenderer = src,
                sourceSkinned = skinned,
                sourceTransform = src.transform,
                cloneTransform = child.transform,
                cloneFilter = cloneMf,
                cloneRenderer = cloneMr,
                bakedMesh = baked,
                originalMaterials = origMats,
                realSliceMaterials = realMats,
                cloneSliceMaterials = cloneMats
            });
        }
    }

    private static Material[] CreateSliceMaterials(Material[] sourceMats, Shader shader)
    {
        var result = new Material[sourceMats.Length];
        for (int i = 0; i < sourceMats.Length; i++)
        {
            Material src = sourceMats[i];
            var mat = new Material(shader) { hideFlags = HideFlags.DontSave };

            if (src != null)
            {
                Texture tex = src.HasProperty(BaseMapId) ? src.GetTexture(BaseMapId) :
                              (src.HasProperty(MainTexId) ? src.GetTexture(MainTexId) : null);
                if (tex != null)
                {
                    mat.SetTexture(BaseMapId, tex);
                    string prop = src.HasProperty(BaseMapId) ? "_BaseMap" : "_MainTex";
                    mat.SetTextureScale(BaseMapId, src.GetTextureScale(prop));
                    mat.SetTextureOffset(BaseMapId, src.GetTextureOffset(prop));
                }

                Color col = src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) :
                            (src.HasProperty(ColorId) ? src.GetColor(ColorId) : Color.white);
                mat.SetColor(BaseColorId, col);

                if (src.HasProperty(SmoothnessId))
                    mat.SetFloat(SmoothnessId, src.GetFloat(SmoothnessId));
            }

            result[i] = mat;
        }
        return result;
    }

    private static void ApplySliceMaterials(Material[] mats, Vector4 center, Vector4 normal)
    {
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            mats[i].SetVector(SliceCenterId, center);
            mats[i].SetVector(SliceNormalId, normal);
        }
    }

    private static void DestroyMaterials(Material[] mats)
    {
        if (mats == null) return;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] != null)
                Object.Destroy(mats[i]);
        }
    }
}
