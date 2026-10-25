using Unity.Cinemachine;
using UnityEngine;

public static class PortalCameraController
{
    private struct SplitSession
    {
        public bool active;
        public PortalGateway entry;
        public PortalGateway exit;
        public PlayerController player;
        public float entrySide;
        public float exitSide;
        public bool mirror;
        public Vector3 lastGhostWorld;
        public float funnelPull;
        public string pinnedEntryScene;
        public string pinnedExitScene;
    }

    private const float PortalCamNearClip = 0.01f;
    private const float SplitCrossDist = 0.025f;
    private const float FrontCameraClearance = 0.045f;
    private const float MinPlayerClearance = 0.005f;

    private static SplitSession split;
    private static bool isBrainHooked;
    private static bool isNearAnyPortal;

    public static bool IsSplitActive => split.active;
    public static PortalGateway SplitExit => split.exit;
    public static float SplitExitSide => split.exitSide;
    public static PortalGateway SplitEntry => split.entry;

    public static void HookBrain()
    {
        if (Application.isPlaying && !isBrainHooked)
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnBrainUpdated);
            isBrainHooked = true;
        }
    }

    public static void UnhookBrain()
    {
        if (isBrainHooked)
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnBrainUpdated);
            isBrainHooked = false;
        }
        isNearAnyPortal = false;
    }

    public static void BeginSplit(PortalGateway entry, PortalGateway exit, PlayerController player, float entrySide, float exitSide, bool mirror, Vector3 targetPos, Quaternion targetRot, Quaternion rotDelta)
    {
        var camMode = player.CameraModeController;
        if (camMode == null) return;

        string entryScene = entry.gameObject.scene.name;
        string exitScene = exit.gameObject.scene.name;
        bool crossScene = entryScene != exitScene;

        if (crossScene)
        {
            SceneStreamer.Instance?.PinScene(entryScene);
            SceneStreamer.Instance?.PinScene(exitScene);
        }

        player.Warp(targetPos, targetRot);
        player.SetCameraBasisOffset(rotDelta);

        Transform realTarget = camMode.RealTarget != null ? camMode.RealTarget : player.transform;
        Vector3 ghostPos = entry.transform.TransformPoint(PortalGateway.MapLocal(exit.transform.InverseTransformPoint(realTarget.position), mirror));
        Quaternion ghostRot = PortalGateway.GetRotationDelta(exit, entry, mirror) * realTarget.rotation;

        camMode.BeginSplit(ghostPos, ghostRot);

        split = new SplitSession
        {
            active = true,
            entry = entry,
            exit = exit,
            player = player,
            entrySide = entrySide,
            exitSide = exitSide,
            mirror = mirror,
            lastGhostWorld = ghostPos,
            funnelPull = 0f,
            pinnedEntryScene = crossScene ? entryScene : null,
            pinnedExitScene = crossScene ? exitScene : null
        };
    }

    public static void CancelSplit(Vector3 exitCurrLocal)
    {
        if (!split.active) return;

        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        PlayerController player = split.player;

        Vector3 returnLocal = PortalGateway.MapLocal(exitCurrLocal, split.mirror);
        if (returnLocal.z * split.entrySide < MinPlayerClearance)
            returnLocal.z = split.entrySide * MinPlayerClearance;

        Vector3 returnPos = entry.transform.TransformPoint(returnLocal);
        Quaternion returnRot = PortalGateway.GetRotationDelta(exit, entry, split.mirror) * player.transform.rotation;

        entry.SetLastTeleportTime(Time.time);
        exit.SetLastTeleportTime(Time.time);

        player.ClearCameraBasisOffset();
        player.Warp(returnPos, returnRot);
        player.CameraModeController?.EndSplit(Camera.main);

        entry.SetProximityFlatten(0f);
        CleanupSplit();
        SceneStreamer.Instance?.RefreshZonesForPosition(returnPos);
    }

    public static void CompleteSplit(Camera mainCam)
    {
        if (!split.active) return;

        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        PlayerController player = split.player;
        var camMode = player != null ? player.CameraModeController : null;
        Transform camT = mainCam != null ? mainCam.transform : (player != null ? player.CameraTransform : null);

        if (camT != null && entry != null && exit != null)
        {
            Vector3 inLocal = entry.transform.InverseTransformPoint(camT.position);
            float maxW = Mathf.Max(0.35f, entry.ApertureSize.x * 0.45f);
            float maxH = Mathf.Max(0.5f, entry.ApertureSize.y * 0.48f);
            inLocal.x = Mathf.Clamp(inLocal.x, -maxW, maxW);
            inLocal.y = Mathf.Clamp(inLocal.y, -maxH, maxH);

            Vector3 outLocal = PortalGateway.MapLocal(inLocal, split.mirror);
            float minOutZ = Mathf.Max(FrontCameraClearance, exit.ExitClearance);
            if (outLocal.z * split.exitSide < minOutZ)
                outLocal.z = split.exitSide * minOutZ;

            Vector3 targetCamPos = exit.transform.TransformPoint(outLocal);
            Quaternion targetCamRot = PortalGateway.GetRotationDelta(entry, exit, split.mirror) * camT.rotation;

            if (camMode != null)
            {
                isNearAnyPortal = true;
                camMode.SetNearPortalClip(true, mainCam);
                camMode.EndSplit(mainCam);

                float yaw = targetCamRot.eulerAngles.y;
                float pitch = targetCamRot.eulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
                camMode.ForceCameraPose(targetCamPos, targetCamRot, yaw, pitch, cutDamping: true);
            }

            camT.SetPositionAndRotation(targetCamPos, targetCamRot);
        }
        else
        {
            camMode?.EndSplit(mainCam);
        }

        if (player != null)
            player.ClearCameraBasisOffset();

        if (entry != null)
            entry.SetProximityFlatten(0f);

        CleanupSplit();
    }

    public static void UpdateSplitGhost()
    {
        if (!split.active) return;

        var camMode = split.player != null ? split.player.CameraModeController : null;
        if (camMode == null) return;

        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        Transform realTarget = camMode.RealTarget != null ? camMode.RealTarget : split.player.transform;

        Vector3 ghostLocal = PortalGateway.MapLocal(exit.transform.InverseTransformPoint(realTarget.position), split.mirror);
        Quaternion ghostRot = PortalGateway.GetRotationDelta(exit, entry, split.mirror) * realTarget.rotation;

        Transform camT = split.player.CameraTransform;
        if (camT != null)
        {
            Vector3 camLocal = entry.transform.InverseTransformPoint(camT.position);
            float innerW = Mathf.Max(0.35f, entry.ApertureSize.x * 0.4f);
            float innerH = Mathf.Max(0.5f, entry.ApertureSize.y * 0.42f);

            float dz = camLocal.z - ghostLocal.z;
            float tPlane = Mathf.Abs(dz) > 0.0001f ? Mathf.Clamp01(camLocal.z / dz) : 0f;
            float hitX = Mathf.Lerp(camLocal.x, ghostLocal.x, tPlane);
            float hitY = Mathf.Lerp(camLocal.y, ghostLocal.y, tPlane);

            if (Mathf.Abs(hitX) > innerW || Mathf.Abs(hitY) > innerH)
            {
                split.funnelPull = Mathf.Clamp01(split.funnelPull + Time.deltaTime * 3.5f);
                Vector3 clampedAperture = new Vector3(
                    Mathf.Clamp(hitX, -innerW * 0.8f, innerW * 0.8f),
                    Mathf.Clamp(hitY, -innerH * 0.8f, innerH * 0.8f),
                    -split.entrySide * 0.15f
                );
                Vector3 orbitLocal = entry.transform.InverseTransformVector(camT.position - split.lastGhostWorld);
                ghostLocal = Vector3.Lerp(ghostLocal, clampedAperture - orbitLocal, split.funnelPull);
            }
            else
            {
                split.funnelPull = Mathf.MoveTowards(split.funnelPull, 0f, Time.deltaTime * 4f);
            }
        }

        Vector3 ghostWorld = entry.transform.TransformPoint(ghostLocal);
        split.lastGhostWorld = ghostWorld;
        camMode.UpdateGhost(ghostWorld, ghostRot);
    }

    private static void OnBrainUpdated(CinemachineBrain brain)
    {
        if (brain == null) return;
        Camera outCam = brain.OutputCamera;
        if (outCam == null || outCam != Camera.main) return;

        PlayerController trackedPlayer = PortalManager.TrackedPlayer;
        Transform camT = outCam.transform;

        if (split.active)
        {
            Vector3 camLocal = split.entry.transform.InverseTransformPoint(camT.position);
            if (camLocal.z * split.entrySide <= SplitCrossDist)
                CompleteSplit(outCam);

            UpdateNearPortalClipState(outCam, camT.position);
            return;
        }

        if (trackedPlayer != null)
            GuardPortalCameraPlane(camT, trackedPlayer);

        UpdateNearPortalClipState(outCam, camT.position);
    }

    private static void GuardPortalCameraPlane(Transform camT, PlayerController player)
    {
        var camMode = player.CameraModeController;
        Transform targetT = (camMode != null && camMode.RealTarget != null) ? camMode.RealTarget : player.transform;
        Vector3 targetWorld = targetT.position;
        var activePortals = PortalManager.ActivePortals;

        for (int i = 0; i < activePortals.Count; i++)
        {
            var p = activePortals[i];
            if (p == null || !p.IsOpen || p.LinkedPortal == null) continue;
            if ((targetWorld - p.transform.position).sqrMagnitude > 81f) continue;

            Vector3 targetLocal = p.transform.InverseTransformPoint(targetWorld);
            Vector3 camLocal = p.transform.InverseTransformPoint(camT.position);
            float side = targetLocal.z >= 0f ? 1f : -1f;
            float targetSignedZ = targetLocal.z * side;
            float camSignedZ = camLocal.z * side;

            float halfW = p.ApertureSize.x * 0.52f;
            float halfH = p.ApertureSize.y * 0.52f;

            if (targetSignedZ > FrontCameraClearance + 0.02f && camSignedZ < FrontCameraClearance)
            {
                float planeZ = side * FrontCameraClearance;
                float dz = camLocal.z - targetLocal.z;
                if (Mathf.Abs(dz) < 0.0001f) continue;

                float tHit = (planeZ - targetLocal.z) / dz;
                Vector3 hitLocal = Vector3.Lerp(targetLocal, camLocal, tHit);

                float weightX = 1f - Mathf.InverseLerp(halfW, halfW + 0.30f, Mathf.Abs(hitLocal.x));
                float weightY = 1f - Mathf.InverseLerp(halfH, halfH + 0.30f, Mathf.Abs(hitLocal.y));
                float weight = weightX * weightY;
                if (weight <= 0.001f) continue;

                Vector3 clampedLocal = Vector3.Lerp(camLocal, hitLocal, weight);
                if (Mathf.Abs(clampedLocal.x) < halfW && Mathf.Abs(clampedLocal.y) < halfH && clampedLocal.z * side < FrontCameraClearance)
                    clampedLocal.z = planeZ;

                camT.position = p.transform.TransformPoint(clampedLocal);
            }
        }
    }

    private static void UpdateNearPortalClipState(Camera outCam, Vector3 camWorldPos)
    {
        PlayerController trackedPlayer = PortalManager.TrackedPlayer;
        if (trackedPlayer == null || trackedPlayer.CameraModeController == null) return;

        float threshold = isNearAnyPortal ? 1.05f : 0.85f;
        bool near = false;
        var activePortals = PortalManager.ActivePortals;

        for (int i = 0; i < activePortals.Count; i++)
        {
            var p = activePortals[i];
            if (p == null || !p.IsOpen) continue;

            Vector3 local = p.transform.InverseTransformPoint(camWorldPos);
            if (Mathf.Abs(local.z) < threshold &&
                Mathf.Abs(local.x) < p.ApertureSize.x * 0.75f + 0.5f &&
                Mathf.Abs(local.y) < p.ApertureSize.y * 0.75f + 0.5f)
            {
                near = true;
                break;
            }
        }

        if (near != isNearAnyPortal)
        {
            isNearAnyPortal = near;
            trackedPlayer.CameraModeController.SetNearPortalClip(near, outCam);
        }
        else if (near || split.active)
        {
            outCam.nearClipPlane = PortalCamNearClip;
        }
    }

    private static void CleanupSplit()
    {
        if (!string.IsNullOrEmpty(split.pinnedEntryScene))
            SceneStreamer.Instance?.UnpinScene(split.pinnedEntryScene);
        if (!string.IsNullOrEmpty(split.pinnedExitScene))
            SceneStreamer.Instance?.UnpinScene(split.pinnedExitScene);

        split = default;
    }
}
