using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotObject : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;

    public void Init(ItemData item)
    {
        SetItem(item, 1);
    }

    public void SetItem(ItemData item, int count = 1)
    {
        bool hasItem = item != null && item.icon != null;
        bool isOverOne = item != null && count > 1;

        icon.sprite = hasItem ? item.icon : null;
        amountText.text = isOverOne ? count.ToString() : null;

        icon.gameObject.SetActive(hasItem);
        amountText.gameObject.SetActive(isOverOne);
    }

    public void Clear()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.gameObject.SetActive(false);
        }
    }
}
