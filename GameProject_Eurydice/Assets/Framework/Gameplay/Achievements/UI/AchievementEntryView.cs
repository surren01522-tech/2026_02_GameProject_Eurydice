using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// 업적 리스트의 한 줄. 그림 교체 포인트:
    /// - background / icon / 진행바(barBg, barFill) / 폰트
    /// 진행바는 스프라이트 없이 anchorMax로 채우므로 아무 스프라이트나 끼워도 동작.
    /// </summary>
    public class AchievementEntryView : MonoBehaviour
    {
        public Image background;
        public Image icon;
        public Text nameText;
        public Text descText;
        public Image barBg;
        public RectTransform barFill;
        public Text progressText;
        public Text unlockedMark;

        public void Set(AchievementData data, int current, int target, bool unlocked)
        {
            nameText.text = data.displayName;
            descText.text = data.description;

            icon.enabled = data.icon != null;
            icon.sprite = data.icon;

            float pct = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
            barFill.anchorMax = new Vector2(unlocked ? 1f : pct, 1f);

            progressText.text = unlocked ? "완료" : $"{current} / {target}";
            unlockedMark.enabled = unlocked;

            // 달성 시 살짝 강조, 미달성은 톤 다운
            var c = background.color;
            background.color = new Color(c.r, c.g, c.b, unlocked ? 1f : 0.75f);
        }
    }
}
