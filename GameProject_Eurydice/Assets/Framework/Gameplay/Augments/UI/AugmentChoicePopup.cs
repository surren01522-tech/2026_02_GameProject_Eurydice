using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    public class AugmentChoicePopup : UIPanel
    {
        [Header("Auto Wiring (Template Generator)")]
        public TMP_Text titleText;
        public Button closeButton;
        public List<AugmentChoiceCardView> choiceViews = new();

        private void Awake()
        {
            EnsureBuilt();
            UIFontUtility.ApplyToHierarchy(transform);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        public override void OnShow(object args)
        {
            EnsureBuilt();
            UIFontUtility.ApplyToHierarchy(transform);

            var choices = args as IReadOnlyList<AugmentData> ?? AugmentManager.Instance.CurrentChoices;
            Rebuild(choices);
        }

        public override bool OnBackRequested()
        {
            return AugmentManager.Instance.CurrentChoices.Count == 0;
        }

        private void Rebuild(IReadOnlyList<AugmentData> choices)
        {
            for (int i = 0; i < choiceViews.Count; i++)
                choiceViews[i].gameObject.SetActive(i < choices.Count);

            for (int i = 0; i < choices.Count && i < choiceViews.Count; i++)
                choiceViews[i].Bind(choices[i], SelectAugment);
        }

        private void SelectAugment(AugmentData data)
        {
            if (data == null)
                return;

            if (AugmentManager.Instance.SelectAugment(data.id))
                Close();
        }

        private void EnsureBuilt()
        {
            if (titleText != null && choiceViews.Count > 0)
                return;

            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            gameObject.name = "AugmentChoicePopup";
            var dim = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            var panel = CreateRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1180f, 560f);
            panel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.17f, 0.98f);

            titleText = CreateText("Title", panel, "증강 선택", 34, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(titleText.rectTransform, 0f, 0f, -72f, 64f);

            closeButton = CreateButton("CloseButton", panel, "X", new Vector2(56f, 44f), Close);
            Anchor(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-68f, -58f), new Vector2(-12f, -14f));

            var row = CreateRect("Choices", panel);
            Anchor(row, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 32f), new Vector2(-28f, -96f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < 3; i++)
                choiceViews.Add(CreateChoice(row, i));
        }

        private AugmentChoiceCardView CreateChoice(RectTransform parent, int index)
        {
            var card = CreateRect($"Choice_{index + 1}", parent);
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.19f, 0.24f, 1f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var view = card.gameObject.AddComponent<AugmentChoiceCardView>();
            view.background = bg;
            view.cardButton = button;

            var icon = CreateRect("Icon", card);
            icon.sizeDelta = new Vector2(72f, 72f);
            Anchor(icon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -96f), new Vector2(90f, -24f));
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.raycastTarget = false;
            view.icon = iconImage;

            var title = CreateText("Title", card, "증강 이름", 26, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -88f), new Vector2(-16f, -24f));
            view.titleText = title;

            var rarity = CreateText("Rarity", card, "Common", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            rarity.color = new Color(0.95f, 0.82f, 0.32f);
            Anchor(rarity.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -126f), new Vector2(-16f, -96f));
            view.rarityText = rarity;

            var desc = CreateText("Description", card, "설명", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            desc.enableWordWrapping = true;
            Anchor(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 78f), new Vector2(-18f, -144f));
            view.descriptionText = desc;

            var confirm = CreateButton("PickButton", card, "선택", new Vector2(0f, 52f), null);
            Anchor(confirm.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 16f), new Vector2(-18f, 68f));
            view.confirmButton = confirm;
            return view;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text CreateText(string name, RectTransform parent, string content, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = UIFontUtility.TmpFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, RectTransform parent, string label, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.28f, 0.32f, 0.42f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var text = CreateText("Label", rect, label, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchTop(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, top - height);
            rect.offsetMax = new Vector2(-right, top);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offMin;
            rect.offsetMax = offMax;
        }

    }
}
