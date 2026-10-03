using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayerIcon
{
    private readonly RectTransform iconRect;
    private readonly Color pointerColor;
    private RectTransform pointerRect;

    public MinimapPlayerIcon(RectTransform icon, Color pointerColor)
    {
        this.iconRect = icon;
        this.pointerColor = pointerColor;
    }

    public void Update(RectTransform container, Vector2 normalizedPos, float yawAngle, float size, bool showPointer)
    {
        if (iconRect == null || container == null) return;

        if (iconRect.parent != container)
        {
            iconRect.SetParent(container, false);
        }

        Vector2 centerPivot = new Vector2(0.5f, 0.5f);
        iconRect.anchorMin = centerPivot;
        iconRect.anchorMax = centerPivot;
        iconRect.pivot = centerPivot;

        Vector2 containerSize = container.rect.size;
        float localX = (normalizedPos.x - 0.5f) * containerSize.x;
        float localY = (normalizedPos.y - 0.5f) * containerSize.y;

        iconRect.anchoredPosition = new Vector2(localX, localY);
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.localScale = Vector3.one;
        iconRect.localEulerAngles = new Vector3(0f, 0f, -yawAngle);

        if (showPointer)
        {
            var pointer = GetOrCreatePointer();
            if (pointer != null)
            {
                pointer.gameObject.SetActive(true);
                float pointerSize = size * 0.5f;
                pointer.sizeDelta = new Vector2(pointerSize, pointerSize);
                pointer.anchoredPosition = new Vector2(0f, size * 0.45f);
            }
        }
        else if (pointerRect != null)
        {
            pointerRect.gameObject.SetActive(false);
        }
    }

    private RectTransform GetOrCreatePointer()
    {
        if (pointerRect != null) return pointerRect;
        if (iconRect == null) return null;

        Transform existingChild = iconRect.Find("DirectionPointer");
        if (existingChild != null)
        {
            pointerRect = existingChild.GetComponent<RectTransform>();
            return pointerRect;
        }

        GameObject pointerObj = new GameObject("DirectionPointer");
        pointerObj.transform.SetParent(iconRect, false);

        pointerRect = pointerObj.AddComponent<RectTransform>();
        pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        pointerRect.pivot = new Vector2(0.5f, 0.5f);
        pointerRect.localEulerAngles = new Vector3(0f, 0f, 45f);

        Image img = pointerObj.AddComponent<Image>();
        img.color = pointerColor;
        img.raycastTarget = false;

        return pointerRect;
    }
}
