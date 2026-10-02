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
        bool hasItem = item != null;
        bool hasIcon = hasItem && item.icon != null;
        bool isOverOne = hasItem && count > 1;

        if (icon != null)
        {
            icon.sprite = hasIcon ? item.icon : null;
            icon.gameObject.SetActive(hasIcon);
        }

        if (amountText != null)
        {
            if (isOverOne)
            {
                amountText.text = count.ToString();
                amountText.gameObject.SetActive(true);
            }
            else if (hasItem && !hasIcon)
            {
                amountText.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
                amountText.gameObject.SetActive(true);
            }
            else
            {
                amountText.text = null;
                amountText.gameObject.SetActive(false);
            }
        }
    }

    public void Clear()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = null;
            amountText.gameObject.SetActive(false);
        }
    }
}
