using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotObject : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button button;

    private ItemData currentItem;
    private Action<ItemData> onClickCallback;

    public ItemData CurrentItem => currentItem;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetClickCallback(Action<ItemData> callback)
    {
        onClickCallback = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null)
            HandleClick();
    }

    private void HandleClick()
    {
        if (currentItem != null)
            onClickCallback?.Invoke(currentItem);
    }

    public void Init(ItemData item)
    {
        SetItem(item, 1);
    }

    public void SetItem(ItemData item, int count = 1)
    {
        currentItem = item;
        bool hasItem = item != null;
        bool hasIcon = hasItem && item.icon != null;
        bool isOverOne = hasItem && count > 1;

        if (icon != null)
        {
            if (hasItem)
            {
                icon.sprite = hasIcon ? item.icon : null;
                icon.color = hasIcon ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.sprite = null;
                icon.color = Color.clear;
                icon.gameObject.SetActive(false);
            }
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
        currentItem = null;
        if (icon != null)
        {
            icon.sprite = null;
            icon.color = Color.clear;
            icon.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = null;
            amountText.gameObject.SetActive(false);
        }
    }
}
