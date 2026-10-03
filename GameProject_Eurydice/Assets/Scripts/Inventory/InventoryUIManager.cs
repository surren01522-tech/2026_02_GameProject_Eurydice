using System.Collections.Generic;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [SerializeField] private GameObject SlotPrefab;
    [SerializeField] private Transform SlotContainer;
    [SerializeField] private int initSlotSize = 5;

    private readonly List<InventorySlotObject> slotUIList = new List<InventorySlotObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (SlotContainer != null)
        {
            slotUIList.AddRange(SlotContainer.GetComponentsInChildren<InventorySlotObject>(true));
        }

        SyncSlot();
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= SyncSlot;
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

    public void SyncSlot()
    {
        if (InventoryManager.Instance == null || SlotContainer == null) return;

        var slots = InventoryManager.Instance.Slots;
        int targetCount = Mathf.Max(initSlotSize, slots.Count);

        EnsureSlotCount(targetCount);

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

    private void EnsureSlotCount(int targetCount)
    {
        while (slotUIList.Count < targetCount && SlotPrefab != null)
        {
            GameObject slotObj = Instantiate(SlotPrefab, SlotContainer);
            InventorySlotObject slotUI = slotObj.GetComponent<InventorySlotObject>();
            if (slotUI != null)
            {
                slotUIList.Add(slotUI);
            }
        }
    }
}
