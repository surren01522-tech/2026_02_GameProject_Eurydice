using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class DiscPuzzle : PuzzleBase
{
    [FoldGroup("기본 설정")]
    [SerializeField] private DiscRotationAxis defaultRotationAxis = DiscRotationAxis.LocalZ;
    [SerializeField] private DiscStartAngleMode defaultStartMode = DiscStartAngleMode.RandomSnap;
    [SerializeField] private float defaultTolerance = 5f;
    [FormerlySerializedAs("snapAngle")]
    [SerializeField, Min(0.1f)] private float stepAngle = 45f;
    [FormerlySerializedAs("smoothSnapSpeed")]
    [SerializeField, Min(0f)] private float rotateSpeed = 12f;
    [SerializeField] private LayerMask discLayerMask = ~0;
    [SerializeField] private float maxRayDistance = 10f;

    [FoldGroup("조각 설정")]
    [SerializeField] private List<DiscPiece> pieces = new List<DiscPiece>();

    [FoldGroup("장착 슬롯 설정")]
    [Tooltip("List of item sockets required to solve this puzzle")]
    [SerializeField] private List<ItemSocket> sockets = new List<ItemSocket>();
    [Tooltip("참일 경우 모든 슬롯이 장착될때까지 회전을 잠급니다")]
    [SerializeField] private bool lockRotationUntilSlotted = false;

    [FoldGroup("해결 이벤트")]
    [SerializeField] private bool snapToTargetOnSolved = true;
    [SerializeField] private UnityEvent onSolvedEvent;
    [SerializeField] private AudioClip solvedSound;

    [FoldGroup("디버그 설정")]
    [SerializeField] private bool debugLogSuccess = true;

    private readonly HashSet<int> visitedIndices = new HashSet<int>();
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public bool LockRotationUntilSlotted => lockRotationUntilSlotted;
    public IReadOnlyList<DiscPiece> Pieces => pieces;
    public IReadOnlyList<ItemSocket> Sockets => sockets;

    private Camera cam;

    public bool AreSocketsFulfilled
    {
        get
        {
            if (sockets == null) return true;
            for (int i = 0; i < sockets.Count; i++)
                if (sockets[i] != null && !sockets[i].IsCorrect) return false;
            return true;
        }
    }

    public bool CanRotatePieces => !lockRotationUntilSlotted || AreSocketsFulfilled;

    protected override void Awake()
    {
        base.Awake();
        InitializePieces();
        UpdatePieceSocketBindings();
    }

    private void OnValidate()
    {
        if (pieces == null) return;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i]?.pieceTransform != null && pieces[i].clickCollider == null)
                pieces[i].clickCollider = pieces[i].pieceTransform.GetComponentInChildren<Collider>();
    }

    private void Start()
    {
        cam = Camera.main;
        BindSocketEvents();
        RestoreSavedState();
    }

    private void RestoreSavedState()
    {
        var savedData = SaveManager.GetPuzzleState(uniqueId, gameObject.scene.name);
        if (savedData == null)
        {
            UpdatePieceSocketBindings();
            return;
        }

        if (savedData.isCompleted)
        {
            IsCompleted = true;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] != null)
                {
                    pieces[i].SetAngle(pieces[i].targetAngle, defaultRotationAxis);
                    if (pieces[i].pieceTransform != null)
                        pieces[i].pieceTransform.gameObject.SetActive(true);
                }
            }
            UpdatePieceSocketBindings();
            return;
        }

        if (savedData.pieceAngles != null && savedData.pieceAngles.Count > 0)
        {
            for (int i = 0; i < pieces.Count && i < savedData.pieceAngles.Count; i++)
            {
                if (pieces[i] != null)
                    pieces[i].SetAngle(savedData.pieceAngles[i], defaultRotationAxis);
            }
        }

        UpdatePieceSocketBindings();
    }

    private void SaveCurrentPuzzleState(bool completed)
    {
        List<float> angles = new List<float>();
        for (int i = 0; i < pieces.Count; i++)
            angles.Add(pieces[i] != null ? pieces[i].logicalAngle : 0f);

        SaveManager.SavePuzzleState(uniqueId, completed, angles, gameObject.scene.name);
    }

    private void BindSocketEvents()
    {
        if (sockets != null)
        {
            for (int i = 0; i < sockets.Count; i++)
                if (sockets[i] != null) sockets[i].OnPlaced += HandleSocketInstalled;
        }

        if (pieces != null)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var s = pieces[i]?.boundSocket;
                if (s != null && (sockets == null || !sockets.Contains(s)))
                    s.OnPlaced += HandleSocketInstalled;
            }
        }
    }

    private void UnbindSocketEvents()
    {
        if (sockets != null)
        {
            for (int i = 0; i < sockets.Count; i++)
                if (sockets[i] != null) sockets[i].OnPlaced -= HandleSocketInstalled;
        }

        if (pieces != null)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var s = pieces[i]?.boundSocket;
                if (s != null && (sockets == null || !sockets.Contains(s)))
                    s.OnPlaced -= HandleSocketInstalled;
            }
        }
    }

    private void OnDestroy()
    {
        StopAllPieceSnaps(snapToLogical: false);
        UnbindSocketEvents();
    }

    private void HandleSocketInstalled(ItemSocket socket)
    {
        UpdatePieceSocketBindings();
        CheckSolution();
    }

    public void UpdatePieceSocketBindings()
    {
        if (pieces == null) return;
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (p == null) continue;

            if (p.boundSocket != null)
            {
                bool slotted = IsCompleted || p.boundSocket.IsCorrect;
                if (p.hideUntilSocketPlaced && p.pieceTransform != null)
                    p.pieceTransform.gameObject.SetActive(slotted);
            }
            else if (IsCompleted && p.hideUntilSocketPlaced && p.pieceTransform != null)
            {
                p.pieceTransform.gameObject.SetActive(true);
            }
        }
    }

    private bool IsBoundSocket(ItemSocket socket)
    {
        if (pieces == null || socket == null) return false;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i]?.boundSocket == socket) return true;
        return false;
    }

    private void InitializePieces()
    {
        for (int i = 0; i < pieces.Count; i++)
            pieces[i]?.Init(defaultRotationAxis, defaultStartMode, stepAngle, defaultTolerance);

        if (AreAllPiecesCorrect())
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] != null && pieces[i].canRotate)
                {
                    visitedIndices.Clear();
                    RotatePieceRecursive(i, stepAngle, 0f, null);
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (!IsActive || IsCompleted) return;
        if (GameStateManager.HasActiveModal || (InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsSelectionMode)) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        HandleClickInput();
    }

    private void HandleClickInput()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, maxRayDistance, discLayerMask);
        if (hitCount <= 0) return;

        for (int i = 1; i < hitCount; i++)
        {
            var key = hitBuffer[i];
            int j = i - 1;
            while (j >= 0 && hitBuffer[j].distance > key.distance)
            {
                hitBuffer[j + 1] = hitBuffer[j];
                j--;
            }
            hitBuffer[j + 1] = key;
        }

        for (int i = 0; i < hitCount; i++)
        {
            ItemSocket hitSocket = hitBuffer[i].collider.GetComponentInParent<ItemSocket>();
            if (hitSocket != null && (sockets == null || sockets.Contains(hitSocket) || IsBoundSocket(hitSocket)))
            {
                if (hitSocket.CanInteract)
                {
                    hitSocket.Interact(null);
                    return;
                }
            }
        }

        for (int i = 0; i < hitCount; i++)
        {
            int pieceIdx = FindPieceIndexByCollider(hitBuffer[i].collider);
            if (pieceIdx < 0 || pieces[pieceIdx] == null) continue;

            DiscPiece clickedPiece = pieces[pieceIdx];
            if (!clickedPiece.IsSlotted)
            {
                if (clickedPiece.boundSocket != null && clickedPiece.boundSocket.CanInteract)
                {
                    clickedPiece.boundSocket.Interact(null);
                    return;
                }
                InteractionFeedbackUIManager.Instance?.Show("필요한 부품을 먼저 끼워야 합니다.");
                return;
            }

            if (!CanRotatePieces)
            {
                InteractionFeedbackUIManager.Instance?.Show("필요한 장치를 먼저 끼워야 합니다.");
                return;
            }

            if (!clickedPiece.canRotate) return;

            RotateClickedPiece(pieceIdx);
            return;
        }
    }

    private void RotateClickedPiece(int pieceIdx)
    {
        visitedIndices.Clear();
        float duration = rotateSpeed > 0f ? Mathf.Clamp(2.5f / rotateSpeed, 0.08f, 0.35f) : 0f;
        RotatePieceRecursive(pieceIdx, stepAngle, duration, CheckSolution);
        SaveCurrentPuzzleState(false);

        if (duration <= 0f)
            CheckSolution();
    }

    private void RotatePieceRecursive(int pieceIdx, float delta, float duration, Action onTweenComplete)
    {
        if (pieceIdx < 0 || pieceIdx >= pieces.Count) return;
        if (!visitedIndices.Add(pieceIdx)) return;

        DiscPiece piece = pieces[pieceIdx];
        if (piece == null || !piece.canRotate) return;

        piece.RotateStep(delta, defaultRotationAxis, duration, gameObject, duration > 0f ? onTweenComplete : null);

        if (piece.links == null) return;
        for (int i = 0; i < piece.links.Count; i++)
        {
            DiscLink link = piece.links[i];
            RotatePieceRecursive(link.targetIndex, delta * link.ratio, duration, onTweenComplete);
        }
    }

    private void StopAllPieceSnaps(bool snapToLogical)
    {
        if (pieces == null) return;
        for (int i = 0; i < pieces.Count; i++)
            pieces[i]?.StopTween(defaultRotationAxis, snapToLogical);
    }

    private bool HasActivePieceTween()
    {
        if (pieces == null) return false;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] != null && pieces[i].IsTweening) return true;
        return false;
    }

    private int FindPieceIndexByCollider(Collider col)
    {
        if (col == null) return -1;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;
            if (pieces[i].clickCollider == col) return i;
            if (pieces[i].pieceTransform != null && col.transform.IsChildOf(pieces[i].pieceTransform)) return i;
            if (pieces[i].boundSocket != null && col.transform.IsChildOf(pieces[i].boundSocket.transform)) return i;
        }

        return -1;
    }

    public bool AreAllPiecesCorrect()
    {
        if (pieces == null || pieces.Count == 0) return false;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] == null || !pieces[i].IsSlotted || !pieces[i].CheckIsCorrect(defaultTolerance)) return false;
        return true;
    }

    public void CheckSolution()
    {
        if (IsCompleted || HasActivePieceTween() || !AreSocketsFulfilled || !AreAllPiecesCorrect()) return;

        StopAllPieceSnaps(snapToLogical: false);

        if (snapToTargetOnSolved && pieces != null)
        {
            for (int i = 0; i < pieces.Count; i++)
                pieces[i]?.SetAngle(pieces[i].targetAngle, defaultRotationAxis);
        }

        if (debugLogSuccess)
        {
            Debug.Log($"[DiscPuzzle] Successfully solved! All {pieces.Count} pieces aligned to target angles.");
            for (int i = 0; i < pieces.Count; i++)
                Debug.Log($"[DiscPuzzle] Piece #{i} ('{pieces[i]?.pieceTransform?.name}') Angle: {pieces[i]?.logicalAngle:F1} deg (Target: {pieces[i]?.targetAngle:F1} deg)");
        }

        if (solvedSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(solvedSound, transform.position);

        onSolvedEvent?.Invoke();
        SaveCurrentPuzzleState(true);
        CompletePuzzle();
    }

    [ContextMenu("Debug: Force Solve")]
    public void ForceSolve()
    {
        StopAllPieceSnaps(snapToLogical: false);
        for (int i = 0; i < pieces.Count; i++)
            pieces[i]?.SetAngle(pieces[i].targetAngle, defaultRotationAxis);

        CheckSolution();
        if (!IsCompleted && !AreSocketsFulfilled)
            Debug.LogWarning("[DiscPuzzle] ForceSolve: Rotated all pieces to target angles, but puzzle is not solved because required sockets are not filled.");
    }

    [ContextMenu("Debug: Reset To Start Angles")]
    public void ResetToStartAngles()
    {
        StopAllPieceSnaps(snapToLogical: false);
        InitializePieces();
    }

    [ContextMenu("Debug: Shuffle Discs")]
    public void ShuffleDiscs()
    {
        StopAllPieceSnaps(snapToLogical: false);
        float step = stepAngle > 0f ? stepAngle : 45f;
        int maxSteps = Mathf.Max(1, Mathf.RoundToInt(360f / step));
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] != null && pieces[i].canRotate)
                pieces[i].SetAngle(pieces[i].targetAngle - UnityEngine.Random.Range(0, maxSteps) * step, defaultRotationAxis);
    }

    protected override void OnExit()
    {
        StopAllPieceSnaps(snapToLogical: true);
    }
}
