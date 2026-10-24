using TMPro;
using UnityEngine;
using DG.Tweening;

public enum FeedbackDisplayMode
{
    Fixed,
    Floating
}

public class InteractionFeedbackUIManager : MonoBehaviour
{
    public static InteractionFeedbackUIManager Instance { get; private set; }

    [Header("Fixed HUD Feedback")]
    [SerializeField] private GameObject fixedPanel;
    [SerializeField] private TextMeshProUGUI fixedText;

    [Header("Floating Target Feedback")]
    [SerializeField] private GameObject floatingPanel;
    [SerializeField] private TextMeshProUGUI floatingText;
    [SerializeField] private Vector3 defaultFloatingOffset = new Vector3(0, 0.8f, 0);

    private Camera mainCam;
    private Tween dismissTween;
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

    private void OnDisable()
    {
        dismissTween?.Kill();
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
    
    public void Show(string message, FeedbackDisplayMode mode = FeedbackDisplayMode.Fixed, Transform target = null, Vector3 worldOffset = default, float duration = 1.5f)
    {
        HideAll();

        currentMode = mode;
        floatingTarget = target;
        floatingOffset = worldOffset == default ? defaultFloatingOffset : worldOffset;

        GameObject activePanel = null;

        if (mode == FeedbackDisplayMode.Fixed)
        {
            if (fixedText != null) fixedText.text = message;
            activePanel = fixedPanel;
        }
        else if (mode == FeedbackDisplayMode.Floating)
        {
            if (floatingText != null) floatingText.text = message;
            if (floatingPanel != null && target != null && mainCam != null)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + floatingOffset);
                if (screenPos.z > 0) floatingPanel.transform.position = screenPos;
            }
            activePanel = floatingPanel;
        }

        if (activePanel != null)
        {
            activePanel.SetActive(true);
            activePanel.transform.DOKill();
            activePanel.transform.localScale = Vector3.one * 0.85f;
            activePanel.transform.DOScale(1f, 0.2f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(activePanel);
        }

        dismissTween?.Kill();
        dismissTween = DOVirtual.DelayedCall(duration, HideAll, ignoreTimeScale: true).SetLink(gameObject);
    }

    public void HideAll()
    {
        dismissTween?.Kill();
        if (fixedPanel != null)
        {
            fixedPanel.transform.DOKill();
            fixedPanel.SetActive(false);
        }
        if (floatingPanel != null)
        {
            floatingPanel.transform.DOKill();
            floatingPanel.SetActive(false);
        }
        floatingTarget = null;
    }
}
