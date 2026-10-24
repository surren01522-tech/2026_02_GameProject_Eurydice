using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UIPanelTween : MonoBehaviour, IUIPanelTransition
{
    [Header("컴포넌트 바인딩")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("트랜지션 연출")]
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [SerializeField] private Ease closeEase = Ease.InCubic;
    [SerializeField] private float driftDistance = 35f;
    [SerializeField] private float startScaleRatio = 0.92f;

    private RectTransform rectTransform;
    private Vector2 defaultAnchoredPos;
    private Vector3 defaultScale;
    private Sequence transitionSeq;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        defaultAnchoredPos = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
    }

    private void OnDisable()
    {
        transitionSeq?.Kill();
    }

    public void PlayOpen(Vector3? originScreenPos = null, Action onComplete = null)
    {
        transitionSeq?.Kill();
        gameObject.SetActive(true);

        Vector2 startOffset = CalculateStartOffset(originScreenPos);
        rectTransform.anchoredPosition = defaultAnchoredPos + startOffset;
        rectTransform.localScale = defaultScale * startScaleRatio;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        transitionSeq = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPos(defaultAnchoredPos, openDuration).SetEase(openEase))
            .Join(rectTransform.DOScale(defaultScale, openDuration).SetEase(openEase));

        if (canvasGroup != null)
            transitionSeq.Join(canvasGroup.DOFade(1f, openDuration).SetEase(openEase));

        transitionSeq.SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                ResetToDefault();
                onComplete?.Invoke();
            });
    }

    public void PlayClose(Vector3? targetScreenPos = null, Action onComplete = null)
    {
        transitionSeq?.Kill();

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        Vector2 targetOffset = CalculateStartOffset(targetScreenPos);

        transitionSeq = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPos(defaultAnchoredPos + targetOffset * 0.5f, closeDuration).SetEase(closeEase))
            .Join(rectTransform.DOScale(defaultScale * startScaleRatio, closeDuration).SetEase(closeEase));

        if (canvasGroup != null)
            transitionSeq.Join(canvasGroup.DOFade(0f, closeDuration).SetEase(closeEase));

        transitionSeq.SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                ResetToDefault();
                onComplete?.Invoke();
            });
    }

    public void StopImmediate()
    {
        transitionSeq?.Kill();
        gameObject.SetActive(false);
        ResetToDefault();
    }

    private Vector2 CalculateStartOffset(Vector3? screenPos)
    {
        if (!screenPos.HasValue) return Vector2.down * driftDistance;
        Vector2 dir = ((Vector2)(screenPos.Value - transform.position)).normalized;
        return dir.sqrMagnitude > 0.001f ? dir * driftDistance : Vector2.down * driftDistance;
    }

    private void ResetToDefault()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = defaultAnchoredPos;
            rectTransform.localScale = defaultScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
