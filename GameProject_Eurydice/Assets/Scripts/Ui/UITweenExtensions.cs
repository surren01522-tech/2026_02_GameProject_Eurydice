using System;
using UnityEngine;
using DG.Tweening;

public static class UITweenExtensions
{
    public static Tween PlayPopIn(this Transform target, float duration = 0.2f, float startScale = 0.85f, Ease ease = Ease.OutBack)
    {
        if (target == null) return null;

        target.DOKill();
        target.localScale = Vector3.one * startScale;
        return target.DOScale(1f, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(target.gameObject);
    }

    public static Sequence PlayNoticeSequence(this CanvasGroup cg, float duration = 3f, RectTransform rect = null, Vector2 slideOffset = default, float fadeDuration = 0.3f, Action onComplete = null)
    {
        if (cg == null) return null;

        cg.DOKill();
        cg.gameObject.SetActive(true);
        cg.alpha = 0f;

        float dur = fadeDuration > 0f ? fadeDuration : 0.25f;
        var seq = DOTween.Sequence().SetUpdate(true).SetLink(cg.gameObject);

        seq.Append(cg.DOFade(1f, dur).SetEase(Ease.OutQuad));

        if (rect != null && slideOffset != Vector2.zero)
        {
            Vector2 basePos = rect.anchoredPosition;
            rect.anchoredPosition = basePos + slideOffset;
            seq.Join(rect.DOAnchorPos(basePos, dur).SetEase(Ease.OutBack));

            if (duration > 0f)
            {
                seq.AppendInterval(duration);
                seq.Append(cg.DOFade(0f, dur).SetEase(Ease.InQuad));
                seq.Join(rect.DOAnchorPos(basePos + slideOffset, dur).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    cg.alpha = 0f;
                    cg.gameObject.SetActive(false);
                    rect.anchoredPosition = basePos;
                    onComplete?.Invoke();
                });
            }
        }
        else
        {
            cg.transform.localScale = Vector3.one * 0.95f;
            seq.Join(cg.transform.DOScale(1f, dur).SetEase(Ease.OutBack));

            if (duration > 0f)
            {
                seq.AppendInterval(duration);
                seq.Append(cg.DOFade(0f, dur).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    cg.alpha = 0f;
                    cg.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
            }
        }

        return seq;
    }

    public static bool TryUpdateWorldToScreen(Camera cam, Transform target, Vector3 offset, GameObject panel)
    {
        if (panel == null) return false;

        if (target == null)
        {
            if (panel.activeSelf) panel.SetActive(false);
            return false;
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);
        if (screenPos.z > 0)
        {
            panel.transform.position = screenPos;
            return true;
        }

        if (panel.activeSelf) panel.SetActive(false);
        return false;
    }
}
