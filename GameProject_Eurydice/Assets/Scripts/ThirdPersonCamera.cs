using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("따라갈 대상")]

    [SerializeField] private Transform target;

    [Header("카메라 위치")]
    [SerializeField] private float distance = 7f;
    [SerializeField] private float height = 1.5f;

    [Header("마우스 회전")]
    [SerializeField] private float mouseSensitivityty = 0.12f;
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("마우스 스크롤")]
    [SerializeField] private float zoomspeed = 1;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;

    private float yaw;
    private float pitch = 15f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivityty;
        pitch -= mouseDelta.y * mouseSensitivityty;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        float scroll = mouse.scroll.ReadValue().y;

        distance -= scroll * zoomspeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    public void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookPoint = target.position + Vector3.up * height;

        Quaternion orbitRotaion = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 cameraOffset = orbitRotaion * new Vector3(0f, 0f, -distance);

        transform.position = lookPoint + cameraOffset;

        transform.LookAt(lookPoint);
    }
}
