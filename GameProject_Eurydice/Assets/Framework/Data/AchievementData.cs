using UnityEngine;

namespace GameFramework.Data
{
    public enum AchievementConditionType
    {
        CumulativeCount,   // 누적 카운트 (예: 적 100마리 처치)
        SingleTrigger      // 1회 달성 (예: 첫 클리어)
    }

    [CreateAssetMenu(menuName = "GameFramework/Achievement Data", fileName = "Achv_")]
    public class AchievementData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("달성 조건")]
        public AchievementConditionType conditionType;
        [Tooltip("구독할 GameplayEvent의 Key (예: enemy_kill)")]
        public string eventKey;
        [Tooltip("Param 필터. 비워두면 모든 Param 허용 (예: 특정 적 ID만 카운트)")]
        public string paramFilter;
        public int targetCount = 1;

        [Header("보상 (선택)")]
        public string rewardItemId;
        public int rewardAmount;
    }
}
