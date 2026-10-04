using System.Collections;
using TMPro;
using UnityEngine;

public enum FeedbackDisplayMode
{
    Fixed,
    Floating
}

public class InteractionFeedbackUI : MonoBehaviour
{
    public static InteractionFeedbackUI Instance { get; private set; }

    [Header("Fixed HUD Feedback")]
    [SerializeField] private GameObject fixedPanel;
    [SerializeField] private TextMeshProUGUI fixedText;

    [Header("Floating Target Feedback")]
    [SerializeField] private GameObject floatingPanel;
    [SerializeField] private TextMeshProUGUI floatingText;
    [SerializeField] private Vector3 defaultFloatingOffset = new Vector3(0, 0.8f, 0);

    private Camera mainCam;
    private Coroutine hideCoroutine;
    private Transform floatingTarget;
    private Vector3 floatingOffset;
    private FeedbackDisplayMode currentMode;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (currentMode != FeedbackDisplayMode.Floating || floatingPanel == null || !floatingPanel.activeSelf) return;

        if (floatingTarget == null)
        {
            floatingPanel.SetActive(false);
            return;
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 worldPos = floatingTarget.position + floatingOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0)
        {
            floatingPanel.transform.position = screenPos;
        }
        else
        {
            floatingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 지정된 메시지와 모드로 피드백 알림을 표시합니다.
    /// </summary>
    public void Show(string message, FeedbackDisplayMode mode = FeedbackDisplayMode.Fixed, Transform target = null, Vector3 worldOffset = default, float duration = 1.5f)
    {
        HideAll();

        currentMode = mode;
        floatingTarget = target;
        floatingOffset = worldOffset == default ? defaultFloatingOffset : worldOffset;

        if (mode == FeedbackDisplayMode.Fixed)
        {
            if (fixedText != null) fixedText.text = message;
            if (fixedPanel != null) fixedPanel.SetActive(true);
        }
        else if (mode == FeedbackDisplayMode.Floating)
        {
            if (floatingText != null) floatingText.text = message;
            if (floatingPanel != null)
            {
                if (target != null && mainCam != null)
                {
                    Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + floatingOffset);
                    if (screenPos.z > 0) floatingPanel.transform.position = screenPos;
                }
                floatingPanel.SetActive(true);
            }
        }

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(CoAutoDismiss(duration));
    }

    private IEnumerator CoAutoDismiss(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideAll();
        hideCoroutine = null;
    }

    public void HideAll()
    {
        if (fixedPanel != null) fixedPanel.SetActive(false);
        if (floatingPanel != null) floatingPanel.SetActive(false);
        floatingTarget = null;
    }
}
