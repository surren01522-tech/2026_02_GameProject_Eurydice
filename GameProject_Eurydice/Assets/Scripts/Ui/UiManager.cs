using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private UIPanel defaultSettingPanel;

    private readonly Stack<UIPanel> panelStack = new Stack<UIPanel>();
    private InputSystem_Actions inputActions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Previous.performed += OnBackPressed;
    }

    void OnDisable()
    {
        inputActions.UI.Previous.performed -= OnBackPressed;
        inputActions.UI.Disable();
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    /// <summary>
    /// 새 패널을 열고 스택에 등록
    /// </summary>
    public void PushPanel(UIPanel panel)
    {
        if (panel == null) return;

        if (panelStack.Count == 0 && GameStateManager.gameState == GameState.Playing)
        {
            GameStateManager.SetGameState(GameState.Pause);
        }

        panelStack.Push(panel);
        panel.Open();
    }

    /// <summary>
    /// 최상단 패널을 닫고 이전 패널로 복귀
    /// </summary>
    public void PopPanel()
    {
        if (panelStack.Count == 0) return;

        UIPanel topPanel = panelStack.Pop();
        topPanel.Close();

        if (panelStack.Count == 0)
        {
            GameStateManager.SetGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// 열려 있는 모든 패널을 닫고 게임 플레이로 복귀
    /// </summary>
    public void CloseAll()
    {
        while (panelStack.Count > 0)
        {
            UIPanel panel = panelStack.Pop();
            panel.Close();
        }

        GameStateManager.SetGameState(GameState.Playing);
    }

    /// <summary>
    /// 기본 설정 패널 열기 (버튼 이벤트 등에서 호출 가능)
    /// </summary>
    public void OpenSetting()
    {
        if (defaultSettingPanel != null && !panelStack.Contains(defaultSettingPanel))
        {
            PushPanel(defaultSettingPanel);
        }
    }

    /// <summary>
    /// ESC(Previous) 액션 트리거 처리
    /// </summary>
    private void OnBackPressed(InputAction.CallbackContext context)
    {
        if (panelStack.Count > 0)
        {
            UIPanel currentPanel = panelStack.Peek();
            if (!currentPanel.OnBackPressed())
            {
                PopPanel();
            }
        }
        else
        {
            if (GameStateManager.gameState == GameState.Playing)
            {
                OpenSetting();
            }
            else
            {
                GameStateManager.SetGameState(GameState.Playing);
            }
        }
    }
}
