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

    public void SetCameraMode(ControlMode mode)
    {
        bool isFreeLook = mode == ControlMode.FreeLook;

        if (freeLookCamera != null)
            freeLookCamera.Priority.Value = isFreeLook ? 10 : 0;

        if (strafeCamera != null)
            strafeCamera.Priority.Value = isFreeLook ? 0 : 10;
    }
}
