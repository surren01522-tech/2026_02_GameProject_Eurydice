using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Sky/Journey 스타일의 소프트 드리프트 & 페이드 UI 패널 트랜지션 컴포넌트
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPanelTween : MonoBehaviour, IUIPanelTransition
{
    [Header("재생 시간")]
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closeDuration = 0.2f;

    [Header("드리프트 & 스케일")]
    [SerializeField] private float driftDistance = 35f;
    [SerializeField] private float startScaleRatio = 0.92f;

    [Header("애니메이션 곡선")]
    [SerializeField] private AnimationCurve motionCurve = new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 defaultAnchoredPos;
    private Vector3 defaultScale;
    private Sequence transitionSeq;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
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
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        transitionSeq = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPos(defaultAnchoredPos, openDuration).SetEase(motionCurve))
            .Join(rectTransform.DOScale(defaultScale, openDuration).SetEase(motionCurve))
            .Join(canvasGroup.DOFade(1f, openDuration).SetEase(motionCurve))
            .SetUpdate(true)
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

        canvasGroup.blocksRaycasts = false;
        Vector2 targetOffset = CalculateStartOffset(targetScreenPos);

        transitionSeq = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPos(defaultAnchoredPos + targetOffset * 0.5f, closeDuration).SetEase(motionCurve))
            .Join(rectTransform.DOScale(defaultScale * startScaleRatio, closeDuration).SetEase(motionCurve))
            .Join(canvasGroup.DOFade(0f, closeDuration).SetEase(motionCurve))
            .SetUpdate(true)
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
