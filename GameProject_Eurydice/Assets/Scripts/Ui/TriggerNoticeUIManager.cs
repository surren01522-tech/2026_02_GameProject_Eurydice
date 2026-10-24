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
    [SerializeField] private float fadeDuration = 0.5f;

    private Sequence noticeSeq;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        noticeSeq?.Kill();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message, float duration = 3f, string title = null)
    {
        if (canvasGroup == null) return;

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

        noticeSeq?.Kill();
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.transform.localScale = Vector3.one * 0.95f;
        float dur = fadeDuration > 0f ? fadeDuration : 0.25f;

        noticeSeq = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, dur).SetEase(Ease.OutQuad))
            .Join(canvasGroup.transform.DOScale(1f, dur).SetEase(Ease.OutBack));

        if (duration > 0f)
        {
            noticeSeq.AppendInterval(duration)
                .Append(canvasGroup.DOFade(0f, dur).SetEase(Ease.InQuad))
                .OnComplete(() => canvasGroup.gameObject.SetActive(false));
        }

        noticeSeq.SetLink(gameObject);
    }
    
    public void Hide(bool immediate = false)
    {
        if (canvasGroup == null) return;

        noticeSeq?.Kill();

        if (immediate)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
        else
        {
            float dur = fadeDuration > 0f ? fadeDuration : 0.25f;
            noticeSeq = DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, dur).SetEase(Ease.InQuad))
                .OnComplete(() => canvasGroup.gameObject.SetActive(false))
                .SetLink(gameObject);
        }
    }
}
