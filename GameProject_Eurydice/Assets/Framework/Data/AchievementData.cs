using UnityEngine;

namespace GameFramework.Data
{
    public enum AchievementConditionType
    {
        CumulativeCount,
        SingleTrigger
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

        [Tooltip("구독할 GameplayEvent의 Key 값입니다. 예: enemy_kill")]
        public string eventKey;

        [Tooltip("선택 필터입니다. 비워두면 모든 Param을 허용합니다.")]
        public string paramFilter;

        public int targetCount = 1;

        [Header("보상 (선택)")]
        public string rewardItemId;
        public int rewardAmount;
    }
}
