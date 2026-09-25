using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraSensitivity : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController axisController;
    
    [Header("민감도 설정")]
    [SerializeField] private float sensitivityX = 1f;
    [SerializeField] private float sensitivityY = 1f;
    [SerializeField] private bool invertY = true;

    public float SensitivityX => sensitivityX;

    private void Awake()
    {
        if (axisController == null)
            axisController = GetComponent<CinemachineInputAxisController>();
    }

    private void Start() => ApplySensitivity();

    private void OnValidate()
    {
        if (Application.isPlaying && axisController != null)
            ApplySensitivity();
    }

    public void ApplySensitivity()
    {
        if (axisController == null) return;

        SetGain("Look Orbit X", sensitivityX);
        SetGain("Look Orbit Y", invertY ? -sensitivityY : sensitivityY);
    }

    private void SetGain(string axisName, float gain)
    {
        var controller = axisController.GetController(axisName);
        if (controller?.Input != null)
            controller.Input.Gain = gain;
    }

    public void SetSensitivity(float x, float y, bool? invert = null)
    {
        sensitivityX = x;
        sensitivityY = y;
        if (invert.HasValue) invertY = invert.Value;
        ApplySensitivity();
    }
}