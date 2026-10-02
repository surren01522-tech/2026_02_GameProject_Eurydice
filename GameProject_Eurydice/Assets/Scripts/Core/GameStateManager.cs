using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum InputMode
{
    GamePlay,
    HUDOverlay,
    UIModal
}

public class GameStateManager : MonoBehaviour
{
    public static InputMode CurrentInputMode { get; private set; } = InputMode.GamePlay;
    public static event Action<InputMode> OnInputModeChanged;

    public static bool IsAltHeld { get; private set; }
    public static bool HasActiveModal { get; private set; }

    public static bool IsGamePlaying => CurrentInputMode == InputMode.GamePlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        ResetState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetState();
    }

    public static void ResetState()
    {
        IsAltHeld = false;
        HasActiveModal = false;
        CurrentInputMode = InputMode.GamePlay;
        ApplyCursorState(InputMode.GamePlay);
        OnInputModeChanged?.Invoke(CurrentInputMode);
    }

    private void Awake()
    {
        ResetState();
    }

    public static void SetAltHeld(bool isHeld)
    {
        if (IsAltHeld == isHeld) return;
        IsAltHeld = isHeld;
        RefreshInputMode();
    }

    public static void SetModalActive(bool active)
    {
        if (HasActiveModal == active) return;
        HasActiveModal = active;
        RefreshInputMode();
    }

    private static void RefreshInputMode()
    {
        InputMode newMode;
        if (HasActiveModal)
        {
            newMode = InputMode.UIModal;
        }
        else if (IsAltHeld)
        {
            newMode = InputMode.HUDOverlay;
        }
        else
        {
            newMode = InputMode.GamePlay;
        }

        if (CurrentInputMode == newMode) return;

        CurrentInputMode = newMode;
        ApplyCursorState(CurrentInputMode);
        OnInputModeChanged?.Invoke(CurrentInputMode);
    }

    private static void ApplyCursorState(InputMode mode)
    {
        bool showCursor = mode != InputMode.GamePlay;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = showCursor;
    }
}
