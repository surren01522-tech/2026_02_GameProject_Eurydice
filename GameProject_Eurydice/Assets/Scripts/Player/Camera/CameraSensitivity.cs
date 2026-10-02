using UnityEngine;
using Unity.Cinemachine;

public class CameraSensitivity : MonoBehaviour
{
    [Header("카메라 입력 축 컨트롤러")]
    [SerializeField] private CinemachineInputAxisController[] axisControllers;
    
    [Header("민감도 설정")]
    [SerializeField] private float sensitivity = 1f;
    [SerializeField] private bool invertY = true;

    private void Awake()
    {
        if (axisControllers == null || axisControllers.Length == 0)
        {
            axisControllers = FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
        }
    }

    private void Start() => ApplySensitivity();

    private void OnEnable()
    {
        GameStateManager.OnInputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnInputModeChanged -= HandleInputModeChanged;
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplySensitivity();
    }

    public void ApplySensitivity()
    {
        if (axisControllers == null) return;

        foreach (var controller in axisControllers)
        {
            if (controller == null) continue;
            SetGain(controller, "Look Orbit X", sensitivity);
            SetGain(controller, "Look Orbit Y", invertY ? -sensitivity : sensitivity);
        }
    }

    private void SetGain(CinemachineInputAxisController axisCtrl, string axisName, float gain)
    {
        var controller = axisCtrl.GetController(axisName);
        if (controller?.Input != null)
            controller.Input.Gain = gain;
    }

    public void SetSens(float val, bool? invert = null)
    {
        sensitivity = val;
        if (invert.HasValue) invertY = invert.Value;
        ApplySensitivity();
    }

    public void SetSensitivity(float x, float y, bool? invert = null) => SetSens(x, invert);

    /// <summary>
    /// 입력 모드 변경 시 카메라 조작 가능 여부 변경
    /// </summary>
    private void HandleInputModeChanged(InputMode mode)
    {
        bool enableCameraInput = (mode == InputMode.GamePlay);
        if (axisControllers == null) return;

        foreach (var controller in axisControllers)
        {
            if (controller != null)
                controller.enabled = enableCameraInput;
        }
    }
}