using UnityEngine;
using Unity.Cinemachine;

public class CameraModeController : MonoBehaviour
{
    [Header("시네머신 카메라")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera strafeCamera;

    private void Awake()
    {
        FindCameras();
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

    public CinemachineCamera ActiveCamera => (freeLookCamera != null && freeLookCamera.Priority.Value >= (strafeCamera != null ? strafeCamera.Priority.Value : 0)) ? freeLookCamera : strafeCamera;

    public void SetCameraMode(ControlMode mode)
    {
        bool isFreeLook = mode == ControlMode.FreeLook;

        if (freeLookCamera != null)
            freeLookCamera.Priority.Value = isFreeLook ? 10 : 0;

        if (strafeCamera != null)
            strafeCamera.Priority.Value = isFreeLook ? 0 : 10;
    }

    public void WarpCameras(Transform target, Vector3 delta)
    {
        if (target == null) return;
        if (freeLookCamera != null) freeLookCamera.OnTargetObjectWarped(target, delta);
        if (strafeCamera != null) strafeCamera.OnTargetObjectWarped(target, delta);
    }

    public void SetCameraOrientation(float yaw, float pitch)
    {
        ApplyOrientationToCamera(freeLookCamera, yaw, pitch);
        ApplyOrientationToCamera(strafeCamera, yaw, pitch);
    }

    private void ApplyOrientationToCamera(CinemachineCamera vcam, float yaw, float pitch)
    {
        if (vcam == null) return;

        var orbital = vcam.GetComponent<CinemachineOrbitalFollow>();
        if (orbital != null)
        {
            orbital.HorizontalAxis.Value = yaw;
            orbital.VerticalAxis.Value = pitch;
        }

        vcam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
