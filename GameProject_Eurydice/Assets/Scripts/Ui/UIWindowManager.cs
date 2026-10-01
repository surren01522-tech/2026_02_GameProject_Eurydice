using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIWindowManager : MonoBehaviour
{
    public static UIWindowManager Instance { get; private set; }

    public enum WindowTab
    {
        None,
        Map,
        Inventory,
        Setting
    }

    [Header("메뉴 버튼")]
    [SerializeField] private GameObject buttonContainer;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button settingButton;

    [Header("컨텐츠 패널")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private SettingWindow settingWindow;

    [Header("HUD 설정")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("기본 활성화 탭")]
    [SerializeField] private WindowTab defaultTab = WindowTab.Map;

    private WindowTab currentTab = WindowTab.None;
    private Coroutine fadeCoroutine;
    private InputSystem_Actions inputActions;
    private bool openedViaEscape;

    public WindowTab CurrentTab => currentTab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (hudCanvasGroup == null)
            hudCanvasGroup = GetComponent<CanvasGroup>();

        if (settingWindow == null)
            settingWindow = FindFirstObjectByType<SettingWindow>(FindObjectsInactive.Include);

        if (buttonContainer == null && mapButton != null && mapButton.transform.parent != null)
            buttonContainer = mapButton.transform.parent.gameObject;

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Previous.performed += OnEscapePerformed;
        GameStateManager.OnInputModeChanged += HandleInputModeChanged;
    }

    private void OnDisable()
    {
        inputActions.UI.Previous.performed -= OnEscapePerformed;
        inputActions.UI.Disable();
        GameStateManager.OnInputModeChanged -= HandleInputModeChanged;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        inputActions?.Dispose();

        if (mapButton != null) mapButton.onClick.RemoveAllListeners();
        if (inventoryButton != null) inventoryButton.onClick.RemoveAllListeners();
        if (settingButton != null) settingButton.onClick.RemoveAllListeners();
    }

    private void OnEscapePerformed(InputAction.CallbackContext context)
    {
        HandleEscape();
    }

    private void Start()
    {
        if (mapButton != null)
            mapButton.onClick.AddListener(() => ToggleTab(WindowTab.Map));

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(() => ToggleTab(WindowTab.Inventory));

        if (settingButton != null)
            settingButton.onClick.AddListener(() => ToggleTab(WindowTab.Setting));

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }

        if (settingWindow != null)
        {
            settingWindow.Close();
        }

        OpenTab(defaultTab);
    }

    private void Update()
    {
        if (!GameStateManager.IsAltHeld && currentTab != WindowTab.None && !openedViaEscape)
        {
            CloseAllTabs();
        }
    }

    public void ToggleTab(WindowTab tab)
    {
        if (currentTab == tab)
        {
            CloseAllTabs(animatePanel: true);
        }
        else
        {
            OpenTab(tab);
        }
    }

    /// <summary>
    /// 대상 탭만 켜고 나머지 모든 패널은 끔
    /// </summary>
    public void OpenTab(WindowTab tab)
    {
        CloseAllPanelsImmediate();
        currentTab = tab;

        Vector3? buttonPos = GetButtonPosition(tab);

        switch (tab)
        {
            case WindowTab.Map:
                ShowPanel(mapPanel, buttonPos);
                break;

            case WindowTab.Inventory:
                ShowPanel(inventoryPanel, buttonPos);
                break;

            case WindowTab.Setting:
                if (settingWindow != null)
                {
                    settingWindow.Open(buttonPos);
                }
                break;
        }

        GameStateManager.SetModalActive(tab != WindowTab.None);
    }

    public void CloseAllTabs(bool animatePanel = false)
    {
        Vector3? buttonPos = GetButtonPosition(currentTab);
        currentTab = WindowTab.None;
        openedViaEscape = false;
        GameStateManager.SetModalActive(false);

        if (animatePanel)
        {
            CloseAllPanelsAnimated(buttonPos);
        }
        else if (GameStateManager.IsAltHeld)
        {
            CloseAllPanelsImmediate();
        }
    }

    private void CloseAllPanelsAnimated(Vector3? targetPos)
    {
        HidePanelAnimated(mapPanel, targetPos);
        HidePanelAnimated(inventoryPanel, targetPos);
        if (settingWindow != null) settingWindow.Close(immediate: false, targetPos);
    }

    private void CloseAllPanelsImmediate()
    {
        HidePanelImmediate(mapPanel);
        HidePanelImmediate(inventoryPanel);
        if (settingWindow != null) settingWindow.Close(immediate: true);
    }

    private void ShowPanel(GameObject panel, Vector3? originPos)
    {
        if (panel == null) return;

        if (panel.TryGetComponent<IUIPanelTransition>(out var transition))
        {
            transition.PlayOpen(originPos);
        }
        else
        {
            panel.SetActive(true);
        }
    }

    private void HidePanelAnimated(GameObject panel, Vector3? targetPos)
    {
        if (panel == null) return;

        if (panel.TryGetComponent<IUIPanelTransition>(out var transition))
        {
            transition.PlayClose(targetPos);
        }
        else
        {
            panel.SetActive(false);
        }
    }

    private void HidePanelImmediate(GameObject panel)
    {
        if (panel == null) return;

        if (panel.TryGetComponent<IUIPanelTransition>(out var transition))
        {
            transition.StopImmediate();
        }
        panel.SetActive(false);
    }

    private Vector3? GetButtonPosition(WindowTab tab)
    {
        return tab switch
        {
            WindowTab.Map => mapButton != null ? mapButton.transform.position : null,
            WindowTab.Inventory => inventoryButton != null ? inventoryButton.transform.position : null,
            WindowTab.Setting => settingButton != null ? settingButton.transform.position : null,
            _ => null
        };
    }

    /// <summary>
    /// ESC 키 입력 처리: 서브패널 닫기 -> 세팅창 닫기 -> 활성 패널 닫기 -> 설정창 열기 순차 진행
    /// </summary>
    public void HandleEscape()
    {
        if (settingWindow != null && settingWindow.IsOpen)
        {
            if (settingWindow.OnBackPressed()) return;

            CloseAllTabs(animatePanel: GameStateManager.IsAltHeld);
            return;
        }

        if (currentTab != WindowTab.None)
        {
            CloseAllTabs(animatePanel: GameStateManager.IsAltHeld);
            return;
        }

        openedViaEscape = true;
        if (buttonContainer != null) buttonContainer.SetActive(false);
        OpenTab(WindowTab.Setting);
    }

    /// <summary>
    /// InputMode에 따른 HUD 페이드
    /// </summary>
    private void HandleInputModeChanged(InputMode mode)
    {
        if (hudCanvasGroup == null) return;

        bool isAltMode = (mode == InputMode.HUDOverlay);
        bool shouldShow = isAltMode || (mode == InputMode.UIModal);

        if (buttonContainer != null && shouldShow)
        {
            bool showButtons = isAltMode || !openedViaEscape;
            buttonContainer.SetActive(showButtons);
        }

        hudCanvasGroup.interactable = shouldShow;
        hudCanvasGroup.blocksRaycasts = shouldShow;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(CoFade(shouldShow ? 1f : 0f, () =>
        {
            if (!shouldShow)
            {
                if (buttonContainer != null)
                {
                    buttonContainer.SetActive(false);
                }
                CloseAllPanelsImmediate();
            }
        }));
    }

    /// <summary>
    /// HUD 페이드 코루틴 (부드러운 감속 보간 및 렌더링 프레임 보장)
    /// </summary>
    private IEnumerator CoFade(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = hudCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = fadeDuration > 0f ? fadeDuration : 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            yield return null;
        }

        hudCanvasGroup.alpha = targetAlpha;
        yield return null;

        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}
