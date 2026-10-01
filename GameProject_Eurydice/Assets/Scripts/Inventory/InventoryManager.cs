using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<InventorySlot> slots = new();

    public IReadOnlyList<InventorySlot> Slots => slots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;
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
