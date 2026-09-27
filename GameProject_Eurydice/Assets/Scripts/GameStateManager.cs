using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Playing,
    Pause
}

public class GameStateManager : MonoBehaviour
{
    public static GameState gameState = GameState.Playing;
    public static event Action<GameState> OnGameStateChanged;

    private InputSystem_Actions inputActions;

    void Awake()
    {
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

    void OnBackPressed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public static void TogglePause()
    {
        SetGameState(gameState == GameState.Playing ? GameState.Pause : GameState.Playing);
    }

    public static void SetGameState(GameState newState)
    {
        if (gameState == newState) return;

        gameState = newState;
        Time.timeScale = (gameState == GameState.Pause) ? 0f : 1f;
        Cursor.lockState = (gameState == GameState.Pause) ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = gameState == GameState.Pause;

        OnGameStateChanged?.Invoke(gameState);
    }
}
