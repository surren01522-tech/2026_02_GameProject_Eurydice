using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [SerializeField] private GameObject SlotPrefab;
    [SerializeField] private Transform SlotContainer;
    [SerializeField] private int initSlotSize = 5;

    private readonly List<InventorySlotObject> slotUIList = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitSlots();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += SyncSlot;
            SyncSlot();
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= SyncSlot;
        }
    }

    private void InitSlots()
    {
        if (SlotContainer == null) return;

        slotUIList.Clear();
        slotUIList.AddRange(SlotContainer.GetComponentsInChildren<InventorySlotObject>(true));

        while (slotUIList.Count < initSlotSize && SlotPrefab != null)
        {
            var slotObj = Instantiate(SlotPrefab, SlotContainer);
            var slotUI = slotObj.GetComponent<InventorySlotObject>();
            if (slotUI != null) slotUIList.Add(slotUI);
        }
    }

    public void SyncSlot()
    {
        if (InventoryManager.Instance == null || SlotContainer == null) return;

        var slots = InventoryManager.Instance.Slots;
        int targetCount = Mathf.Max(initSlotSize, slots.Count);

        while (slotUIList.Count < targetCount && SlotPrefab != null)
        {
            var slotObj = Instantiate(SlotPrefab, SlotContainer);
            var slotUI = slotObj.GetComponent<InventorySlotObject>();
            if (slotUI != null) slotUIList.Add(slotUI);
        }

        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i < slots.Count)
            {
                slotUIList[i].gameObject.SetActive(true);
                slotUIList[i].SetItem(slots[i].itemData, slots[i].count);
            }
            else
            {
                slotUIList[i].Clear();
                slotUIList[i].gameObject.SetActive(i < initSlotSize);
            }
        }
    }
}
