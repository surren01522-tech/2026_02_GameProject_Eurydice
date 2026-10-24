using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class ScreenFader : MonoBehaviour
{
    private const string PrefabPath = "Fade Canvas";
    private static ScreenFader activeInstance;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.8f;

    public static bool IsFading { get; private set; }

    public static void FadeOut(float duration = -1f, Action onComplete = null)
    {
        EnsureInstance();
        if (activeInstance == null) return;
        activeInstance.StartCoroutine(activeInstance.RunFadeOut(duration, onComplete));
    }

    public static void FadeIn(float duration = -1f, Action onComplete = null)
    {
        if (activeInstance == null)
        {
            onComplete?.Invoke();
            return;
        }
        activeInstance.StartCoroutine(activeInstance.RunFadeIn(duration, onComplete));
    }

    public static IEnumerator FadeOutRoutine(float duration = -1f)
    {
        EnsureInstance();
        if (activeInstance == null) yield break;
        yield return activeInstance.Fade(0f, 1f, duration);
    }

    public static IEnumerator FadeInRoutine(float duration = -1f)
    {
        if (activeInstance == null) yield break;
        yield return activeInstance.Fade(1f, 0f, duration);
        DestroyInstance();
    }

    public static IEnumerator FadeTransitionRoutine(float duration = -1f, Action onDark = null)
    {
        yield return FadeOutRoutine(duration);
        onDark?.Invoke();
        yield return FadeInRoutine(duration);
    }

    private IEnumerator RunFadeOut(float duration, Action onComplete)
    {
        yield return Fade(0f, 1f, duration);
        onComplete?.Invoke();
    }

    private IEnumerator RunFadeIn(float duration, Action onComplete)
    {
        yield return Fade(1f, 0f, duration);
        onComplete?.Invoke();
        DestroyInstance();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        IsFading = true;
        canvasGroup.alpha = from;
        canvasGroup.blocksRaycasts = true;

        float dur = duration >= 0f ? duration : fadeDuration;
        yield return canvasGroup.DOFade(to, dur)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(gameObject)
            .WaitForCompletion();

        canvasGroup.blocksRaycasts = to > 0f;
        if (to <= 0f) IsFading = false;
    }

    private static void EnsureInstance()
    {
        if (activeInstance != null) return;

        var prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null) return;

        var instance = Instantiate(prefab);
        activeInstance = instance.GetComponent<ScreenFader>();
        if (activeInstance != null)
            DontDestroyOnLoad(activeInstance.gameObject);
        else
            Destroy(instance);
    }

    private static void DestroyInstance()
    {
        IsFading = false;
        if (activeInstance == null) return;
        Destroy(activeInstance.gameObject);
        activeInstance = null;
    }

    private void OnDestroy()
    {
        if (activeInstance != this) return;
        activeInstance = null;
        IsFading = false;
    }
}
