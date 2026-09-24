using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    public class AugmentChoiceCardView : MonoBehaviour
    {
        public Button cardButton;
        public Image background;
        public Image icon;
        public TMP_Text titleText;
        public TMP_Text rarityText;
        public TMP_Text descriptionText;
        public Button confirmButton;

        public void Bind(AugmentData data, System.Action<AugmentData> onSelect)
        {
            titleText.text = data.displayName;
            rarityText.text = data.rarity.ToString();
            descriptionText.text = BuildDescription(data);

            icon.enabled = data.icon != null;
            icon.sprite = data.icon;

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => onSelect?.Invoke(data));
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => onSelect?.Invoke(data));
            }
        }

        private static string BuildDescription(AugmentData data)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(data.description))
                parts.Add(data.description);

            foreach (var modifier in data.modifiers)
                parts.Add($"{modifier.statKey} {modifier.operation} {modifier.value}");

            if (!string.IsNullOrWhiteSpace(data.rewardItemId) && data.rewardAmount > 0)
                parts.Add($"보상 아이템: {data.rewardItemId} x{data.rewardAmount}");

            return string.Join("\n", parts);
        }
    }
}
