using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private CinemachineOrbitalFollow[] orbitalFollows;

    [Header("거리 설정")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float defaultDistance = 5f;

    [Header("줌 감도")]
    [SerializeField] private float zoomSensitivity = 1f;
    [SerializeField] private float smoothSpeed = 10f;

    private float currentDistance;
    private float targetDistance;

    public float CurrentDistance => targetDistance;

    private void Awake()
    {
        FindOrbitalFollows();
        targetDistance = defaultDistance;
    }

    private void Update()
    {
        if (Mouse.current == null || GameStateManager.CurrentInputMode != InputMode.GamePlay) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, smoothSpeed * Time.deltaTime);
        ApplyDistance(currentDistance);
    }

    private void FindOrbitalFollows()
    {
        if (orbitalFollows == null || orbitalFollows.Length == 0)
        {
            orbitalFollows = FindObjectsByType<CinemachineOrbitalFollow>(FindObjectsSortMode.None);
        }
    }

    private void ApplyDistance(float distance)
    {
        if (orbitalFollows == null) return;

        for (int i = 0; i < orbitalFollows.Length; i++)
        {
            if (orbitalFollows[i] != null)
                orbitalFollows[i].Radius = distance;
        }
    }

    public void SetDistance(float distance, bool immediate = false)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        if (immediate)
        {
            currentDistance = targetDistance;
            ApplyDistance(currentDistance);
        }
    }
}
