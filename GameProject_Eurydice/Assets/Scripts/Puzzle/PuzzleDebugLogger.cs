using UnityEngine;
using UnityEngine.Events;

public class PuzzleDebugLogger : MonoBehaviour
{
    [Header("타겟 퍼즐(선택)")]
    [SerializeField] private PuzzleBase targetPuzzle;

    private void Awake()
    {
        if (targetPuzzle != null)
        {
            targetPuzzle.OnPuzzleCompleted += HandlePuzzleCompleted;
        }
    }

    private void OnDestroy()
    {
        if (targetPuzzle != null)
        {
            targetPuzzle.OnPuzzleCompleted -= HandlePuzzleCompleted;
        }
    }

    public void HandlePuzzleCompleted()
    {
        Debug.Log("Puzzle Clear!");
    }

    public void TriggerSuccess()
    {
        HandlePuzzleCompleted();
    }
}
