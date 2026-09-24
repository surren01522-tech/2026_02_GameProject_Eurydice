using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    /// <summary>모든 업적 정의를 담는 DB. Resources/AchievementDatabase 에 생성.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Achievement Database", fileName = "AchievementDatabase")]
    public class AchievementDatabase : ScriptableObject
    {
        public List<AchievementData> achievements = new();
    }
}
