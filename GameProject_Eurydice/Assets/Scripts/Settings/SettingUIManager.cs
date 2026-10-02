using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingUIManager : MonoBehaviour
{
    public static SettingUIManager Instance { get; private set; }

    [Serializable]
    public struct MenuEntry
    {
        public string title;
        public Button button;
        public GameObject panel;
    }

    [Header("메인 요소")]
    [SerializeField] private GameObject settingPanel;

    [Header("인디케이터")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private float moveSpeed = 20f;

    [Header("메뉴 항목 (Audio, Graphic, Control)")]
    [SerializeField] private List<MenuEntry> menuList = new();

    [Header("오디오 설정 UI")]
    [SerializeField] private SettingAudioSliderControl masterControl;
    [SerializeField] private SettingAudioSliderControl bgmControl;
    [SerializeField] private SettingAudioSliderControl sfxControl;
    [SerializeField] private SettingAudioSliderControl uiControl;

    [Header("그래픽 설정 UI")]
    [SerializeField] private SettingSelectorControl resSelector;
    [SerializeField] private SettingSelectorControl screenModeSelector;
    [SerializeField] private SettingSelectorControl qualitySelector;
    [SerializeField] private SettingSelectorControl vsyncSelector;
    [SerializeField] private SettingSelectorControl fpsSelector;

    [Header("컨트롤 설정 UI")]
    [SerializeField] private SettingSliderControl sensControl;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Button resetBindingsButton;

    [Header("키 바인딩 슬롯")]
    [SerializeField] private SettingKeyRebindSlot moveForwardSlot;
    [SerializeField] private SettingKeyRebindSlot moveBackwardSlot;
    [SerializeField] private SettingKeyRebindSlot moveLeftSlot;
    [SerializeField] private SettingKeyRebindSlot moveRightSlot;
    [SerializeField] private SettingKeyRebindSlot jumpSlot;
    [SerializeField] private SettingKeyRebindSlot sprintSlot;
    [SerializeField] private SettingKeyRebindSlot interactSlot;
    [SerializeField] private SettingKeyRebindSlot toggleViewSlot;

    [Header("디버그 설정 UI")]
    [SerializeField] private Button debugTabButton;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button deleteSaveDataButton;
    [SerializeField] private Button deleteSettingsButton;
    [SerializeField] private Button deleteAllDataButton;
    [SerializeField] private SettingSelectorControl sceneSelector;
    [SerializeField] private TMP_Dropdown sceneDropdown;
    [SerializeField] private Button loadSceneButton;
    [SerializeField] private Button quitGameButton;

    private int selectedSceneIndex = 0;

    private readonly List<(int w, int h)> supportedResolutions = new();

    private static readonly (string name, FullScreenMode mode)[] ScreenModes = {
        ("Borderless Window", FullScreenMode.FullScreenWindow),
        ("Fullscreen", FullScreenMode.ExclusiveFullScreen),
        ("Windowed", FullScreenMode.Windowed)
    };

    private static readonly int[] FpsLimits = { 30, 60, 120, 144, 240, -1 };

    private readonly Vector3[] corners = new Vector3[4];
    private int activeEntryIndex = -1;
    private GameObject lastSelected;
    private GameObject lastMenuButton;
    private Coroutine arrowCoroutine;
    private RectTransform[] cachedLayoutTransforms;
    private InputSystem_Actions inputActions;

    public bool IsOpen => settingPanel != null && settingPanel.activeSelf;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (settingPanel == null)
        {
            var panelTransform = transform.Find("Setting Panel");
            if (panelTransform != null) settingPanel = panelTransform.gameObject;
        }

        if (settingPanel != null)
        {
            var layoutGroups = settingPanel.GetComponentsInChildren<LayoutGroup>(true);
            cachedLayoutTransforms = new RectTransform[layoutGroups.Length];
            for (int i = 0; i < layoutGroups.Length; i++)
            {
                cachedLayoutTransforms[i] = layoutGroups[i].GetComponent<RectTransform>();
            }
        }

        for (int i = 0; i < menuList.Count; i++)
        {
            var button = menuList[i].button;
            if (button == null) continue;
            BindButtonEvents(button, i);
        }

        if (settingPanel != null)
            settingPanel.SetActive(false);
    }

    private void Start()
    {
        Warmup();
        InitAudioUI();
        InitGraphicUI();
        InitControlUI();
        InitDebugUI();
    }

    private void Warmup()
    {
        if (settingPanel == null) return;

        var cg = settingPanel.GetComponent<CanvasGroup>();
        bool addedCg = false;
        if (cg == null)
        {
            cg = settingPanel.AddComponent<CanvasGroup>();
            addedCg = true;
        }

        float prevAlpha = cg.alpha;
        bool prevInteractable = cg.interactable;
        bool prevBlocksRaycasts = cg.blocksRaycasts;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        settingPanel.SetActive(true);
        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].panel != null)
                menuList[i].panel.SetActive(true);
        }

        Canvas.ForceUpdateCanvases();
        RebuildLayouts();

        HideAllSubPanels();
        settingPanel.SetActive(false);

        cg.alpha = prevAlpha;
        cg.interactable = prevInteractable;
        cg.blocksRaycasts = prevBlocksRaycasts;

        if (addedCg && !settingPanel.TryGetComponent<IUIPanelTransition>(out _))
            Destroy(cg);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnBindingsChanged -= RefreshAllKeySlots;
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (!IsOpen || EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(lastSelected);
    }

    private void OnDisable()
    {
        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }
    }

    public void Open(Vector3? originPos = null)
    {
        if (settingPanel != null)
        {
            if (settingPanel.TryGetComponent<IUIPanelTransition>(out var transition))
                transition.PlayOpen(originPos);
            else
                settingPanel.SetActive(true);
        }

        HideAllSubPanels();

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }

        if (menuList.Count > 0 && menuList[0].button != null)
        {
            var firstButton = menuList[0].button.gameObject;
            lastSelected = firstButton;
            lastMenuButton = firstButton;
            EventSystem.current?.SetSelectedGameObject(firstButton);
            StartCoroutine(CoInitArrowPosition(firstButton));
        }
    }

    public void Close(bool immediate = false, Vector3? targetPos = null)
    {
        HideAllSubPanels();

        if (settingPanel != null)
        {
            if (!immediate && settingPanel.TryGetComponent<IUIPanelTransition>(out var transition))
                transition.PlayClose(targetPos);
            else
            {
                if (settingPanel.TryGetComponent<IUIPanelTransition>(out var tr))
                    tr.StopImmediate();
                settingPanel.SetActive(false);
            }
        }
    }

    public bool OnBackPressed()
    {
        if (activeEntryIndex >= 0)
        {
            AudioManager.Instance?.PlayCancel();
            CloseAllSubPanels();
            return true;
        }
        return false;
    }

    public void OpenSubPanel(int index)
    {
        activeEntryIndex = index;
        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].panel != null)
                menuList[i].panel.SetActive(i == index);
        }

        var targetPanel = menuList[index].panel;
        if (targetPanel != null)
        {
            var firstSelectable = targetPanel.GetComponentInChildren<Selectable>();
            if (firstSelectable != null)
            {
                EventSystem.current?.SetSelectedGameObject(firstSelectable.gameObject);
                lastSelected = firstSelectable.gameObject;
            }
        }
    }

    public void CloseAllSubPanels()
    {
        HideAllSubPanels();

        if (lastMenuButton != null && lastMenuButton.activeInHierarchy)
        {
            EventSystem.current?.SetSelectedGameObject(lastMenuButton);
            lastSelected = lastMenuButton;
            MoveArrowTo(lastMenuButton);
        }
    }

    private void HideAllSubPanels()
    {
        activeEntryIndex = -1;
        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].panel != null)
                menuList[i].panel.SetActive(false);
        }
    }

    private IEnumerator CoInitArrowPosition(GameObject target)
    {
        yield return null;
        if (arrow != null && target != null)
            arrow.localPosition = GetArrowPosition(target);
    }

    private void RebuildLayouts()
    {
        if (cachedLayoutTransforms == null) return;

        for (int i = 0; i < cachedLayoutTransforms.Length; i++)
        {
            if (cachedLayoutTransforms[i] != null && cachedLayoutTransforms[i].gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cachedLayoutTransforms[i]);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void BindButtonEvents(Button button, int index)
    {
        var trigger = button.gameObject.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => EventSystem.current?.SetSelectedGameObject(button.gameObject));
        trigger.triggers.Add(enterEntry);

        var selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback.AddListener(_ => SelectMenu(index, true));
        trigger.triggers.Add(selectEntry);

        button.onClick.AddListener(() => OnEntryClicked(index));
    }

    private void SelectMenu(int index, bool playSound)
    {
        if (index < 0 || index >= menuList.Count) return;
        var buttonGo = menuList[index].button.gameObject;

        if (lastSelected != buttonGo)
        {
            if (playSound) AudioManager.Instance?.PlayNavigate();
            lastSelected = buttonGo;
            lastMenuButton = buttonGo;
            MoveArrowTo(buttonGo);
        }
    }

    private void OnEntryClicked(int index)
    {
        if (index < 0 || index >= menuList.Count) return;

        var entry = menuList[index];
        if (entry.panel != null)
        {
            if (activeEntryIndex == index)
            {
                AudioManager.Instance?.PlayCancel();
                CloseAllSubPanels();
                return;
            }

            AudioManager.Instance?.PlaySubmit();
            lastMenuButton = entry.button.gameObject;
            MoveArrowTo(lastMenuButton);
            OpenSubPanel(index);
        }
        else
        {
            AudioManager.Instance?.PlaySubmit();
            Close();
        }
    }

    private void MoveArrowTo(GameObject target)
    {
        if (arrow == null || target == null) return;
        if (arrowCoroutine != null) StopCoroutine(arrowCoroutine);
        arrowCoroutine = StartCoroutine(MoveArrow(GetArrowPosition(target)));
    }

    private IEnumerator MoveArrow(Vector3 targetLocalPos)
    {
        while (arrow != null && Vector3.Distance(arrow.localPosition, targetLocalPos) > 0.1f)
        {
            arrow.localPosition = Vector3.Lerp(arrow.localPosition, targetLocalPos, Time.unscaledDeltaTime * moveSpeed);
            yield return null;
        }

        if (arrow != null) arrow.localPosition = targetLocalPos;
        arrowCoroutine = null;
    }

    private Vector3 GetArrowPosition(GameObject target)
    {
        if (target == null || arrow == null) return Vector3.zero;

        var targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null) return arrow.localPosition;

        targetRect.GetWorldCorners(corners);
        Vector3 leftCenterWorld = (corners[0] + corners[1]) * 0.5f;

        float arrowWidth = arrow.rect.width * arrow.lossyScale.x;
        float arrowHeight = arrow.rect.height * arrow.lossyScale.y;

        float pivotToRightEdge = (1f - arrow.pivot.x) * arrowWidth;
        float pivotToCenterY = (arrow.pivot.y - 0.5f) * arrowHeight;

        Vector3 finalWorldPos = leftCenterWorld
            - targetRect.right * (spacing + pivotToRightEdge)
            + targetRect.up * pivotToCenterY;

        return arrow.parent.InverseTransformPoint(finalWorldPos);
    }

    /// <summary>
    /// 오디오 슬라이더 UI를 초기화하고 이벤트를 연결합니다.
    /// </summary>
    private void InitAudioUI()
    {
        if (SettingManager.Instance == null) return;
        var s = SettingManager.Instance.CurrentSettings;

        if (masterControl != null)
        {
            masterControl.Init(s.masterVolume, s.masterMute);
            masterControl.onValueChanged += SettingManager.Instance.SetMasterVol;
            masterControl.onMuteChanged += SettingManager.Instance.SetMasterMute;
        }
        if (bgmControl != null)
        {
            bgmControl.Init(s.bgmVolume, s.bgmMute);
            bgmControl.onValueChanged += SettingManager.Instance.SetBGMVol;
            bgmControl.onMuteChanged += SettingManager.Instance.SetBGMMute;
        }
        if (sfxControl != null)
        {
            sfxControl.Init(s.sfxVolume, s.sfxMute);
            sfxControl.onValueChanged += SettingManager.Instance.SetSFXVol;
            sfxControl.onMuteChanged += SettingManager.Instance.SetSFXMute;
        }
        if (uiControl != null)
        {
            uiControl.Init(s.uiVolume, s.uiMute);
            uiControl.onValueChanged += SettingManager.Instance.SetUIVol;
            uiControl.onMuteChanged += SettingManager.Instance.SetUIMute;
        }
    }

    /// <summary>
    /// 그래픽 셀렉터 UI를 초기화하고 이벤트를 연결합니다.
    /// </summary>
    private void InitGraphicUI()
    {
        if (SettingManager.Instance == null) return;
        var s = SettingManager.Instance.CurrentSettings;

        if (resSelector != null)
        {
            supportedResolutions.Clear();
            var resOptions = new List<string>();
            var addedSet = new HashSet<(int, int)>();

            foreach (var r in Screen.resolutions)
            {
                if (r.width < 1024 || r.height < 600) continue;
                if (addedSet.Add((r.width, r.height)))
                    supportedResolutions.Add((r.width, r.height));
            }

            if (supportedResolutions.Count == 0)
            {
                supportedResolutions.Add((1920, 1080));
                supportedResolutions.Add((1280, 720));
            }

            int currentResIdx = 0;
            for (int i = 0; i < supportedResolutions.Count; i++)
            {
                var (w, h) = supportedResolutions[i];
                resOptions.Add($"{w} x {h}");
                if (w == s.resolutionWidth && h == s.resolutionHeight)
                    currentResIdx = i;
            }

            resSelector.Init(resOptions, currentResIdx);
            resSelector.onIndexChanged += idx =>
            {
                var (w, h) = supportedResolutions[idx];
                SettingManager.Instance.SetRes(w, h);
            };
        }

        if (screenModeSelector != null)
        {
            var modeOptions = new List<string>();
            int currentModeIdx = 0;
            for (int i = 0; i < ScreenModes.Length; i++)
            {
                modeOptions.Add(ScreenModes[i].name);
                if (ScreenModes[i].mode == s.fullScreenMode)
                    currentModeIdx = i;
            }
            screenModeSelector.Init(modeOptions, currentModeIdx);
            screenModeSelector.onIndexChanged += idx => SettingManager.Instance.SetFullScreen(ScreenModes[idx].mode);
        }

        if (qualitySelector != null)
        {
            var qNames = new List<string>(QualitySettings.names);
            qualitySelector.Init(qNames, Mathf.Clamp(s.qualityLevel, 0, qNames.Count - 1));
            qualitySelector.onIndexChanged += idx => SettingManager.Instance.SetQuality(idx);
        }

        if (vsyncSelector != null)
        {
            var vsyncOptions = new List<string> { "Off", "On" };
            vsyncSelector.Init(vsyncOptions, s.vSyncCount > 0 ? 1 : 0);
            vsyncSelector.onIndexChanged += idx => SettingManager.Instance.SetVSync(idx);
        }

        if (fpsSelector != null)
        {
            var fpsOptions = new List<string> { "30", "60", "120", "144", "240", "Unlimited" };
            int currentFpsIdx = 1;
            for (int i = 0; i < FpsLimits.Length; i++)
            {
                if (FpsLimits[i] == s.targetFrameRate)
                {
                    currentFpsIdx = i;
                    break;
                }
            }
            fpsSelector.Init(fpsOptions, currentFpsIdx);
            fpsSelector.onIndexChanged += idx => SettingManager.Instance.SetFPS(FpsLimits[idx]);
        }
    }

    /// <summary>
    /// 컨트롤 설정 UI(감도, Y축 반전, 키 바인딩 슬롯)를 초기화합니다.
    /// </summary>
    private void InitControlUI()
    {
        if (SettingManager.Instance == null) return;
        var s = SettingManager.Instance.CurrentSettings;

        if (sensControl != null)
        {
            sensControl.Init(s.mouseSensitivity);
            sensControl.onValueChanged += SettingManager.Instance.SetSens;
        }

        if (invertYToggle != null)
        {
            invertYToggle.SetIsOnWithoutNotify(s.invertY);
            invertYToggle.onValueChanged.AddListener(SettingManager.Instance.SetInvertY);
        }

        InitKeySlots();

        resetBindingsButton?.onClick.AddListener(ResetKeyBindings);
    }

    /// <summary>
    /// 인스펙터에 연결된 각 키 슬롯에 Input System Action을 1:1로 직접 연결합니다.
    /// </summary>
    private void InitKeySlots()
    {
        if (inputActions == null)
            inputActions = new InputSystem_Actions();

        SettingManager.Instance?.ApplyBindings(inputActions.asset);

        var player = inputActions.Player;

        moveForwardSlot?.Init(player.Move, "up", "Move Forward");
        moveBackwardSlot?.Init(player.Move, "down", "Move Backward");
        moveLeftSlot?.Init(player.Move, "left", "Move Left");
        moveRightSlot?.Init(player.Move, "right", "Move Right");
        jumpSlot?.Init(player.Jump, null, "Jump");
        sprintSlot?.Init(player.Sprint, null, "Sprint");
        interactSlot?.Init(player.Interact, null, "Interact");
        toggleViewSlot?.Init(player.ToggleView, null, "Toggle View");

        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.OnBindingsChanged -= RefreshAllKeySlots;
            SettingManager.Instance.OnBindingsChanged += RefreshAllKeySlots;
        }
    }

    private void ResetKeyBindings()
    {
        if (inputActions == null)
            inputActions = new InputSystem_Actions();

        SettingManager.Instance?.ResetBindings(inputActions.asset);
        RefreshAllKeySlots();
    }

    private void RefreshAllKeySlots()
    {
        moveForwardSlot?.RefreshDisplay();
        moveBackwardSlot?.RefreshDisplay();
        moveLeftSlot?.RefreshDisplay();
        moveRightSlot?.RefreshDisplay();
        jumpSlot?.RefreshDisplay();
        sprintSlot?.RefreshDisplay();
        interactSlot?.RefreshDisplay();
        toggleViewSlot?.RefreshDisplay();
    }

    /// <summary>
    /// 개발 빌드 또는 에디터 환경에서만 디버그 UI 요소를 활성화하고 버튼 이벤트를 연결합니다.
    /// </summary>
    private void InitDebugUI()
    {
        bool isDebug = Debug.isDebugBuild || Application.isEditor;

        if (debugTabButton != null)
            debugTabButton.gameObject.SetActive(isDebug);

        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].title != null && menuList[i].title.ToLower().Contains("debug"))
            {
                menuList[i].button?.gameObject.SetActive(isDebug);
                if (!isDebug && menuList[i].panel != null)
                    menuList[i].panel.SetActive(false);
            }
        }

        if (!isDebug && debugPanel != null)
            debugPanel.SetActive(false);

        if (deleteSaveDataButton != null)
            deleteSaveDataButton.onClick.AddListener(SaveManager.DeleteSaveData);

        if (deleteSettingsButton != null)
            deleteSettingsButton.onClick.AddListener(SaveManager.DeleteSettings);

        if (deleteAllDataButton != null)
            deleteAllDataButton.onClick.AddListener(SaveManager.DeleteAllData);

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        var sceneOptions = new List<string>();
        int currentSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            sceneOptions.Add($"[{i}] {name}");
        }

        if (sceneOptions.Count == 0)
            sceneOptions.Add($"[0] {SceneManager.GetActiveScene().name}");

        selectedSceneIndex = Mathf.Clamp(currentSceneBuildIndex >= 0 ? currentSceneBuildIndex : 0, 0, sceneOptions.Count - 1);

        if (sceneSelector != null)
        {
            sceneSelector.Init(sceneOptions, selectedSceneIndex);
            sceneSelector.onIndexChanged += idx =>
            {
                selectedSceneIndex = idx;
                if (sceneDropdown != null) sceneDropdown.SetValueWithoutNotify(idx);
            };
        }

        if (sceneDropdown != null)
        {
            sceneDropdown.ClearOptions();
            sceneDropdown.AddOptions(sceneOptions);
            sceneDropdown.SetValueWithoutNotify(selectedSceneIndex);
            sceneDropdown.onValueChanged.AddListener(idx =>
            {
                selectedSceneIndex = idx;
                if (sceneSelector != null) sceneSelector.SetIndex(idx, false);
            });
        }

        if (loadSceneButton != null)
            loadSceneButton.onClick.AddListener(LoadSelectedScene);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);
    }

    private void LoadSelectedScene()
    {
        if (selectedSceneIndex >= 0 && selectedSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;
            GameStateManager.ResetState();
            SceneManager.LoadScene(selectedSceneIndex);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
