using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Core;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Achievement list popup template.
    /// </summary>
    public class AchievementListPopup : UIPanel
    {
        [Header("Auto Wiring (Template Generator)")]
        public Transform listParent;
        public AchievementEntryView entryTemplate;
        public TMP_Text titleTmpText;
        public Text titleText;
        public Button closeButton;

        private readonly List<AchievementEntryView> _views = new();

        private void Awake()
        {
            UIFontUtility.ApplyToHierarchy(transform);
            if (titleTmpText != null && string.IsNullOrWhiteSpace(titleTmpText.text))
                titleTmpText.text = "업적";
            if (titleText != null && string.IsNullOrWhiteSpace(titleText.text))
                titleText.text = "업적";

            if (listParent != null && listParent.TryGetComponent(out VerticalLayoutGroup layout))
            {
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }

            if (entryTemplate != null)
                StretchEntry(entryTemplate.transform as RectTransform);

            entryTemplate.gameObject.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public override void OnShow(object args)
        {
            EventBus.Subscribe<AchievementProgressEvent>(OnProgress);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnUnlocked);
            Rebuild();
        }

        public override void OnHide()
        {
            EventBus.Unsubscribe<AchievementProgressEvent>(OnProgress);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnUnlocked);
        }

        private void OnProgress(AchievementProgressEvent e) => Rebuild();
        private void OnUnlocked(AchievementUnlockedEvent e) => Rebuild();

        private void Rebuild()
        {
            var mgr = AchievementManager.Instance;
            var all = mgr.AllData;

            while (_views.Count < all.Count)
            {
                var view = Instantiate(entryTemplate, listParent);
                StretchEntry(view.transform as RectTransform);
                view.gameObject.SetActive(true);
                _views.Add(view);
            }

            int shown = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var data = all[i];
                if (data == null)
                    continue;

                var (cur, target) = mgr.GetProgressInfo(data.id);
                _views[shown].gameObject.SetActive(true);
                _views[shown].Set(data, cur, target, mgr.IsUnlocked(data.id));
                shown++;
            }

            for (int i = shown; i < _views.Count; i++)
                _views[i].gameObject.SetActive(false);
        }

        private static void StretchEntry(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);

            if (rect.TryGetComponent(out LayoutElement element))
                element.flexibleWidth = 1f;
        }
    }
}
