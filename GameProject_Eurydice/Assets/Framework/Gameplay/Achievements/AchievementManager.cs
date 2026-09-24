using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 업적 시스템. GameplayEvent를 구독해 자동으로 진행도를 갱신한다.
    /// Resources/AchievementDatabase.asset 자동 로드. ISavable 구현.
    /// 게임 코드에서는 이벤트만 쏘면 된다:
    ///   EventBus.Publish(new GameplayEvent("enemy_kill", 1, "slime"));
    /// </summary>
    public class AchievementManager : MonoSingleton<AchievementManager>, ISavable
    {
        [SerializeField] private AchievementDatabase database;

        private readonly Dictionary<string, int> _progress = new();
        private readonly HashSet<string> _unlocked = new();

        public string SaveKey => "achievements";

        protected override void OnInitialize()
        {
            if (database == null) database = Resources.Load<AchievementDatabase>("AchievementDatabase");
            SaveManager.Instance.Register(this);
            EventBus.Subscribe<GameplayEvent>(OnGameplayEvent);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<GameplayEvent>(OnGameplayEvent);
            base.OnDestroy();
        }

        private void OnGameplayEvent(GameplayEvent evt)
        {
            if (database == null) return;

            foreach (var achv in database.achievements)
            {
                if (achv == null || _unlocked.Contains(achv.id)) continue;
                if (achv.eventKey != evt.Key) continue;
                if (!string.IsNullOrEmpty(achv.paramFilter) && achv.paramFilter != evt.Param) continue;

                int current = _progress.GetValueOrDefault(achv.id, 0);
                current = achv.conditionType == AchievementConditionType.SingleTrigger
                    ? achv.targetCount
                    : current + evt.Amount;

                _progress[achv.id] = current;
                EventBus.Publish(new AchievementProgressEvent
                {
                    AchievementId = achv.id, Current = Mathf.Min(current, achv.targetCount), Target = achv.targetCount
                });

                if (current >= achv.targetCount) Unlock(achv);
            }
        }

        private void Unlock(AchievementData achv)
        {
            _unlocked.Add(achv.id);
            Debug.Log($"[Achievement] 달성: {achv.displayName}");
            EventBus.Publish(new AchievementUnlockedEvent { AchievementId = achv.id });

            // 보상 지급
            if (!string.IsNullOrEmpty(achv.rewardItemId) && achv.rewardAmount > 0)
                InventoryManager.Instance.AddItem(achv.rewardItemId, achv.rewardAmount);
        }

        // ===================== 조회 =====================

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        /// <summary>업적 정의 조회 (UI용)</summary>
        public AchievementData GetData(string id)
            => database?.achievements.Find(a => a != null && a.id == id);

        /// <summary>전체 업적 정의 (리스트 UI용)</summary>
        public System.Collections.Generic.IReadOnlyList<AchievementData> AllData
            => database != null ? database.achievements : new System.Collections.Generic.List<AchievementData>();

        public int GetProgress(string id) => _progress.GetValueOrDefault(id, 0);

        public (int current, int target) GetProgressInfo(string id)
        {
            var achv = database?.achievements.Find(a => a != null && a.id == id);
            if (achv == null) return (0, 0);
            return (Mathf.Min(GetProgress(id), achv.targetCount), achv.targetCount);
        }

        // ===================== 저장 =====================

        [Serializable] private class SaveData
        {
            public List<string> unlockedIds = new();
            public List<string> progressKeys = new();
            public List<int> progressValues = new();
        }

        public string CaptureState()
        {
            var data = new SaveData();
            data.unlockedIds.AddRange(_unlocked);
            foreach (var kv in _progress)
            {
                data.progressKeys.Add(kv.Key);
                data.progressValues.Add(kv.Value);
            }
            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _unlocked.Clear();
            _progress.Clear();
            if (data == null) return;

            foreach (var id in data.unlockedIds) _unlocked.Add(id);
            for (int i = 0; i < data.progressKeys.Count; i++)
                _progress[data.progressKeys[i]] = data.progressValues[i];
        }
    }
}
