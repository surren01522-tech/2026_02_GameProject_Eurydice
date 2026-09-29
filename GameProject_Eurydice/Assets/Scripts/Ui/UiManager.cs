using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    private readonly Stack<UIPanel> panelStack = new();
    private InputSystem_Actions inputActions;

    public bool HasActivePanel => panelStack.Count > 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Previous.performed += OnBackPressed;
    }

    private void OnDisable()
    {
        inputActions.UI.Previous.performed -= OnBackPressed;
        inputActions.UI.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    /// <summary>
    /// 새 패널을 열고 스택에 등록합니다.
    /// </summary>
    public void PushPanel(UIPanel panel)
    {
        if (panel == null) return;

        panelStack.Push(panel);
        panel.Open();
        GameStateManager.SetModalActive(true);
    }

    /// <summary>
    /// 최상단 패널을 닫고 이전 패널로 복귀합니다.
    /// </summary>
    public void PopPanel()
    {
        if (panelStack.Count == 0) return;

        UIPanel topPanel = panelStack.Pop();
        topPanel.Close();
        GameStateManager.SetModalActive(panelStack.Count > 0);
    }

    /// <summary>
    /// 특정 패널을 열거나 이미 열려있으면 닫습니다.
    /// </summary>
    public void TogglePanel(UIPanel panel)
    {
        if (panel == null) return;

        if (panelStack.Count > 0 && panelStack.Peek() == panel)
        {
            PopPanel();
        }
        else
        {
            PushPanel(panel);
        }
    }

    /// <summary>
    /// 열려 있는 모든 패널을 닫습니다.
    /// </summary>
    public void CloseAll()
    {
        while (panelStack.Count > 0)
        {
            UIPanel panel = panelStack.Pop();
            panel.Close();
        }

        GameStateManager.SetModalActive(false);
    }

    /// <summary>
    /// ESC(Previous) 액션 트리거 처리. 스택 패널을 닫거나 설정창을 바로 엽니다.
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
        else if (SettingWindow.Instance != null)
        {
            if (SettingWindow.Instance.IsOpen)
            {
                SettingWindow.Instance.OnBackPressed();
            }
            else
            {
                SettingWindow.Instance.Open();
            }
        }
    }
}
