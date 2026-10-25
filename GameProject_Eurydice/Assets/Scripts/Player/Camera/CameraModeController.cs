using UnityEngine;
using Unity.Cinemachine;

public class CameraModeController : MonoBehaviour
{
    private const float PortalNearClip = 0.01f;

    [Header("시네머신 카메라")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera strafeCamera;

    private Transform ghostTarget;
    private Transform realTarget;
    private float savedFreeNearClip = 0.3f;
    private float savedStrafeNearClip = 0.3f;
    private bool hasSavedNearClip;
    private bool isSplit;
    private bool isNearPortal;

    public CinemachineCamera ActiveCamera =>
        (freeLookCamera != null && freeLookCamera.Priority.Value >= (strafeCamera != null ? strafeCamera.Priority.Value : 0))
            ? freeLookCamera
            : strafeCamera;

    public Transform RealTarget => isSplit ? realTarget : (ActiveCamera != null ? ActiveCamera.Target.TrackingTarget : null);
    public bool IsSplit => isSplit;

    private void Awake()
    {
        FindCameras();
        CacheDefaultNearClip();
    }

    private void OnDestroy()
    {
        if (ghostTarget != null)
            Destroy(ghostTarget.gameObject);
    }

    private void FindCameras()
    {
        if (freeLookCamera != null && strafeCamera != null) return;

        var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam == null) continue;
            string camName = cam.gameObject.name.ToLower();
            if (freeLookCamera == null && camName.Contains("free"))
                freeLookCamera = cam;
            else if (strafeCamera == null && camName.Contains("strafe"))
                strafeCamera = cam;
        }

        if (freeLookCamera == null && cameras.Length > 0)
            freeLookCamera = cameras[0];
        if (strafeCamera == null && cameras.Length > 1)
            strafeCamera = cameras[1];
    }

    private void CacheDefaultNearClip()
    {
        if (hasSavedNearClip) return;
        if (freeLookCamera != null) savedFreeNearClip = freeLookCamera.Lens.NearClipPlane;
        if (strafeCamera != null) savedStrafeNearClip = strafeCamera.Lens.NearClipPlane;
        hasSavedNearClip = freeLookCamera != null || strafeCamera != null;
    }

    public void SetCameraMode(ControlMode mode)
    {
        bool isFreeLook = mode == ControlMode.FreeLook;
        if (freeLookCamera != null) freeLookCamera.Priority.Value = isFreeLook ? 10 : 0;
        if (strafeCamera != null) strafeCamera.Priority.Value = isFreeLook ? 0 : 10;
    }

    public void WarpCameras(Transform target, Vector3 delta)
    {
        if (target == null) return;
        if (freeLookCamera != null) freeLookCamera.OnTargetObjectWarped(target, delta);
        if (strafeCamera != null) strafeCamera.OnTargetObjectWarped(target, delta);
    }

    public void SetCameraOrientation(float yaw, float pitch)
    {
        ApplyOrientation(freeLookCamera, yaw, pitch);
        ApplyOrientation(strafeCamera, yaw, pitch);
    }

    public void ForceCameraPose(Vector3 worldPos, Quaternion worldRot, float yaw, float pitch, bool cutDamping = true)
    {
        ForcePose(freeLookCamera, worldPos, worldRot, yaw, pitch, cutDamping);
        ForcePose(strafeCamera, worldPos, worldRot, yaw, pitch, cutDamping);
    }

    public void SetNearPortalClip(bool nearPortal, Camera outputCam = null)
    {
        CacheDefaultNearClip();
        isNearPortal = nearPortal;
        RefreshLensNearClip(outputCam);
    }

    public void BeginSplit(Vector3 ghostPos, Quaternion ghostRot)
    {
        CacheDefaultNearClip();
        EnsureGhostTarget();

        if (!isSplit)
        {
            var active = ActiveCamera;
            realTarget = active != null ? active.Target.TrackingTarget : transform;
            isSplit = true;
        }

        ghostTarget.SetPositionAndRotation(ghostPos, ghostRot);
        ConfigureVcamTarget(freeLookCamera, ghostTarget);
        ConfigureVcamTarget(strafeCamera, ghostTarget);
        RefreshLensNearClip(Camera.main);
    }

    public void UpdateGhost(Vector3 ghostPos, Quaternion ghostRot)
    {
        if (ghostTarget != null)
            ghostTarget.SetPositionAndRotation(ghostPos, ghostRot);
    }

    public void EndSplit(Camera outputCam = null)
    {
        if (!isSplit) return;
        isSplit = false;

        ConfigureVcamTarget(freeLookCamera, realTarget);
        ConfigureVcamTarget(strafeCamera, realTarget);
        RefreshLensNearClip(outputCam);
    }

    private void RefreshLensNearClip(Camera outputCam)
    {
        bool useShortClip = isSplit || isNearPortal;
        SetLensNearClip(freeLookCamera, useShortClip ? PortalNearClip : savedFreeNearClip);
        SetLensNearClip(strafeCamera, useShortClip ? PortalNearClip : savedStrafeNearClip);

        if (outputCam != null)
        {
            float defaultClip = ActiveCamera == strafeCamera ? savedStrafeNearClip : savedFreeNearClip;
            outputCam.nearClipPlane = useShortClip ? PortalNearClip : defaultClip;
        }
    }

    private void EnsureGhostTarget()
    {
        if (ghostTarget != null) return;
        var go = new GameObject("PortalGhostTarget") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        ghostTarget = go.transform;
    }

    private static void ConfigureVcamTarget(CinemachineCamera vcam, Transform trackingTarget)
    {
        if (vcam == null) return;
        vcam.Target.TrackingTarget = trackingTarget;
    }

    private static void SetLensNearClip(CinemachineCamera vcam, float nearClip)
    {
        if (vcam == null) return;
        var lens = vcam.Lens;
        lens.NearClipPlane = nearClip;
        vcam.Lens = lens;
    }

    private static void ForcePose(CinemachineCamera vcam, Vector3 worldPos, Quaternion worldRot, float yaw, float pitch, bool cutDamping)
    {
        if (vcam == null) return;

        vcam.ForceCameraPosition(worldPos, worldRot);

        float wrappedYaw = Mathf.DeltaAngle(0f, yaw);
        if (vcam.TryGetComponent<CinemachineOrbitalFollow>(out var orbital))
        {
            orbital.HorizontalAxis.Value = wrappedYaw;
            orbital.VerticalAxis.Value = Mathf.Clamp(pitch, orbital.VerticalAxis.Range.x, orbital.VerticalAxis.Range.y);
        }

        if (cutDamping)
            vcam.PreviousStateIsValid = false;
    }

    private static void ApplyOrientation(CinemachineCamera vcam, float yaw, float pitch)
    {
        if (vcam == null) return;

        float wrappedYaw = Mathf.DeltaAngle(0f, yaw);
        if (vcam.TryGetComponent<CinemachineOrbitalFollow>(out var orbital))
        {
            orbital.HorizontalAxis.Value = wrappedYaw;
            orbital.VerticalAxis.Value = Mathf.Clamp(pitch, orbital.VerticalAxis.Range.x, orbital.VerticalAxis.Range.y);
        }

        vcam.transform.rotation = Quaternion.Euler(pitch, wrappedYaw, 0f);
        vcam.PreviousStateIsValid = false;
    }
}
