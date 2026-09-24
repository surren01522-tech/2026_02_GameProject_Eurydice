using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    [CreateAssetMenu(menuName = "GameFramework/Trait Database", fileName = "TraitDatabase")]
    public class TraitDatabase : ScriptableObject
    {
        public List<TraitData> traits = new();

        private Dictionary<string, TraitData> _map;

        public TraitData Get(string id)
        {
            _map ??= Build();
            return _map.TryGetValue(id, out var trait) ? trait : null;
        }

        private Dictionary<string, TraitData> Build()
        {
            var map = new Dictionary<string, TraitData>();
            foreach (var trait in traits)
            {
                if (trait != null && !string.IsNullOrWhiteSpace(trait.id))
                    map[trait.id] = trait;
            }

            return map;
        }
    }
}
