using System;
using System.Collections;
using UnityEngine;

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
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        defaultAnchoredPos = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    /// <summary>
    /// 버튼 위치 방향에서 원래 위치로 부드럽게 드리프트하며 패널을 엽니다.
    /// </summary>
    public void PlayOpen(Vector3? originScreenPos = null, Action onComplete = null)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        gameObject.SetActive(true);
        Vector2 startOffset = CalculateStartOffset(originScreenPos);
        transitionCoroutine = StartCoroutine(CoAnimateOpen(startOffset, onComplete));
    }

    /// <summary>
    /// 패널을 부드럽게 축소 및 페이드아웃하며 닫습니다.
    /// </summary>
    public void PlayClose(Vector3? targetScreenPos = null, Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        Vector2 targetOffset = CalculateStartOffset(targetScreenPos);
        transitionCoroutine = StartCoroutine(CoAnimateClose(targetOffset, onComplete));
    }

    /// <summary>
    /// 애니메이션을 즉시 중단하고 기본 상태로 초기화합니다.
    /// </summary>
    public void StopImmediate()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        ResetToDefault();
        gameObject.SetActive(false);
    }

    private Vector2 CalculateStartOffset(Vector3? screenPos)
    {
        if (!screenPos.HasValue)
        {
            return Vector2.down * driftDistance;
        }

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

    private IEnumerator CoAnimateOpen(Vector2 startOffset, Action onComplete)
    {
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float curveT = motionCurve.Evaluate(t);

            rectTransform.anchoredPosition = defaultAnchoredPos + Vector2.Lerp(startOffset, Vector2.zero, curveT);
            rectTransform.localScale = defaultScale * Mathf.LerpUnclamped(startScaleRatio, 1f, curveT);
            canvasGroup.alpha = curveT;

            yield return null;
        }

        ResetToDefault();
        transitionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator CoAnimateClose(Vector2 targetOffset, Action onComplete)
    {
        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 currentScale = rectTransform.localScale;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float curveT = motionCurve.Evaluate(t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, defaultAnchoredPos + targetOffset * 0.5f, curveT);
            rectTransform.localScale = Vector3.Lerp(currentScale, defaultScale * startScaleRatio, curveT);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveT);

            yield return null;
        }

        ResetToDefault();
        gameObject.SetActive(false);
        transitionCoroutine = null;
        onComplete?.Invoke();
    }
}
