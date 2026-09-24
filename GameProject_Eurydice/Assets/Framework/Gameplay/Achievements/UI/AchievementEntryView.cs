using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// A single row in the achievement list popup.
    /// </summary>
    public class AchievementEntryView : MonoBehaviour
    {
        public Image background;
        public Image icon;
        public TMP_Text nameTmpText;
        public Text nameText;
        public TMP_Text descTmpText;
        public Text descText;
        public Image barBg;
        public RectTransform barFill;
        public TMP_Text progressTmpText;
        public Text progressText;
        public TMP_Text unlockedMarkTmpText;
        public Text unlockedMark;

        private void Awake()
        {
            UIFontUtility.ApplyToHierarchy(transform);
        }

        public void Set(AchievementData data, int current, int target, bool unlocked)
        {
            SetText(nameTmpText, nameText, data.displayName);
            SetText(descTmpText, descText, data.description);

            icon.enabled = data.icon != null;
            icon.sprite = data.icon;

            float pct = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
            barFill.anchorMax = new Vector2(unlocked ? 1f : pct, 1f);

            SetText(progressTmpText, progressText, unlocked ? "완료" : $"{current} / {target}");
            SetEnabled(unlockedMarkTmpText, unlockedMark, unlocked);

            var c = background.color;
            background.color = new Color(c.r, c.g, c.b, unlocked ? 1f : 0.75f);
        }

        private static void SetText(TMP_Text tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
                tmpText.text = value;
            if (legacyText != null)
                legacyText.text = value;
        }

        private static void SetEnabled(TMP_Text tmpText, Text legacyText, bool enabled)
        {
            if (tmpText != null)
                tmpText.enabled = enabled;
            if (legacyText != null)
                legacyText.enabled = enabled;
        }
    }
}
