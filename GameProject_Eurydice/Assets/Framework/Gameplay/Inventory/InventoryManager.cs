using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
    }

    /// <summary>
    /// 인벤토리(가방) 시스템. ISavable 구현으로 자동 저장 대상.
    /// Resources/ItemDatabase.asset 자동 로드.
    /// 사용 예)
    ///   InventoryManager.Instance.AddItem("potion", 3);
    ///   InventoryManager.Instance.RemoveItem("potion", 1);
    ///   int n = InventoryManager.Instance.GetCount("potion");
    /// UI는 EventBus의 ItemAddedEvent / InventoryChangedEvent 구독으로 갱신.
    /// </summary>
    public class InventoryManager : MonoSingleton<InventoryManager>, ISavable
    {
        [SerializeField] private ItemDatabase database;

        private readonly List<ItemStack> _stacks = new();

        public IReadOnlyList<ItemStack> Stacks => _stacks;
        public ItemDatabase Database => database;

        public string SaveKey => "inventory";

        protected override void OnInitialize()
        {
            if (database == null) database = Resources.Load<ItemDatabase>("ItemDatabase");
            SaveManager.Instance.Register(this);
        }

        // ===================== 조회 =====================

        public int GetCount(string itemId)
        {
            int total = 0;
            foreach (var s in _stacks)
                if (s.itemId == itemId) total += s.count;
            return total;
        }

        public bool Has(string itemId, int amount = 1) => GetCount(itemId) >= amount;

        // ===================== 추가 =====================

        public bool AddItem(string itemId, int amount = 1)
        {
            if (amount <= 0) return false;

            var data = database?.Get(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[Inventory] 아이템 정의 없음: {itemId}");
                return false;
            }

            int remaining = amount;

            // 기존 스택 채우기
            foreach (var s in _stacks)
            {
                if (s.itemId != itemId || s.count >= data.maxStack) continue;
                int add = Mathf.Min(remaining, data.maxStack - s.count);
                s.count += add;
                remaining -= add;
                if (remaining <= 0) break;
            }

            // 새 스택 생성
            while (remaining > 0)
            {
                int add = Mathf.Min(remaining, data.maxStack);
                _stacks.Add(new ItemStack { itemId = itemId, count = add });
                remaining -= add;
            }

            EventBus.Publish(new ItemAddedEvent { ItemId = itemId, Amount = amount, TotalCount = GetCount(itemId) });
            EventBus.Publish(new InventoryChangedEvent());
            EventBus.Publish(new GameplayEvent("item_collect", amount, itemId)); // 업적 연동
            return true;
        }

        // ===================== 제거 =====================

        public bool RemoveItem(string itemId, int amount = 1)
        {
            if (amount <= 0 || !Has(itemId, amount)) return false;

            int remaining = amount;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var s = _stacks[i];
                if (s.itemId != itemId) continue;

                int remove = Mathf.Min(remaining, s.count);
                s.count -= remove;
                remaining -= remove;
                if (s.count <= 0) _stacks.RemoveAt(i);
            }

            EventBus.Publish(new ItemRemovedEvent { ItemId = itemId, Amount = amount, TotalCount = GetCount(itemId) });
            EventBus.Publish(new InventoryChangedEvent());
            return true;
        }

        public void Clear()
        {
            _stacks.Clear();
            EventBus.Publish(new InventoryChangedEvent());
        }

        // ===================== 정렬 =====================

        public void SortByType()
        {
            _stacks.Sort((a, b) =>
            {
                var da = database.Get(a.itemId);
                var db = database.Get(b.itemId);
                int cmp = (da?.type ?? 0).CompareTo(db?.type ?? 0);
                return cmp != 0 ? cmp : string.Compare(a.itemId, b.itemId, StringComparison.Ordinal);
            });
            EventBus.Publish(new InventoryChangedEvent());
        }

        // ===================== 저장 =====================

        [Serializable] private class SaveData { public List<ItemStack> stacks; }

        public string CaptureState()
            => JsonUtility.ToJson(new SaveData { stacks = _stacks.ToList() });

        public void RestoreState(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _stacks.Clear();
            if (data?.stacks != null) _stacks.AddRange(data.stacks);
            EventBus.Publish(new InventoryChangedEvent());
        }
    }
}
