using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIWindowManager : MonoBehaviour
{
    public static UIWindowManager Instance { get; private set; }

    public enum WindowTab { None, Map, Inventory, Setting }

    [Header("메뉴 버튼")]
    [SerializeField] private GameObject buttonContainer;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button settingButton;

    [Header("컨텐츠 패널")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private SettingUIManager settingUI;

    [Header("HUD 설정")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private WindowTab defaultTab = WindowTab.None;

    public WindowTab CurrentTab => currentTab;

    private WindowTab currentTab = WindowTab.None;
    private Coroutine fadeCoroutine;
    private InputSystem_Actions inputActions;
    private bool openedViaEscape;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (hudCanvasGroup == null) hudCanvasGroup = GetComponent<CanvasGroup>();
        if (settingUI == null) settingUI = FindFirstObjectByType<SettingUIManager>(FindObjectsInactive.Include);
        if (buttonContainer == null && mapButton != null) buttonContainer = mapButton.transform.parent.gameObject;

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

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void Start()
    {
        BindButton(mapButton, WindowTab.Map);
        BindButton(inventoryButton, WindowTab.Inventory);
        BindButton(settingButton, WindowTab.Setting);

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }

        CloseAllPanels(animate: false);

        if (defaultTab != WindowTab.None)
        {
            OpenTab(defaultTab);
        }
    }

    private void BindButton(Button btn, WindowTab tab)
    {
        if (btn != null) btn.onClick.AddListener(() => ToggleTab(tab));
    }

    private void OnEscapePerformed(InputAction.CallbackContext _) => HandleEscape();

    public void ToggleTab(WindowTab tab)
    {
        if (currentTab == tab) CloseAllTabs(animatePanel: true);
        else OpenTab(tab);
    }

    public void OpenTab(WindowTab tab)
    {
        CloseAllPanels(animate: false);
        currentTab = tab;

        Vector3? originPos = GetButtonPos(tab);

        if (tab == WindowTab.Setting && settingUI != null)
        {
            settingUI.Open(originPos);
        }
        else
        {
            SetPanelState(GetPanel(tab), true, animate: true, originPos);
        }

        GameStateManager.SetModalActive(tab != WindowTab.None);
    }

    public void CloseAllTabs(bool animatePanel = false)
    {
        Vector3? targetPos = GetButtonPos(currentTab);
        currentTab = WindowTab.None;
        openedViaEscape = false;
        GameStateManager.SetModalActive(false);

        CloseAllPanels(animatePanel, targetPos);
    }

    public void HandleEscape()
    {
        if (settingUI != null && settingUI.IsOpen && settingUI.OnBackPressed()) return;

        if (currentTab != WindowTab.None)
        {
            CloseAllTabs(animatePanel: GameStateManager.IsAltHeld);
            return;
        }

        openedViaEscape = true;
        if (buttonContainer != null) buttonContainer.SetActive(false);
        OpenTab(WindowTab.Setting);
    }

    private void CloseAllPanels(bool animate, Vector3? targetPos = null)
    {
        SetPanelState(mapPanel, false, animate, targetPos);
        SetPanelState(inventoryPanel, false, animate, targetPos);

        if (settingUI != null)
        {
            settingUI.Close(immediate: !animate, targetPos);
        }
    }

    private void SetPanelState(GameObject panel, bool active, bool animate, Vector3? pos = null)
    {
        if (panel == null) return;

        IUIPanelTransition transition = panel.GetComponent<IUIPanelTransition>();
        if (transition != null)
        {
            if (active)
            {
                if (animate) transition.PlayOpen(pos);
                else panel.SetActive(true);
            }
            else
            {
                if (animate) transition.PlayClose(pos);
                else { transition.StopImmediate(); panel.SetActive(false); }
            }
        }
        else
        {
            panel.SetActive(active);
        }
    }

    private GameObject GetPanel(WindowTab tab)
    {
        switch (tab)
        {
            case WindowTab.Map: return mapPanel;
            case WindowTab.Inventory: return inventoryPanel;
            case WindowTab.Setting: return settingUI != null ? settingUI.gameObject : null;
            default: return null;
        }
    }

    private Vector3? GetButtonPos(WindowTab tab)
    {
        Button btn = null;
        switch (tab)
        {
            case WindowTab.Map: btn = mapButton; break;
            case WindowTab.Inventory: btn = inventoryButton; break;
            case WindowTab.Setting: btn = settingButton; break;
        }
        return btn != null ? btn.transform.position : (Vector3?)null;
    }

    private void HandleInputModeChanged(InputMode mode)
    {
        if (hudCanvasGroup == null) return;

        bool isAltMode = (mode == InputMode.HUDOverlay);
        bool shouldShow = isAltMode || (mode == InputMode.UIModal && !GameStateManager.IsPuzzleActive);

        if (buttonContainer != null && shouldShow)
        {
            buttonContainer.SetActive(isAltMode || !openedViaEscape);
        }

        hudCanvasGroup.interactable = shouldShow;
        hudCanvasGroup.blocksRaycasts = shouldShow;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(CoFade(shouldShow ? 1f : 0f, () =>
        {
            if (!shouldShow)
            {
                if (buttonContainer != null) buttonContainer.SetActive(false);
                CloseAllPanels(animate: false);
            }
        }));
    }

    private IEnumerator CoFade(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = hudCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = fadeDuration > 0f ? fadeDuration : 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        hudCanvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}
