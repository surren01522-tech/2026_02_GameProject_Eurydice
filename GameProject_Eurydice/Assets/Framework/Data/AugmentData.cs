using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    public enum AugmentRarity
    {
        Common,
        Rare,
        Epic
    }

    [CreateAssetMenu(menuName = "GameFramework/Augment Data", fileName = "Augment_")]
    public class AugmentData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public AugmentRarity rarity = AugmentRarity.Common;
        public bool unique = true;
        public int maxPickCount = 1;
        public List<GameplayModifier> modifiers = new();

        [Header("Optional Reward")]
        public string rewardItemId;
        public int rewardAmount;
    }
}
