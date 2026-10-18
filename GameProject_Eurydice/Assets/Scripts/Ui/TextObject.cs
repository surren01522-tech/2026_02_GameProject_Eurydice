using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TriggerUIMode
{
    WorldText,
    UIText
}

[RequireComponent(typeof(Collider))]
public class TextObject : MonoBehaviour
{
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

    private readonly List<CanvasGroup> targetCanvasGroups = new List<CanvasGroup>();
    private Coroutine fadeCoroutine;
    private bool hasTriggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        InitTargetCanvasGroups();
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

    private void InitTargetCanvasGroups()
    {
        targetCanvasGroups.Clear();
        if (targetObjects == null) return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            var obj = targetObjects[i];
            if (obj == null) continue;

            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            obj.SetActive(false);
            targetCanvasGroups.Add(cg);
        }
    }

    private void StartFade(float targetAlpha)
    {
        if (targetCanvasGroups.Count == 0) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CoFade(targetAlpha));
    }

    private IEnumerator CoFade(float targetAlpha)
    {
        if (targetAlpha > 0f)
        {
            for (int i = 0; i < targetCanvasGroups.Count; i++)
            {
                if (targetCanvasGroups[i] != null)
                    targetCanvasGroups[i].gameObject.SetActive(true);
            }
        }

        float startAlpha = targetCanvasGroups.Count > 0 && targetCanvasGroups[0] != null ? targetCanvasGroups[0].alpha : 0f;
        float elapsed = 0f;
        float duration = fadeDuration > 0f ? fadeDuration : 0.01f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            for (int i = 0; i < targetCanvasGroups.Count; i++)
            {
                if (targetCanvasGroups[i] != null)
                    targetCanvasGroups[i].alpha = currentAlpha;
            }

            yield return null;
        }

        for (int i = 0; i < targetCanvasGroups.Count; i++)
        {
            if (targetCanvasGroups[i] == null) continue;
            targetCanvasGroups[i].alpha = targetAlpha;

            if (targetAlpha <= 0f)
                targetCanvasGroups[i].gameObject.SetActive(false);
        }

        fadeCoroutine = null;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
    }
}
