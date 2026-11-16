using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MapMarkerUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private Action<MapMarkerUI> onClick;
    private Action<MapMarkerUI, bool> onHover;
    public MapStaticMarkerData Data { get; private set; }

    public void Setup(MapStaticMarkerData data, Sprite sprite, Color color, float size, Vector2 normalizedPos,
        Action<MapMarkerUI> onClickCallback, Action<MapMarkerUI, bool> onHoverCallback)
    {
        Data = data;
        onClick = onClickCallback;
        onHover = onHoverCallback;

        if (iconImage == null) iconImage = GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = color;
            iconImage.raycastTarget = true;
        }

        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = normalizedPos;
        rt.anchoredPosition = Vector2.zero;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left) onClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData e) => onHover?.Invoke(this, true);
    public void OnPointerExit(PointerEventData e) => onHover?.Invoke(this, false);
}
