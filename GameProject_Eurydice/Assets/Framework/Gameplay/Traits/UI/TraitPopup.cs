using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    public class TraitPopup : UIPanel
    {
        [Header("Auto Wiring (Template Generator)")]
        public TMP_Text titleText;
        public TMP_Text pointsText;
        public RectTransform contentRoot;
        public TraitEntryView entryTemplate;
        public Button closeButton;

        private readonly List<TraitEntryView> _views = new();

        private void Awake()
        {
            EnsureBuilt();
            UIFontUtility.ApplyToHierarchy(transform);
            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            EventBus.Subscribe<TraitPointsChangedEvent>(OnPointsChanged);
            EventBus.Subscribe<TraitInvestedEvent>(OnTraitInvested);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TraitPointsChangedEvent>(OnPointsChanged);
            EventBus.Unsubscribe<TraitInvestedEvent>(OnTraitInvested);
        }

        public override void OnShow(object args)
        {
            EnsureBuilt();
            UIFontUtility.ApplyToHierarchy(transform);
            Rebuild();
        }

        private void OnPointsChanged(TraitPointsChangedEvent e)
        {
            if (gameObject.activeInHierarchy)
                Rebuild();
        }

        private void OnTraitInvested(TraitInvestedEvent e)
        {
            if (gameObject.activeInHierarchy)
                Rebuild();
        }

        private void Rebuild()
        {
            var mgr = TraitManager.Instance;
            var all = mgr.Database != null ? mgr.Database.traits : new List<TraitData>();
            pointsText.text = $"포인트: {mgr.AvailablePoints}";

            while (_views.Count < all.Count)
            {
                var view = Instantiate(entryTemplate, contentRoot);
                view.gameObject.SetActive(true);
                _views.Add(view);
            }

            int shown = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var trait = all[i];
                if (trait == null)
                    continue;

                var view = _views[shown];
                view.gameObject.SetActive(true);
                view.Bind(trait, mgr.GetRank(trait.id), mgr.AvailablePoints, mgr.CanInvest(trait.id), Invest);
                shown++;
            }

            for (int i = shown; i < _views.Count; i++)
                _views[i].gameObject.SetActive(false);
        }

        private void Invest(TraitData trait)
        {
            if (trait == null)
                return;

            TraitManager.Instance.Invest(trait.id);
        }

        private void EnsureBuilt()
        {
            if (titleText != null && contentRoot != null && entryTemplate != null)
                return;

            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            gameObject.name = "TraitPopup";
            var dim = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            var panel = CreateRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(980f, 720f);
            panel.gameObject.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 0.98f);

            titleText = CreateText("Title", panel, "특성", 32f, FontStyles.Bold, TextAlignmentOptions.Center);
            Anchor(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -68f), new Vector2(0f, -12f));

            pointsText = CreateText("Points", panel, "포인트: 0", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(pointsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -112f), new Vector2(-24f, -78f));

            closeButton = CreateButton("CloseButton", panel, "닫기", new Vector2(108f, 44f), Close);
            Anchor(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -60f), new Vector2(-24f, -16f));

            var scroll = CreateRect("Scroll", panel);
            Anchor(scroll, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 24f), new Vector2(-24f, -132f));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = CreateRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            contentRoot = CreateRect("Content", viewport);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);

            var layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;
            contentRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = contentRoot;

            entryTemplate = CreateEntry(contentRoot);
            entryTemplate.gameObject.SetActive(false);
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

        private static TraitEntryView CreateEntry(RectTransform parent)
        {
            var root = CreateRect("TraitEntryTemplate", parent);
            root.sizeDelta = new Vector2(0f, 142f);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 142f;
            layout.flexibleWidth = 1f;
            root.gameObject.AddComponent<Image>().color = new Color(0.19f, 0.2f, 0.24f, 1f);

            var view = root.gameObject.AddComponent<TraitEntryView>();

            var icon = CreateRect("Icon", root);
            Anchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -40f), new Vector2(98f, 40f));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var name = CreateText("Name", root, "?뱀꽦 ?대쫫", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
            Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(114f, -44f), new Vector2(-200f, -10f));
            view.nameText = name;

            var rank = CreateText("Rank", root, "Lv 0 / 5", 18f, FontStyles.Bold, TextAlignmentOptions.Right);
            Anchor(rank.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -42f), new Vector2(-24f, -14f));
            view.rankText = rank;

            var desc = CreateText("Description", root, "?ㅻ챸", 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            desc.enableWordWrapping = true;
            Anchor(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(114f, 18f), new Vector2(-200f, -52f));
            view.descText = desc;

            var invest = CreateButton("InvestButton", root, "?ъ옄", new Vector2(150f, 48f), null);
            Anchor(invest.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-174f, -24f), new Vector2(-24f, 24f));
            view.investButton = invest;
            view.buttonLabel = invest.GetComponentInChildren<TextMeshProUGUI>(true);

            return view;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
