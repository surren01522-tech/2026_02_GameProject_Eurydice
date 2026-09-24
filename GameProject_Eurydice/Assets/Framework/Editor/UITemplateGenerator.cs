using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Services;
using GameFramework.Gameplay;
using GameFramework.Data;

namespace GameFramework.EditorTools
{
    /// <summary>
    /// 메뉴 한 번으로 UI 프리팹 템플릿을 생성한다.
    /// Tools > GameFramework > UI 템플릿 생성 > ...
    /// 생성 위치: Assets/Resources/UI/  (UIManager가 찾는 경로)
    /// 전부 흰색 플레이스홀더 → Inspector에서 스프라이트만 교체하면 된다.
    /// </summary>
    public static class UITemplateGenerator
    {
        private const string Dir = "Assets/Resources/UI";

        // ===================== 인벤토리 =====================

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/인벤토리 팝업")]
        public static void CreateInventoryPopup()
        {
            EnsureDir();
            var root = NewRect("InventoryPopup");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var popup = root.gameObject.AddComponent<InventoryPopup>();

            // 패널 (확대: 860x620)
            var panel = NewRect("Panel", root);
            panel.sizeDelta = new Vector2(860, 620);
            panel.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 0.98f);

            var title = MakeText("Title", panel, "인벤토리", 32, FontStyle.Bold);
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -68), new Vector2(0, 0));
            popup.titleText = title.GetComponent<Text>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(56, 48));
            Anchor(close, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-68, -58), new Vector2(-12, -10));
            popup.closeButton = close.GetComponent<Button>();

            var sort = MakeButton("SortButton", panel, "정렬", new Vector2(120, 50));
            Anchor(sort, new Vector2(0, 0), new Vector2(0, 0), new Vector2(22, 16), new Vector2(142, 66));
            popup.sortButton = sort.GetComponent<Button>();

            // 슬롯 영역: ScrollRect — 슬롯 수가 몇 개든 스크롤로 대응
            var scroll = NewRect("Scroll", panel);
            Anchor(scroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(22, 80), new Vector2(-22, -82));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            // 6열 고정 + 패널 폭에 맞춰 셀 크기 자동 계산 (해상도 대응)
            content.gameObject.AddComponent<GridLayoutGroup>();
            var auto = content.gameObject.AddComponent<AutoGridCellSize>();
            auto.columns = 6;
            auto.spacing = 12f;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            popup.slotParent = content;

            // 슬롯 템플릿 (비활성)
            var slot = NewRect("SlotTemplate", content);
            var slotBg = slot.gameObject.AddComponent<Image>();
            slotBg.color = new Color(0.24f, 0.24f, 0.28f);
            var view = slot.gameObject.AddComponent<InventorySlotView>();
            view.background = slotBg;

            var icon = NewRect("Icon", slot);
            Anchor(icon, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            view.icon = icon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var cnt = MakeText("Count", slot, "99", 22, FontStyle.Bold);
            Anchor(cnt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), new Vector2(-8, 28));
            var cntText = cnt.GetComponent<Text>();
            cntText.alignment = TextAnchor.LowerRight;
            view.countText = cntText;

            slot.gameObject.SetActive(false);
            popup.slotTemplate = view;

            SaveAsPrefab(root.gameObject, "InventoryPopup");
        }

        // ===================== 설정창 =====================

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
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 0));
            popup.titleText = title.GetComponent<Text>();

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

            var lb = MakeText("Label", row, label, 22, FontStyle.Bold);
            Anchor(lb, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(90, 0));
            lb.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            // 슬라이더 (Background / Fill Area / Handle 구조를 코드로 구성)
            var sroot = NewRect("Slider", row);
            Anchor(sroot, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(100, -12), new Vector2(0, 12));
            var slider = sroot.gameObject.AddComponent<Slider>();

            var bg = NewRect("Background", sroot);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.12f);

            var fillArea = NewRect("Fill Area", sroot);
            Anchor(fillArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 4), new Vector2(-6, -4));
            var fill = NewRect("Fill", fillArea);
            Stretch(fill);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.42f, 0.68f, 1f);

            var handleArea = NewRect("Handle Slide Area", sroot);
            Anchor(handleArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, 0));
            var handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(22, 0);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }


        // ===================== 업적 리스트 =====================

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
            Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 0));
            popup.titleText = title.GetComponent<Text>();

            var close = MakeButton("CloseButton", panel, "X", new Vector2(52, 44));
            Anchor(close, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-62, -54), new Vector2(-10, -10));
            popup.closeButton = close.GetComponent<Button>();

            // 스크롤 뷰 (Viewport + Content)
            var scroll = NewRect("Scroll", panel);
            Anchor(scroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 16), new Vector2(-16, -74));
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var vlayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 10;
            vlayout.childForceExpandHeight = false;
            vlayout.childControlHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            popup.listParent = content;

            // 항목 템플릿
            var entry = NewRect("EntryTemplate", content);
            entry.sizeDelta = new Vector2(0, 96);
            entry.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            var entryBg = entry.gameObject.AddComponent<Image>();
            entryBg.color = new Color(0.22f, 0.22f, 0.26f);
            var view = entry.gameObject.AddComponent<AchievementEntryView>();
            view.background = entryBg;

            var eicon = NewRect("Icon", entry);
            Anchor(eicon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -30), new Vector2(74, 30));
            view.icon = eicon.gameObject.AddComponent<Image>();
            view.icon.raycastTarget = false;

            var ename = MakeText("Name", entry, "업적 이름", 21, FontStyle.Bold);
            Anchor(ename, new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -38), new Vector2(-90, -8));
            var enameT = ename.GetComponent<Text>();
            enameT.alignment = TextAnchor.MiddleLeft;
            view.nameText = enameT;

            var edesc = MakeText("Desc", entry, "설명", 15, FontStyle.Normal);
            Anchor(edesc, new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -62), new Vector2(-90, -38));
            var edescT = edesc.GetComponent<Text>();
            edescT.alignment = TextAnchor.MiddleLeft;
            edescT.color = new Color(0.75f, 0.75f, 0.8f);
            view.descText = edescT;

            // 진행바 (스프라이트 불필요 — anchorMax 방식)
            var barBg = NewRect("BarBg", entry);
            Anchor(barBg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(88, 12), new Vector2(-90, 26));
            view.barBg = barBg.gameObject.AddComponent<Image>();
            view.barBg.color = new Color(0.1f, 0.1f, 0.12f);
            view.barBg.raycastTarget = false;

            var barFill = NewRect("BarFill", barBg);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(0.5f, 1f);
            barFill.offsetMin = Vector2.zero; barFill.offsetMax = Vector2.zero;
            var fillImg = barFill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.55f, 0.9f, 0.37f);
            fillImg.raycastTarget = false;
            view.barFill = barFill;

            var eprog = MakeText("Progress", entry, "0 / 10", 14, FontStyle.Normal);
            Anchor(eprog, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-86, 8), new Vector2(-12, 30));
            var eprogT = eprog.GetComponent<Text>();
            eprogT.alignment = TextAnchor.MiddleRight;
            view.progressText = eprogT;

            var emark = MakeText("UnlockedMark", entry, "★ 달성", 17, FontStyle.Bold);
            Anchor(emark, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-86, 6), new Vector2(-12, 34));
            var emarkT = emark.GetComponent<Text>();
            emarkT.alignment = TextAnchor.MiddleRight;
            emarkT.color = new Color(1f, 0.83f, 0.35f);
            view.unlockedMark = emarkT;

            entry.gameObject.SetActive(false);
            popup.entryTemplate = view;

            SaveAsPrefab(root.gameObject, "AchievementListPopup");
        }

        // ===================== 업적 토스트 =====================

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/업적 달성 토스트")]
        public static void CreateAchievementToast()
        {
            EnsureDir();
            var root = NewRect("AchievementToast");
            // 상단 중앙 고정, 화면 밖(+120)에서 시작
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

            var ticon = NewRect("Icon", root);
            Anchor(ticon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, -26), new Vector2(66, 26));
            toast.icon = ticon.gameObject.AddComponent<Image>();
            toast.icon.raycastTarget = false;

            var header = MakeText("Header", root, "업적 달성!", 15, FontStyle.Bold);
            Anchor(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(80, -36), new Vector2(-14, -8));
            var headerT = header.GetComponent<Text>();
            headerT.alignment = TextAnchor.MiddleLeft;
            headerT.color = new Color(1f, 0.83f, 0.35f);
            toast.headerText = headerT;

            var tname = MakeText("Name", root, "업적 이름", 21, FontStyle.Bold);
            Anchor(tname, new Vector2(0, 0), new Vector2(1, 0), new Vector2(80, 8), new Vector2(-14, 44));
            var tnameT = tname.GetComponent<Text>();
            tnameT.alignment = TextAnchor.MiddleLeft;
            toast.nameText = tnameT;

            SaveAsPrefab(root.gameObject, "AchievementToast");
        }

        // ===================== 일괄 생성 =====================

        [MenuItem("Tools/GameFramework/UI 템플릿 생성/전체 UI 한 번에 생성")]
        public static void CreateAllUI()
        {
            CreateInventoryPopup();
            CreateSettingsPopup();
            CreateAchievementListPopup();
            CreateAchievementToast();
            Debug.Log("[GameFramework] UI 템플릿 4종 생성 완료 (Resources/UI/)");
        }

        [MenuItem("Tools/GameFramework/게임잼 원클릭 세팅")]
        public static void GameJamSetup()
        {
            // 1) 라이브러리 에셋 4종
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            CreateAssetIfMissing<SoundLibrary>("Assets/Resources/SoundLibrary.asset");
            CreateAssetIfMissing<EffectLibrary>("Assets/Resources/EffectLibrary.asset");
            CreateAssetIfMissing<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
            CreateAssetIfMissing<AchievementDatabase>("Assets/Resources/AchievementDatabase.asset");

            // 2) UI 템플릿 전부
            CreateAllUI();

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("게임잼 세팅 완료",
                "라이브러리 에셋 4종 + UI 템플릿 4종이 준비됐습니다.\n" +
                "이제 Framework Hub(Ctrl+Shift+G)에서 사운드/이펙트/업적을 등록하세요.", "OK");
        }

        private static void CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        // ===================== 공통 헬퍼 =====================

        private static RectTransform NewRect(string name, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        private static RectTransform MakeText(string name, RectTransform parent, string content, int size, FontStyle style)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return rt;
        }

        private static RectTransform MakeButton(string name, RectTransform parent, string label, Vector2 size)
        {
            var rt = NewRect(name, parent);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.36f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var txt = MakeText("Text", rt, label, 20, FontStyle.Bold);
            Stretch(txt);
            return rt;
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
            Debug.Log($"[GameFramework] {path} 생성 완료 — UIManager.Show<{name.Replace("Popup","")}Popup>(\"{name}\") 로 열 수 있습니다.");
        }
    }
}
