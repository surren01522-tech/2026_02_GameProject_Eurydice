using UnityEngine;

namespace GameFramework.Data
{
    public enum ItemType { Consumable, Equipment, Material, Quest, Currency }

    [CreateAssetMenu(menuName = "GameFramework/Item Data", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public ItemType type;
        public int maxStack = 99;
        public int sellPrice;
    }
}
