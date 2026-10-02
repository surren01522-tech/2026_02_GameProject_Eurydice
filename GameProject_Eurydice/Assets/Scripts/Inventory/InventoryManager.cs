using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<InventorySlot> slots = new();
    [SerializeField] private List<ItemData> itemDatabase = new();

    public IReadOnlyList<InventorySlot> Slots => slots;
    public event Action OnInventoryChanged;

    private readonly Dictionary<string, ItemData> itemLookup = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitItemLookup();
        Load();
    }

    private void Start()
    {
        OnInventoryChanged?.Invoke();
    }

    private void OnEnable()
    {
        SaveManager.OnSaveDataDeleted += Clear;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveDataDeleted -= Clear;
    }

    public void Clear()
    {
        slots.Clear();
        OnInventoryChanged?.Invoke();
    }

    private void InitItemLookup()
    {
        itemLookup.Clear();

        if (itemDatabase.Count == 0)
        {
            var loadedItems = Resources.LoadAll<ItemData>("Items");
            if (loadedItems != null && loadedItems.Length > 0)
            {
                itemDatabase.AddRange(loadedItems);
            }
            else
            {
                var allItems = Resources.LoadAll<ItemData>("");
                if (allItems != null) itemDatabase.AddRange(allItems);
            }
        }

        foreach (var item in itemDatabase)
        {
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                itemLookup[item.id] = item;
            }
        }
    }

    public void Save()
    {
        var saveData = new InventorySaveData();
        foreach (var slot in slots)
        {
            if (slot?.itemData != null && slot.count > 0)
            {
                saveData.slots.Add(new InventorySlotSaveData(slot.itemData.id, slot.count));
            }
        }
        SaveManager.SaveInventory(saveData);
    }

    public void Load()
    {
        var saveData = SaveManager.LoadInventory();
        if (saveData == null || saveData.slots == null || saveData.slots.Count == 0) return;

        slots.Clear();
        foreach (var slotData in saveData.slots)
        {
            if (itemLookup.TryGetValue(slotData.itemId, out var item))
            {
                slots.Add(new InventorySlot(item, slotData.count));
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (!string.IsNullOrEmpty(item.id))
        {
            itemLookup[item.id] = item;
            if (!itemDatabase.Contains(item)) itemDatabase.Add(item);
        }

        int totalAmount = amount;

        // 기존 아이템이 있는 슬롯을 찾아서 최대 스택을 마저 채움
        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            if (slots[i].itemData != item) continue;

            int space = item.maxStack - slots[i].count;
            if (space <= 0) continue;

            int add = Mathf.Min(space, amount);
            slots[i].count += add;
            amount -= add;
        }

        // 새로운 슬롯이 필요한 상태
        while (amount > 0)
        {
            int add = Mathf.Min(item.maxStack, amount);
            slots.Add(new InventorySlot(item, add));
            amount -= add;
        }

        Debug.Log($"{item.name} {totalAmount}개 획득");
        OnInventoryChanged?.Invoke();
        Save();
    }

    public bool RemoveItemAt(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || amount <= 0) return false;
        if (slots[slotIndex].count < amount) return false;

        slots[slotIndex].count -= amount;
        if (slots[slotIndex].count == 0)
        {
            slots.RemoveAt(slotIndex);
        }

        OnInventoryChanged?.Invoke();
        Save();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (!HasItem(item, amount)) return false;
        int totalAmount = amount;

        // 마지막 슬롯부터 검사해서 사실상 가장 적은 슬롯 부터 차감
        for (int i = slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].itemData != item) continue;

            if (slots[i].count <= amount)
            {
                amount -= slots[i].count;
                slots.RemoveAt(i);
            }
            else
            {
                slots[i].count -= amount;
                amount = 0;
            }
        }

        Debug.Log($"{item.name} {totalAmount}개 차감");
        OnInventoryChanged?.Invoke();
        Save();
        return true;
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].itemData == item)
            {
                total += slots[i].count;
                if (total >= amount) return true;
            }
        }
        return false;
    }
}
