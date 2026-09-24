using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    [System.Serializable]
    public class EffectEntry
    {
        public string id;
        public GameObject prefab;         // ParticleSystem 포함 프리팹
        public float lifetime = -1f;      // -1이면 파티클 duration 기준 자동 계산
    }

    /// <summary>이펙트를 문자열 ID로 조회하는 라이브러리. Resources/EffectLibrary 에 생성.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Effect Library", fileName = "EffectLibrary")]
    public class EffectLibrary : ScriptableObject
    {
        public List<EffectEntry> effects = new();

        private Dictionary<string, EffectEntry> _map;

        public EffectEntry Get(string id)
        {
            _map ??= Build();
            return _map.TryGetValue(id, out var e) ? e : null;
        }

        private Dictionary<string, EffectEntry> Build()
        {
            var map = new Dictionary<string, EffectEntry>();
            foreach (var e in effects)
                if (!string.IsNullOrEmpty(e.id)) map[e.id] = e;
            return map;
        }
    }
}
