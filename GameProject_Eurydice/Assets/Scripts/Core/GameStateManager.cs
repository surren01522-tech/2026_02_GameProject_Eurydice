using System;
using UnityEngine;

public enum GameState
{
    Playing,
    Pause
}

public class GameStateManager : MonoBehaviour
{
    public static GameState gameState = GameState.Playing;
    public static event Action<GameState> OnGameStateChanged;

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
