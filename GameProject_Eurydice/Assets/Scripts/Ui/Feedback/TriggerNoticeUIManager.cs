using TMPro;
using UnityEngine;
using DG.Tweening;

public class TriggerNoticeUIManager : MonoBehaviour
{
    public static TriggerNoticeUIManager Instance { get; private set; }

    [Header("UI 컴포넌트")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("연출 설정")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Tween noticeTween;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        Hide(immediate: true);
    }

    private void OnDisable()
    {
        noticeTween?.Kill();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message, float duration = 3f, string title = null)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            titleText.text = title ?? string.Empty;
        }

        if (messageText != null)
        {
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            messageText.text = message ?? string.Empty;
        }

        if (canvasGroup != null)
        {
            noticeTween?.Kill();
            noticeTween = canvasGroup.PlayNoticeSequence(duration, fadeDuration: fadeDuration);
        }
    }

    public void Hide(bool immediate = false)
    {
        noticeTween?.Kill();

        if (canvasGroup == null) return;

        if (immediate)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
        else
        {
            noticeTween = canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.gameObject.SetActive(false);
                });
        }
    }
}
