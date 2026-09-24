using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;
        public bool randomPitch;          // ±0.1 랜덤 피치 (타격음 등에 유용)
    }

    /// <summary>사운드를 문자열 ID로 조회하는 라이브러리. Resources/SoundLibrary 에 생성.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        public List<SoundEntry> sounds = new();

        private Dictionary<string, SoundEntry> _map;

        public SoundEntry Get(string id)
        {
            _map ??= Build();
            return _map.TryGetValue(id, out var e) ? e : null;
        }

        private Dictionary<string, SoundEntry> Build()
        {
            var map = new Dictionary<string, SoundEntry>();
            foreach (var s in sounds)
                if (!string.IsNullOrEmpty(s.id)) map[s.id] = s;
            return map;
        }
    }
}
