using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PuzzleUI : MonoBehaviour
{
    public static PuzzleUI Instance { get; private set; }

    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject exitPanel;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI exitButtonText;

    private Action onExitCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (exitPanel == null) exitPanel = gameObject;
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(HandleExitClicked);
            UIButtonTween.Bind(exitButton);
        }

        exitPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(HandleExitClicked);

        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 퍼즐 종료 UI를 화면에 표시합니다.
    /// </summary>
    public void Show(Action onExit)
    {
        onExitCallback = onExit;
        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
            exitPanel.transform.PlayPopIn(0.2f, 0.85f);
        }
    }

    /// <summary>
    /// 퍼즐 종료 UI를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        onExitCallback = null;
        if (exitPanel != null)
        {
            exitPanel.transform.DOKill();
            exitPanel.SetActive(false);
        }
    }

    private void HandleExitClicked()
    {
        var callback = onExitCallback;
        Hide();
        callback?.Invoke();
    }
}
