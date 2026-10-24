using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("버튼 연출")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Selectable selectable;
    private Vector3 defaultScale;
    private Tween scaleTween;
    private bool isHovered;

    public static UIButtonTween Bind(Selectable target)
    {
        if (target == null) return null;
        var comp = target.GetComponent<UIButtonTween>();
        return comp != null ? comp : target.gameObject.AddComponent<UIButtonTween>();
    }

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        defaultScale = transform.localScale;
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = defaultScale;
        isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        isHovered = true;
        AnimateScale(defaultScale * hoverScale, hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateScale(defaultScale, Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        AnimateScale(defaultScale * pressScale, pressEase, duration * 0.6f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;
        AnimateScale(isHovered ? defaultScale * hoverScale : defaultScale, releaseEase);
    }

    private void AnimateScale(Vector3 target, Ease ease, float customDuration = -1f)
    {
        scaleTween?.Kill();
        float d = customDuration >= 0f ? customDuration : duration;
        scaleTween = transform.DOScale(target, d)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
