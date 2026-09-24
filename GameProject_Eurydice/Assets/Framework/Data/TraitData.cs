using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Data
{
    [CreateAssetMenu(menuName = "GameFramework/Trait Data", fileName = "Trait_")]
    public class TraitData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public int maxRank = 5;
        public int pointCost = 1;
        public List<GameplayModifier> modifiersPerRank = new();
    }
}
