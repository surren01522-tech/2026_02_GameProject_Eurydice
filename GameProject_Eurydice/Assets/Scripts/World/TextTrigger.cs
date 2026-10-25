using System.Collections.Generic;
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
    private bool hasTriggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        InitTargetCanvasGroups();
    }

    private void OnDisable()
    {
        for (int i = 0; i < targetCanvasGroups.Count; i++)
        {
            if (targetCanvasGroups[i] != null) targetCanvasGroups[i].DOKill();
        }
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
            if (cg == null) continue;

            cg.alpha = 0f;
            obj.SetActive(false);
            targetCanvasGroups.Add(cg);
        }
    }

    private void StartFade(float targetAlpha)
    {
        float dur = fadeDuration > 0f ? fadeDuration : 0.01f;

        for (int i = 0; i < targetCanvasGroups.Count; i++)
        {
            var cg = targetCanvasGroups[i];
            if (cg == null) continue;

            cg.DOKill();
            if (targetAlpha > 0f) cg.gameObject.SetActive(true);

            cg.DOFade(targetAlpha, dur)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(cg.gameObject)
                .OnComplete(() =>
                {
                    if (targetAlpha <= 0f) cg.gameObject.SetActive(false);
                });
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
    }
}
