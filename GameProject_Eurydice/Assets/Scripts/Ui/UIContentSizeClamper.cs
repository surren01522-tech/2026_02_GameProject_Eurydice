using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIContentSizeClamper : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private bool controlHeight = true;
    [SerializeField] private float minHeight = 100f;
    [SerializeField] private float maxHeight = 600f;
    [SerializeField] private float paddingHeight = 0f;

    [SerializeField] private bool controlWidth = false;
    [SerializeField] private float minWidth = 100f;
    [SerializeField] private float maxWidth = 600f;
    [SerializeField] private float paddingWidth = 0f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        AdjustSize();
    }

    /// <summary>
    /// Content 크기에 맞춰 패널 크기를 Min/Max 범위 내로 조절
    /// </summary>
    public void AdjustSize()
    {
        if (content == null) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (controlHeight)
        {
            float targetHeight = Mathf.Clamp(content.rect.height + paddingHeight, minHeight, maxHeight);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }

        if (controlWidth)
        {
            float targetWidth = Mathf.Clamp(content.rect.width + paddingWidth, minWidth, maxWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        }
    }
}
