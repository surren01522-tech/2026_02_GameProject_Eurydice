using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HUDOverlay : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 12f;
    [SerializeField] private List<GameObject> hideOnModalPanels = new();

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        GameStateManager.OnInputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnInputModeChanged -= HandleInputModeChanged;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        bool isHudMode = (mode == InputMode.HUDOverlay);
        bool shouldShow = isHudMode || (mode == InputMode.UIModal);

        SetPanelsActive(isHudMode);

        canvasGroup.interactable = shouldShow;
        canvasGroup.blocksRaycasts = shouldShow;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(CoFade(shouldShow ? 1f : 0f));
    }

    /// <summary>
    /// 가려질 대상 패널들의 활성 상태를 일괄 전환합니다.
    /// </summary>
    private void SetPanelsActive(bool active)
    {
        for (int i = 0; i < hideOnModalPanels.Count; i++)
        {
            if (hideOnModalPanels[i] != null)
            {
                hideOnModalPanels[i].SetActive(active);
            }
        }
    }

    /// <summary>
    /// UI 페이드 코루틴
    /// </summary>
    private IEnumerator CoFade(float targetAlpha)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }
}
