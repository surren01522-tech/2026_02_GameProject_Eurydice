using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework.Services;
using GameFramework.Gameplay;
using GameFramework.Data;

namespace GameFramework.EditorTools
{
    /// <summary>
    /// Generates the default UI prefabs used by the framework.
    /// Generated prefabs are saved under Assets/Resources/UI so UIManager can load them.
    /// </summary>
    public static class UITemplateGenerator
    {
        private const string Dir = "Assets/Resources/UI";
        private const string FontResourcePath = "Fonts/NotoSansKR-VF";
        [MenuItem("Tools/GameFramework/UI 템플릿 생성/인벤토리 팝업")]
        public static void CreateInventoryPopup()
        {
            EnsureDir();
            var root = NewRect("InventoryPopup");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var popup = root.gameObject.AddComponent<InventoryPopup>();

            var panel = NewRect("Panel", root);
            panel.sizeDelta = new Vector2(860, 620);
            panel.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 0.98f);

            var title = MakeText("Title", panel, "인벤토리", 32, FontStyle.Bold);
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -68), Vector2.zero);
            popup.titleTmpText = title.GetComponent<TextMeshProUGUI>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(56, 48));
            Anchor(close, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-68, -58), new Vector2(-12, -10));
            popup.closeButton = close.GetComponent<Button>();

            var sort = MakeButton("SortButton", panel, "정렬", new Vector2(120, 50));
            Anchor(sort, Vector2.zero, Vector2.zero, new Vector2(22, 16), new Vector2(142, 66));
            popup.sortButton = sort.GetComponent<Button>();

            var scroll = NewRect("Scroll", panel);
            Anchor(scroll, Vector2.zero, Vector2.one, new Vector2(22, 80), new Vector2(-22, -82));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            content.gameObject.AddComponent<GridLayoutGroup>();
            var auto = content.gameObject.AddComponent<AutoGridCellSize>();
            auto.columns = 6;
            auto.spacing = 12f;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            popup.slotParent = content;

            var slot = NewRect("SlotTemplate", content);
            var slotBg = slot.gameObject.AddComponent<Image>();
            slotBg.color = new Color(0.24f, 0.24f, 0.28f);
            var view = slot.gameObject.AddComponent<InventorySlotView>();
            view.background = slotBg;

            var icon = NewRect("Icon", slot);
            Anchor(icon, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var count = MakeText("Count", slot, "99", 22, FontStyle.Bold);
            Anchor(count, Vector2.zero, new Vector2(1, 0), new Vector2(0, 2), new Vector2(-8, 28));
            var countText = count.GetComponent<TextMeshProUGUI>();
            countText.alignment = TextAlignmentOptions.BottomRight;
            view.countTmpText = countText;

            slot.gameObject.SetActive(false);
            popup.slotTemplate = view;

            SaveAsPrefab(root.gameObject, "InventoryPopup");
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/설정 팝업")]
        public static void CreateSettingsPopup()
        {
            EnsureDir();
            var root = NewRect("SettingsPopup");
            Stretch(root);
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);

            var popup = root.gameObject.AddComponent<SettingsPopup>();

            var panel = NewRect("Panel", root);
            panel.sizeDelta = new Vector2(640, 440);
            panel.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 0.98f);

            var title = MakeText("Title", panel, "설정", 30, FontStyle.Bold);
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), Vector2.zero);
            popup.titleTmpText = title.GetComponent<TextMeshProUGUI>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(52, 44));
            Anchor(close, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-62, -54), new Vector2(-10, -10));
            popup.closeButton = close.GetComponent<Button>();

            popup.bgmSlider = MakeVolumeRow(panel, "BGM", -30);
            popup.sfxSlider = MakeVolumeRow(panel, "SFX", -130);

            SaveAsPrefab(root.gameObject, "SettingsPopup");
        }

        private static Slider MakeVolumeRow(RectTransform panel, string label, float y)
        {
            var row = NewRect($"Row_{label}", panel);
            Anchor(row, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(30, y - 24), new Vector2(-30, y + 24));

            var text = MakeText("Label", row, label, 22, FontStyle.Bold);
            Anchor(text, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(90, 0));
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            var sliderRoot = NewRect("Slider", row);
            Anchor(sliderRoot, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(100, -12), new Vector2(0, 12));
            var slider = sliderRoot.gameObject.AddComponent<Slider>();

            var bg = NewRect("Background", sliderRoot);
            Stretch(bg);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.12f);

            var fillArea = NewRect("Fill Area", sliderRoot);
            Anchor(fillArea, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
            var fill = NewRect("Fill", fillArea);
            Stretch(fill);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.42f, 0.68f, 1f);

            var handleArea = NewRect("Handle Slide Area", sliderRoot);
            Anchor(handleArea, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            var handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(22, 0);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/업적 리스트 팝업")]
        public static void CreateAchievementListPopup()
        {
            EnsureDir();
            var root = NewRect("AchievementListPopup");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var popup = root.gameObject.AddComponent<AchievementListPopup>();

            var panel = NewRect("Panel", root);
            panel.sizeDelta = new Vector2(780, 680);
            panel.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 0.98f);

            var title = MakeText("Title", panel, "업적", 30, FontStyle.Bold);
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), Vector2.zero);
            popup.titleTmpText = title.GetComponent<TextMeshProUGUI>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(52, 44));
            Anchor(close, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-62, -54), new Vector2(-10, -10));
            popup.closeButton = close.GetComponent<Button>();

            var scroll = NewRect("Scroll", panel);
            Anchor(scroll, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -74));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            popup.listParent = content;

            var entry = NewRect("EntryTemplate", content);
            entry.sizeDelta = new Vector2(0, 96);
            entry.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            var entryBg = entry.gameObject.AddComponent<Image>();
            entryBg.color = new Color(0.22f, 0.22f, 0.26f);
            var view = entry.gameObject.AddComponent<AchievementEntryView>();
            view.background = entryBg;

            var icon = NewRect("Icon", entry);
            Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -30), new Vector2(74, 30));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var name = MakeText("Name", entry, "업적 이름", 21, FontStyle.Bold);
            Anchor(name, new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -38), new Vector2(-90, -8));
            var nameText = name.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Left;
            view.nameTmpText = nameText;

            var desc = MakeText("Desc", entry, "설명", 15, FontStyle.Normal);
            Anchor(desc, new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -62), new Vector2(-90, -38));
            var descText = desc.GetComponent<TextMeshProUGUI>();
            descText.alignment = TextAlignmentOptions.Left;
            descText.color = new Color(0.75f, 0.75f, 0.8f);
            view.descTmpText = descText;

            var barBg = NewRect("BarBg", entry);
            Anchor(barBg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(88, 12), new Vector2(-90, 26));
            view.barBg = barBg.gameObject.AddComponent<Image>();
            view.barBg.color = new Color(0.1f, 0.1f, 0.12f);
            view.barBg.raycastTarget = false;

            var barFill = NewRect("BarFill", barBg);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(0.5f, 1f);
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;
            var fillImage = barFill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.55f, 0.9f, 0.37f);
            fillImage.raycastTarget = false;
            view.barFill = barFill;

            var progress = MakeText("Progress", entry, "0 / 10", 14, FontStyle.Normal);
            Anchor(progress, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-86, 8), new Vector2(-12, 30));
            var progressText = progress.GetComponent<TextMeshProUGUI>();
            progressText.alignment = TextAlignmentOptions.Right;
            view.progressTmpText = progressText;

            var unlocked = MakeText("UnlockedMark", entry, "★ 달성", 17, FontStyle.Bold);
            Anchor(unlocked, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-86, 6), new Vector2(-12, 34));
            var unlockedText = unlocked.GetComponent<TextMeshProUGUI>();
            unlockedText.alignment = TextAlignmentOptions.Right;
            unlockedText.color = new Color(1f, 0.83f, 0.35f);
            view.unlockedMarkTmpText = unlockedText;

            entry.gameObject.SetActive(false);
            popup.entryTemplate = view;

            SaveAsPrefab(root.gameObject, "AchievementListPopup");
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/업적 달성 토스트")]
        public static void CreateAchievementToast()
        {
            EnsureDir();
            var root = NewRect("AchievementToast");
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(440, 84);
            root.anchoredPosition = new Vector2(0, 120);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            var toast = root.gameObject.AddComponent<AchievementToastView>();
            toast.group = group;

            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.13f, 0.13f, 0.16f, 0.96f);
            bg.raycastTarget = false;
            toast.background = bg;

            var icon = NewRect("Icon", root);
            Anchor(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -26), new Vector2(66, 26));
            toast.icon = icon.gameObject.AddComponent<Image>();
            toast.icon.raycastTarget = false;

            var header = MakeText("Header", root, "업적 달성!", 15, FontStyle.Bold);
            Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(80, -36), new Vector2(-14, -8));
            var headerText = header.GetComponent<TextMeshProUGUI>();
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.color = new Color(1f, 0.83f, 0.35f);
            toast.headerTmpText = headerText;

            var name = MakeText("Name", root, "업적 이름", 21, FontStyle.Bold);
            Anchor(name, new Vector2(0, 0), new Vector2(1, 0), new Vector2(80, 8), new Vector2(-14, 44));
            var nameText = name.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Left;
            toast.nameTmpText = nameText;

            SaveAsPrefab(root.gameObject, "AchievementToast");
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/증강 선택 팝업")]
        public static void CreateAugmentChoicePopup()
        {
            EnsureDir();
            var root = NewRect("AugmentChoicePopup");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var popup = root.gameObject.AddComponent<AugmentChoicePopup>();

            var panel = NewRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1180f, 560f);
            panel.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.17f, 0.98f);

            var title = MakeText("Title", panel, "증강 선택", 34, FontStyle.Bold);
            Anchor(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), new Vector2(0f, -8f));
            popup.titleText = title.GetComponent<TextMeshProUGUI>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(56f, 44f));
            Anchor(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-68f, -58f), new Vector2(-12f, -14f));
            popup.closeButton = close.GetComponent<Button>();

            var row = NewRect("Choices", panel);
            Anchor(row, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 32f), new Vector2(-28f, -96f));
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16f;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;

            popup.choiceViews.Clear();
            for (int i = 0; i < 3; i++)
                popup.choiceViews.Add(CreateAugmentChoiceCard(row, i));

            SaveAsPrefab(root.gameObject, "AugmentChoicePopup");
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/특성 팝업")]
        public static void CreateTraitPopup()
        {
            EnsureDir();
            var root = NewRect("TraitPopup");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var popup = root.gameObject.AddComponent<TraitPopup>();

            var panel = NewRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(980f, 720f);
            panel.gameObject.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 0.98f);

            var title = MakeText("Title", panel, "특성", 32, FontStyle.Bold);
            Anchor(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -68f), new Vector2(0f, -12f));
            popup.titleText = title.GetComponent<TextMeshProUGUI>();

            var points = MakeText("Points", panel, "포인트: 0", 22, FontStyle.Bold);
            Anchor(points, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -112f), new Vector2(-24f, -78f));
            var pointsText = points.GetComponent<TextMeshProUGUI>();
            pointsText.alignment = TextAlignmentOptions.Left;
            popup.pointsText = pointsText;

            var close = MakeButton("CloseButton", panel, "닫기", new Vector2(108f, 44f));
            Anchor(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-132f, -60f), new Vector2(-24f, -16f));
            popup.closeButton = close.GetComponent<Button>();

            var scroll = NewRect("Scroll", panel);
            Anchor(scroll, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 24f), new Vector2(-24f, -132f));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            popup.contentRoot = content;

            popup.entryTemplate = CreateTraitEntryTemplate(content);
            popup.entryTemplate.gameObject.SetActive(false);

            SaveAsPrefab(root.gameObject, "TraitPopup");
        }

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/전체 UI 한 번에 생성")]
        public static void CreateAllUI()
        {
            CreateInventoryPopup();
            CreateSettingsPopup();
            CreateAchievementListPopup();
            CreateAchievementToast();
            CreateAugmentChoicePopup();
            CreateTraitPopup();
            Debug.Log("[GameFramework] UI 템플릿 6종 생성 완료 (Resources/UI/)");
        }

        [MenuItem("Tools/GameFramework/게임잼 기본 세팅")]
        public static void GameJamSetup()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            CreateAssetIfMissing<SoundLibrary>("Assets/Resources/SoundLibrary.asset");
            CreateAssetIfMissing<EffectLibrary>("Assets/Resources/EffectLibrary.asset");
            CreateAssetIfMissing<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
            CreateAssetIfMissing<AchievementDatabase>("Assets/Resources/AchievementDatabase.asset");
            CreateAssetIfMissing<AugmentDatabase>("Assets/Resources/AugmentDatabase.asset");
            CreateAssetIfMissing<TraitDatabase>("Assets/Resources/TraitDatabase.asset");

            CreateAllUI();

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "게임잼 기본 세팅 완료",
                "기본 데이터베이스 6종과 UI 템플릿이 준비되었습니다.\n이제 Framework Hub(Ctrl+Shift+G)에서 아이템, 업적, 증강, 특성, 사운드, 이펙트를 등록하세요.",
                "OK");
        }

        private static void CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        private static RectTransform NewRect(string name, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            if (parent != null)
                rect.SetParent(parent, false);
            return rect;
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

        private static RectTransform MakeText(string name, RectTransform parent, string content, int size, FontStyle style)
        {
            var rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = UIFontUtility.TmpFontAsset;
            text.fontSize = size;
            text.fontStyle = ToTmpFontStyle(style);
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            text.raycastTarget = false;
            return rect;
        }

        private static FontStyles ToTmpFontStyle(FontStyle style)
        {
            return style switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        private static RectTransform MakeButton(string name, RectTransform parent, string label, Vector2 size)
        {
            var rect = NewRect(name, parent);
            rect.sizeDelta = size;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.36f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = MakeText("Text", rect, label, 20, FontStyle.Bold);
            Stretch(text);
            return rect;
        }

        private static AugmentChoiceCardView CreateAugmentChoiceCard(RectTransform parent, int index)
        {
            var card = NewRect($"Choice_{index + 1}", parent);
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.19f, 0.24f, 1f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            var view = card.gameObject.AddComponent<AugmentChoiceCardView>();
            view.background = bg;
            view.cardButton = button;

            var icon = NewRect("Icon", card);
            icon.sizeDelta = new Vector2(72f, 72f);
            Anchor(icon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -96f), new Vector2(90f, -24f));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var title = MakeText("Title", card, "증강 이름", 26, FontStyle.Bold);
            Anchor(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -88f), new Vector2(-16f, -24f));
            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.alignment = TextAlignmentOptions.Left;
            view.titleText = titleText;

            var rarity = MakeText("Rarity", card, "Common", 18, FontStyle.Bold);
            Anchor(rarity, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -126f), new Vector2(-16f, -96f));
            var rarityText = rarity.GetComponent<TextMeshProUGUI>();
            rarityText.alignment = TextAlignmentOptions.Left;
            rarityText.color = new Color(0.95f, 0.82f, 0.32f);
            view.rarityText = rarityText;

            var desc = MakeText("Description", card, "설명", 18, FontStyle.Normal);
            Anchor(desc, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 78f), new Vector2(-18f, -144f));
            var descText = desc.GetComponent<TextMeshProUGUI>();
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.enableWordWrapping = true;
            view.descriptionText = descText;

            var confirm = MakeButton("PickButton", card, "선택", new Vector2(0f, 52f));
            Anchor(confirm, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 16f), new Vector2(-18f, 68f));
            view.confirmButton = confirm.GetComponent<Button>();
            return view;
        }

        private static TraitEntryView CreateTraitEntryTemplate(RectTransform parent)
        {
            var root = NewRect("TraitEntryTemplate", parent);
            root.sizeDelta = new Vector2(0f, 142f);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 142f;
            layout.flexibleWidth = 1f;
            root.gameObject.AddComponent<Image>().color = new Color(0.19f, 0.2f, 0.24f, 1f);

            var view = root.gameObject.AddComponent<TraitEntryView>();

            var icon = NewRect("Icon", root);
            Anchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -40f), new Vector2(98f, 40f));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var name = MakeText("Name", root, "특성 이름", 24, FontStyle.Bold);
            Anchor(name, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(114f, -44f), new Vector2(-200f, -10f));
            var nameText = name.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Left;
            view.nameText = nameText;

            var rank = MakeText("Rank", root, "Lv 0 / 5", 18, FontStyle.Bold);
            Anchor(rank, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -42f), new Vector2(-24f, -14f));
            var rankText = rank.GetComponent<TextMeshProUGUI>();
            rankText.alignment = TextAlignmentOptions.Right;
            view.rankText = rankText;

            var desc = MakeText("Description", root, "설명", 18, FontStyle.Normal);
            Anchor(desc, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(114f, 18f), new Vector2(-200f, -52f));
            var descText = desc.GetComponent<TextMeshProUGUI>();
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.enableWordWrapping = true;
            view.descText = descText;

            var invest = MakeButton("InvestButton", root, "투자", new Vector2(150f, 48f));
            Anchor(invest, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-174f, -24f), new Vector2(-24f, 24f));
            view.investButton = invest.GetComponent<Button>();
            view.buttonLabel = invest.GetComponentInChildren<TextMeshProUGUI>(true);

            return view;
        }

        private static void EnsureDir()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        private static void SaveAsPrefab(GameObject root, string name)
        {
            string path = $"{Dir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[GameFramework] {path} 생성 완료");
        }
    }
}
