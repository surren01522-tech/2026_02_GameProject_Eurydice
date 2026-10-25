using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class PortalManager
{
    private const float FrontCameraClearance = 0.045f;
    private const float MinPlayerClearance = 0.005f;

    private static readonly Dictionary<string, PortalGateway> PortalsById = new();
    private static readonly List<PortalGateway> ActivePortalsList = new();
    private static readonly Plane[] FrustumPlanes = new Plane[6];
    private static readonly List<Renderer> HiddenRenderers = new(16);
    private static readonly PortalTravellerVisual TravellerVisual = new();

    private static PlayerController trackedPlayer;
    private static Vector3 prevPlayerPos;
    private static bool hasPrevPlayerPos;
    private static int lastSteppedFrame = -1;
    private static bool isRenderingAnyPortal;

    public static IReadOnlyList<PortalGateway> ActivePortals => ActivePortalsList;
    public static PlayerController TrackedPlayer => trackedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        ActivePortalsList.Clear();
        PortalsById.Clear();
        UnhookPipeline();
        HookPipeline();
    }

    public static void Register(PortalGateway portal)
    {
        if (portal == null) return;

        if (!ActivePortalsList.Contains(portal))
            ActivePortalsList.Add(portal);

        HookPipeline();

        if (!string.IsNullOrEmpty(portal.PortalId))
            PortalsById[portal.PortalId] = portal;

        AutoLink(portal);
    }

    public static void Unregister(PortalGateway portal)
    {
        if (portal == null) return;

        if (!string.IsNullOrEmpty(portal.PortalId) && PortalsById.TryGetValue(portal.PortalId, out var existing) && existing == portal)
            PortalsById.Remove(portal.PortalId);

        ActivePortalsList.Remove(portal);

        if (ActivePortalsList.Count == 0)
        {
            UnhookPipeline();
            PortalCameraController.UnhookBrain();
            if (trackedPlayer != null)
                trackedPlayer.CameraModeController?.SetNearPortalClip(false, Camera.main);

            TravellerVisual.Dispose();
            trackedPlayer = null;
            hasPrevPlayerPos = false;
            lastSteppedFrame = -1;
        }
    }

    public static PortalGateway GetPortalById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        PortalsById.TryGetValue(id, out var portal);
        return portal;
    }

    private static void HookPipeline()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        PortalCameraController.HookBrain();
    }

    private static void UnhookPipeline()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    public static void UpdateManager()
    {
        if (lastSteppedFrame == Time.frameCount) return;
        lastSteppedFrame = Time.frameCount;

        if (trackedPlayer == null)
        {
            trackedPlayer = Object.FindFirstObjectByType<PlayerController>();
            hasPrevPlayerPos = false;
            if (trackedPlayer == null) return;
        }

        Vector3 currPos = trackedPlayer.transform.position;
        if (!hasPrevPlayerPos)
        {
            prevPlayerPos = currPos;
            hasPrevPlayerPos = true;
            TravellerVisual.Sync(trackedPlayer, null, 1f, ActivePortalsList);
            return;
        }

        if (PortalCameraController.IsSplitActive)
        {
            PortalGateway entry = PortalCameraController.SplitEntry;
            PortalGateway exit = PortalCameraController.SplitExit;

            if (entry == null || exit == null || !entry.IsOpen || !exit.IsOpen)
            {
                PortalCameraController.CompleteSplit(Camera.main);
            }
            else if (TryDetectDirectionalCross(exit, prevPlayerPos, currPos, PortalCameraController.SplitExitSide, out Vector3 exitCurrLocal))
            {
                PortalCameraController.CancelSplit(exitCurrLocal);
            }
            else
            {
                for (int i = 0; i < ActivePortalsList.Count; i++)
                {
                    var p = ActivePortalsList[i];
                    if (p == null || p == exit || !p.IsOpen || p.LinkedPortal == null) continue;
                    if (Time.time - p.LastTeleportTime < p.TeleportCooldown) continue;

                    if (TryDetectCross(p, prevPlayerPos, currPos, out float side, out Vector3 currLocal))
                    {
                        PortalCameraController.CompleteSplit(Camera.main);
                        BeginCross(p, trackedPlayer, side, currLocal);
                        break;
                    }
                }
            }

            if (PortalCameraController.IsSplitActive)
                PortalCameraController.UpdateSplitGhost();
        }
        else
        {
            for (int i = 0; i < ActivePortalsList.Count; i++)
            {
                var p = ActivePortalsList[i];
                if (p == null || !p.IsOpen || p.LinkedPortal == null) continue;
                if (Time.time - p.LastTeleportTime < p.TeleportCooldown) continue;

                if (TryDetectCross(p, prevPlayerPos, currPos, out float side, out Vector3 currLocal))
                {
                    BeginCross(p, trackedPlayer, side, currLocal);
                    break;
                }
            }
        }

        prevPlayerPos = trackedPlayer.transform.position;
        TravellerVisual.Sync(trackedPlayer, PortalCameraController.IsSplitActive ? PortalCameraController.SplitExit : null, PortalCameraController.SplitExitSide, ActivePortalsList);
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (isRenderingAnyPortal) return;
        if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
        if (cam != Camera.main && cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;

        GeometryUtility.CalculateFrustumPlanes(cam, FrustumPlanes);

        for (int i = 0; i < ActivePortalsList.Count; i++)
        {
            var p = ActivePortalsList[i];
            if (p == null || !p.IsOpen || p.LinkedPortal == null || p.PortalRenderer == null || !p.PortalRenderer.enabled) continue;
            if (cam == p.PortalCamera) continue;

            bool isSplitEntry = Application.isPlaying && PortalCameraController.IsSplitActive && PortalCameraController.SplitEntry == p && cam == Camera.main;

            if (!isSplitEntry)
            {
                float sqrDist = (cam.transform.position - p.transform.position).sqrMagnitude;
                if (sqrDist > p.MaxRenderDistance * p.MaxRenderDistance) continue;
                if (!GeometryUtility.TestPlanesAABB(FrustumPlanes, p.PortalRenderer.bounds)) continue;
            }

            isRenderingAnyPortal = true;
            try
            {
                p.RenderPortalView(cam, isSplitEntry);
            }
            finally
            {
                isRenderingAnyPortal = false;
            }
        }
    }

    public static void HideAllPortalRenderers()
    {
        HiddenRenderers.Clear();
        for (int i = 0; i < ActivePortalsList.Count; i++)
        {
            var p = ActivePortalsList[i];
            if (p != null && p.PortalRenderer != null && p.PortalRenderer.enabled)
            {
                p.PortalRenderer.enabled = false;
                HiddenRenderers.Add(p.PortalRenderer);
            }
        }
    }

    public static void RestorePortalRenderers()
    {
        for (int i = 0; i < HiddenRenderers.Count; i++)
        {
            if (HiddenRenderers[i] != null)
                HiddenRenderers[i].enabled = true;
        }
        HiddenRenderers.Clear();
    }

    private static void BeginCross(PortalGateway entry, PlayerController player, float entrySide, Vector3 currLocal)
    {
        PortalGateway exit = entry.LinkedPortal;
        if (player == null || exit == null) return;

        bool mirror = entry.MirrorForward;
        float exitSide = mirror ? entrySide : -entrySide;

        Vector3 outLocal = PortalGateway.MapLocal(currLocal, mirror);
        if (outLocal.z * exitSide < MinPlayerClearance)
            outLocal.z = exitSide * MinPlayerClearance;

        Vector3 targetPos = exit.transform.TransformPoint(outLocal);
        Quaternion rotDelta = PortalGateway.GetRotationDelta(entry, exit, mirror);
        Quaternion targetRot = rotDelta * player.transform.rotation;

        entry.SetLastTeleportTime(Time.time);
        exit.SetLastTeleportTime(Time.time);

        Transform camT = player.CameraTransform;
        var camMode = player.CameraModeController;
        bool shouldSplit = camT != null && camMode != null &&
                           entry.transform.InverseTransformPoint(camT.position).z * entrySide > 0f;

        if (shouldSplit)
        {
            PortalCameraController.BeginSplit(entry, exit, player, entrySide, exitSide, mirror, targetPos, targetRot, rotDelta);
        }
        else
        {
            Vector3 camOutLocal = camT != null ? PortalGateway.MapLocal(entry.transform.InverseTransformPoint(camT.position), mirror) : outLocal;
            float minZ = Mathf.Max(FrontCameraClearance, exit.ExitClearance);
            if (camOutLocal.z * exitSide < minZ)
                camOutLocal.z = exitSide * minZ;

            Vector3 targetCamPos = exit.transform.TransformPoint(camOutLocal);
            Quaternion targetCamRot = camT != null ? rotDelta * camT.rotation : targetRot;
            player.Teleport(targetPos, targetRot, targetCamPos, targetCamRot);
        }

        SceneStreamer.Instance?.RefreshZonesForPosition(targetPos);
        entry.PlayTeleportAudioAndEvent(targetPos);
    }

    private static bool TryDetectCross(PortalGateway portal, Vector3 prevWorld, Vector3 currWorld, out float entrySide, out Vector3 currLocal)
    {
        Vector3 prevLocal = portal.transform.InverseTransformPoint(prevWorld);
        currLocal = portal.transform.InverseTransformPoint(currWorld);
        entrySide = Mathf.Abs(prevLocal.z) > 0.0001f ? Mathf.Sign(prevLocal.z) : 1f;

        bool crossed = (prevLocal.z < 0f && currLocal.z >= 0f) || (prevLocal.z > 0f && currLocal.z <= 0f);
        if (!crossed) return false;

        return IsWithinAperture(portal, prevLocal, currLocal);
    }

    private static bool TryDetectDirectionalCross(PortalGateway portal, Vector3 prevWorld, Vector3 currWorld, float fromSide, out Vector3 currLocal)
    {
        Vector3 prevLocal = portal.transform.InverseTransformPoint(prevWorld);
        currLocal = portal.transform.InverseTransformPoint(currWorld);

        if (prevLocal.z * fromSide <= 0f || currLocal.z * fromSide > 0f)
            return false;

        return IsWithinAperture(portal, prevLocal, currLocal);
    }

    private static bool IsWithinAperture(PortalGateway portal, Vector3 prevLocal, Vector3 currLocal)
    {
        float dz = currLocal.z - prevLocal.z;
        if (Mathf.Abs(dz) < 0.00001f) return false;

        Vector3 hit = Vector3.Lerp(prevLocal, currLocal, Mathf.Clamp01(-prevLocal.z / dz));
        float halfW = Mathf.Max(0.4f, portal.ApertureSize.x * 0.55f);
        float halfH = Mathf.Max(0.6f, portal.ApertureSize.y * 0.65f);
        return Mathf.Abs(hit.x) <= halfW && Mathf.Abs(hit.y) <= halfH;
    }

    private static void AutoLink(PortalGateway portal)
    {
        if (!string.IsNullOrEmpty(portal.TargetPortalId) && PortalsById.TryGetValue(portal.TargetPortalId, out var target) && target != null && target != portal)
        {
            portal.SetLinkedPortal(target);
            if (target.LinkedPortal == null && !string.IsNullOrEmpty(target.TargetPortalId) && target.TargetPortalId == portal.PortalId)
                target.SetLinkedPortal(portal);
        }

        foreach (var kvp in PortalsById)
        {
            var other = kvp.Value;
            if (other != null && other != portal && !string.IsNullOrEmpty(portal.PortalId) && other.TargetPortalId == portal.PortalId)
            {
                other.SetLinkedPortal(portal);
                if (portal.LinkedPortal == null && !string.IsNullOrEmpty(portal.TargetPortalId) && portal.TargetPortalId == other.PortalId)
                    portal.SetLinkedPortal(other);
            }
        }
    }
}
