using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    public class TraitEntryView : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text rankText;
        public TMP_Text descText;
        public Button investButton;
        public TMP_Text buttonLabel;

        public void Bind(TraitData data, int currentRank, int availablePoints, bool canInvest, System.Action<TraitData> onInvest)
        {
            nameText.text = data.displayName;
            rankText.text = $"Lv {currentRank} / {Mathf.Max(1, data.maxRank)}";
            descText.text = BuildDescription(data, currentRank);

            icon.enabled = data.icon != null;
            icon.sprite = data.icon;

            buttonLabel.text = canInvest ? $"투자 ({Mathf.Max(1, data.pointCost)}P)" : "최대";
            investButton.interactable = canInvest;
            investButton.onClick.RemoveAllListeners();
            investButton.onClick.AddListener(() => onInvest?.Invoke(data));
        }

        private static string BuildDescription(TraitData data, int currentRank)
        {
            string text = string.IsNullOrWhiteSpace(data.description) ? "" : data.description;
            foreach (var modifier in data.modifiersPerRank)
            {
                string line = $"{modifier.statKey} {modifier.operation} {modifier.value} / 랭크";
                text = string.IsNullOrWhiteSpace(text) ? line : $"{text}\n{line}";
            }

            return text;
        }
    }
}
