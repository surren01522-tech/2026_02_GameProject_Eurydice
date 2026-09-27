using UnityEngine;
using Unity.Cinemachine;

public class CameraModeController : MonoBehaviour
{
    [Header("시네머신 카메라")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera strafeCamera;

    /// <summary>지정된 조작 모드에 맞게 카메라 우선순위를 갱신합니다.</summary>
    public void SetCameraMode(ControlMode mode)
    {
        bool isFreeLook = mode == ControlMode.FreeLook;

        if (freeLookCamera != null)
            freeLookCamera.Priority.Value = isFreeLook ? 10 : 0;

        if (strafeCamera != null)
            strafeCamera.Priority.Value = isFreeLook ? 0 : 10;
    }
}
