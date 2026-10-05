using System;
using Unity.Cinemachine;
using UnityEngine;

public abstract class PuzzleBase : MonoBehaviour, IInteractable
{
    public static PuzzleBase ActivePuzzle { get; private set; }

    [FoldGroup("상호작용 팝업 설정")]
    [SerializeField] private string interactionPrompt = "Interact";
    [SerializeField] private InteractionDisplayMode displayMode = InteractionDisplayMode.Floating;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.5f, 0);

    [FoldGroup("카메라 설정")]
    [SerializeField] private CinemachineCamera puzzleCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int defaultPriority = -10;

    [FoldGroup("ID 설정")]
    [SerializeField] protected string uniqueId;

    [FoldGroup("퍼즐 상태")]
    [SerializeField] private bool isCompleted = false;

    public string UniqueId => uniqueId;
    public bool IsActive { get; private set; }
    public bool IsCompleted { get => isCompleted; protected set => isCompleted = value; }

    public event Action OnPuzzleEntered;
    public event Action OnPuzzleExited;
    public event Action OnPuzzleCompleted;

    public string InteractionPrompt => interactionPrompt;
    public InteractionDisplayMode DisplayMode => displayMode;
    public Vector3 WorldOffset => worldOffset;
    public Transform TargetTransform => this != null ? transform : null;
    public bool CanInteract => !IsActive && !isCompleted;

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = Guid.NewGuid().ToString();
        }
    }

    protected virtual void Awake()
    {
        if (puzzleCamera != null)
            puzzleCamera.Priority = defaultPriority;
    }

    public void Interact(PlayerController player)
    {
        EnterPuzzle();
    }

    /// <summary>
    /// 퍼즐 모드 진입: 조작 잠금, 전용 카메라 전환, UI 표시
    /// </summary>
    public virtual void EnterPuzzle()
    {
        if (IsActive || isCompleted) return;

        IsActive = true;
        ActivePuzzle = this;

        GameStateManager.SetPuzzleActive(true);

        if (puzzleCamera != null)
            puzzleCamera.Priority = activePriority;

        PuzzleUI.Instance?.Show(ExitPuzzle);

        OnEnter();
        OnPuzzleEntered?.Invoke();
    }

    /// <summary>
    /// 퍼즐 모드 종료: 조작 및 시점 원복, UI 숨김
    /// </summary>
    public virtual void ExitPuzzle()
    {
        if (!IsActive) return;

        IsActive = false;
        if (ActivePuzzle == this) ActivePuzzle = null;

        if (puzzleCamera != null)
            puzzleCamera.Priority = defaultPriority;

        PuzzleUI.Instance?.Hide();

        GameStateManager.SetPuzzleActive(false);

        OnExit();
        OnPuzzleExited?.Invoke();
    }

    public virtual void CompletePuzzle()
    {
        isCompleted = true;
        OnPuzzleCompleted?.Invoke();
        ExitPuzzle();
    }

    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }

    protected virtual void OnDisable()
    {
        if (IsActive)
            ExitPuzzle();
    }
}
