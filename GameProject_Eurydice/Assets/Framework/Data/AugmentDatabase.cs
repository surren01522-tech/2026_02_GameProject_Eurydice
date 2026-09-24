using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    [CreateAssetMenu(menuName = "GameFramework/Augment Database", fileName = "AugmentDatabase")]
    public class AugmentDatabase : ScriptableObject
    {
        public List<AugmentData> augments = new();

        private Dictionary<string, AugmentData> _map;

        public AugmentData Get(string id)
        {
            _map ??= Build();
            return _map.TryGetValue(id, out var augment) ? augment : null;
        }

        private Dictionary<string, AugmentData> Build()
        {
            var map = new Dictionary<string, AugmentData>();
            foreach (var augment in augments)
            {
                if (augment != null && !string.IsNullOrWhiteSpace(augment.id))
                    map[augment.id] = augment;
            }

            return map;
        }
    }
}
