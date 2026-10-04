using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [SerializeField] private GameObject SlotPrefab;
    [SerializeField] private Transform SlotContainer;
    [SerializeField] private int initSlotSize = 5;

    private readonly List<InventorySlotObject> slotUIList = new List<InventorySlotObject>();
    private ItemSocket currentTargetSocket;
    private Action<ItemData> onItemSelectedCallback;

    public bool IsSelectionMode { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeSlots();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        InitializeSlots();
        SyncSlot();
    }

    private void OnEnable()
    {
        InitializeSlots();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= SyncSlot;
            InventoryManager.Instance.OnInventoryChanged += SyncSlot;
        }

        SyncSlot();
    }

    private void InitializeSlots()
    {
        if (SlotContainer == null || slotUIList.Count > 0) return;
        foreach (var slot in SlotContainer.GetComponentsInChildren<InventorySlotObject>(true))
        {
            slot.SetClickCallback(OnSlotClicked);
            slotUIList.Add(slot);
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= SyncSlot;

        if (IsSelectionMode) CancelSelection();
    }

    public void SyncSlot()
    {
        if (InventoryManager.Instance == null || SlotContainer == null) return;

        var slots = InventoryManager.Instance.Slots;
        EnsureSlotCount(Mathf.Max(initSlotSize, slots.Count));

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

    /// <summary>
    /// 퍼즐 등의 기믹 슬롯 설치를 위한 아이템 선택 모드로 인벤토리를 엽니다.
    /// </summary>
    public void OpenForSelection(ItemSocket targetSocket, Action<ItemData> onSelected = null)
    {
        UIWindowManager.Instance?.OpenTab(UIWindowManager.WindowTab.Inventory);
        currentTargetSocket = targetSocket;
        onItemSelectedCallback = onSelected;
        IsSelectionMode = true;
    }

    /// <summary>
    /// 아이템 선택 모드를 종료하고 창을 닫습니다.
    /// </summary>
    public void CloseSelection()
    {
        CancelSelection();
        UIWindowManager.Instance?.CloseAllTabs(animatePanel: false);
    }

    public void CancelSelection()
    {
        IsSelectionMode = false;
        currentTargetSocket = null;
        onItemSelectedCallback = null;
    }

    private void OnSlotClicked(ItemData item)
    {
        if (item == null || !IsSelectionMode) return;
        onItemSelectedCallback?.Invoke(item);

        if (currentTargetSocket != null && currentTargetSocket.TryInsertItem(item))
        {
            CloseSelection();
        }
    }

    private void EnsureSlotCount(int targetCount)
    {
        InitializeSlots();
        while (slotUIList.Count < targetCount && SlotPrefab != null)
        {
            GameObject slotObj = Instantiate(SlotPrefab, SlotContainer);
            if (slotObj.TryGetComponent<InventorySlotObject>(out var slotUI))
            {
                slotUI.SetClickCallback(OnSlotClicked);
                slotUIList.Add(slotUI);
            }
        }
    }
}
