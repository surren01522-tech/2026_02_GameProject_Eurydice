using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    /// <summary>모든 아이템 정의를 담는 DB. Resources/ItemDatabase 에 생성.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new();

        private Dictionary<string, ItemData> _map;

        public ItemData Get(string id)
        {
            _map ??= Build();
            return _map.TryGetValue(id, out var item) ? item : null;
        }

        private Dictionary<string, ItemData> Build()
        {
            var map = new Dictionary<string, ItemData>();
            foreach (var i in items)
                if (i != null && !string.IsNullOrEmpty(i.id)) map[i.id] = i;
            return map;
        }
    }
}
