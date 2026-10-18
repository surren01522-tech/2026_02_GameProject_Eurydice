using System.Collections;
using TMPro;
using UnityEngine;

public class TriggerNoticeUIManager : MonoBehaviour
{
    public static TriggerNoticeUIManager Instance { get; private set; }

    [Header("UI 컴포넌트")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("연출 설정")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine currentCoroutine;

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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 지정된 메시지와 타이틀을 페이드인 연출과 함께 HUD에 표시합니다.
    /// duration이 0 이하이면 외부에서 Hide를 호출할 때까지 유지됩니다.
    /// </summary>
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

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(CoShow(duration));
    }

    /// <summary>
    /// 표시 중인 HUD 알림을 숨깁니다.
    /// </summary>
    public void Hide(bool immediate = false)
    {
        if (canvasGroup == null) return;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        if (immediate)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
            currentCoroutine = null;
        }
        else
        {
            currentCoroutine = StartCoroutine(CoFade(0f, () => canvasGroup.gameObject.SetActive(false)));
        }
    }

    private IEnumerator CoShow(float duration)
    {
        canvasGroup.gameObject.SetActive(true);
        yield return CoFade(1f);

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
            yield return CoFade(0f);
            canvasGroup.gameObject.SetActive(false);
        }

        currentCoroutine = null;
    }

    private IEnumerator CoFade(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float dur = fadeDuration > 0f ? fadeDuration : 0.01f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / dur);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
