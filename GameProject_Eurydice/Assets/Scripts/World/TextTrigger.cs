using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public enum TriggerUIMode
{
    WorldText,
    UIText
}

[RequireComponent(typeof(Collider))]
public class TextTrigger : MonoBehaviour
{
    private sealed class FadeTarget
    {
        public GameObject root;
        public CanvasGroup canvasGroup;
        public TMP_Text[] texts;
        public float[] baseAlphas;
        public float factor;
        public Tween tween;
    }

    [Header("UI 모드")]
    [SerializeField] private TriggerUIMode mode = TriggerUIMode.WorldText;
    [SerializeField] private bool triggerOnce = false;

    [Header("오브젝트 설정")]
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("UI 설정")]
    [SerializeField] private string title;
    [SerializeField, TextArea(2, 4)] private string message;
    [SerializeField] private float duration = 3f;
    [SerializeField] private bool hideOnExit = false;

    private readonly List<FadeTarget> fadeTargets = new List<FadeTarget>();
    private bool hasTriggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        InitFadeTargets();
    }

    private void OnDisable()
    {
        for (int i = 0; i < fadeTargets.Count; i++)
            fadeTargets[i].tween?.Kill();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!IsPlayer(other)) return;

        if (mode == TriggerUIMode.WorldText)
        {
            StartFade(1f);
        }
        else if (mode == TriggerUIMode.UIText && TriggerNoticeUIManager.Instance != null)
        {
            TriggerNoticeUIManager.Instance.Show(message, duration, title);
        }

        if (triggerOnce) hasTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (mode == TriggerUIMode.WorldText)
        {
            StartFade(0f);
        }
        else if (mode == TriggerUIMode.UIText && hideOnExit && TriggerNoticeUIManager.Instance != null)
        {
            TriggerNoticeUIManager.Instance.Hide();
        }
    }

    private void InitFadeTargets()
    {
        fadeTargets.Clear();
        if (targetObjects == null) return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            var obj = targetObjects[i];
            if (obj == null) continue;

            var cg = obj.GetComponent<CanvasGroup>();
            // CanvasGroup은 3D TextMeshPro(MeshRenderer)에 영향을 주지 않으므로 3D 텍스트를 별도 수집
            TMP_Text[] texts = cg != null
                ? obj.GetComponentsInChildren<TextMeshPro>(true)
                : obj.GetComponentsInChildren<TMP_Text>(true);

            if (cg == null && texts.Length == 0) continue;

            var alphas = new float[texts.Length];
            for (int t = 0; t < texts.Length; t++)
                alphas[t] = texts[t].alpha > 0f ? texts[t].alpha : 1f;

            var target = new FadeTarget
            {
                root = obj,
                canvasGroup = cg,
                texts = texts,
                baseAlphas = alphas,
                factor = 0f
            };

            ApplyFadeFactor(target, 0f);
            obj.SetActive(false);
            fadeTargets.Add(target);
        }
    }

    private void StartFade(float targetFactor)
    {
        float dur = fadeDuration > 0f ? fadeDuration : 0.01f;

        for (int i = 0; i < fadeTargets.Count; i++)
        {
            var target = fadeTargets[i];
            if (target.root == null) continue;

            target.tween?.Kill();
            if (targetFactor > 0f) target.root.SetActive(true);

            target.tween = DOVirtual.Float(target.factor, targetFactor, dur, val => ApplyFadeFactor(target, val))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(target.root)
                .OnComplete(() =>
                {
                    ApplyFadeFactor(target, targetFactor);
                    if (targetFactor <= 0f && target.root != null)
                        target.root.SetActive(false);
                });
        }
    }

    private static void ApplyFadeFactor(FadeTarget target, float factor)
    {
        target.factor = factor;

        if (target.canvasGroup != null)
            target.canvasGroup.alpha = factor;

        for (int i = 0; i < target.texts.Length; i++)
        {
            if (target.texts[i] != null)
                target.texts[i].alpha = target.baseAlphas[i] * factor;
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
    }
}
