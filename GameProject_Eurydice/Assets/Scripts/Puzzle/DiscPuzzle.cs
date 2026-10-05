using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DiscPuzzle : PuzzleBase
{
    [FoldGroup("기본 설정")]
    [SerializeField] private DiscRotationAxis defaultRotationAxis = DiscRotationAxis.LocalZ;
    [SerializeField] private DiscStartAngleMode defaultStartMode = DiscStartAngleMode.RandomSnap;
    [SerializeField] private float defaultTolerance = 5f;
    [SerializeField] private bool useSnap = false;
    [SerializeField] private float snapAngle = 45f;
    [SerializeField] private float smoothSnapSpeed = 12f;
    [SerializeField] private bool invertAllRotations = false;
    [SerializeField] private LayerMask discLayerMask = ~0;
    [SerializeField] private float maxRayDistance = 10;

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

    private int activePieceIndex = -1;
    private float lastMouseAngle;
    private bool isDragging;
    private Vector2 cachedScreenPivot;
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
                if (sockets[i] != null && !sockets[i].IsPlaced) return false;
            return true;
        }
    }

    public bool CanRotatePieces => !lockRotationUntilSlotted || AreSocketsFulfilled;

    protected override void Awake()
    {
        base.Awake();
        InitializePieces();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
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
        var savedData = SaveManager.GetPuzzleState(uniqueId);
        if (savedData == null) return;

        if (savedData.isCompleted)
        {
            IsCompleted = true;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] != null)
                    pieces[i].SetAngle(pieces[i].targetAngle, defaultRotationAxis);
            }
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
    }

    private void SaveCurrentPuzzleState(bool completed)
    {
        List<float> angles = new List<float>();
        for (int i = 0; i < pieces.Count; i++)
            angles.Add(pieces[i] != null ? pieces[i].currentAngle : 0f);

        SaveManager.SavePuzzleState(uniqueId, completed, angles);
    }

    private void BindSocketEvents()
    {
        if (sockets == null) return;
        for (int i = 0; i < sockets.Count; i++)
            if (sockets[i] != null) sockets[i].OnPlaced += HandleSocketInstalled;
    }

    private void UnbindSocketEvents()
    {
        if (sockets == null) return;
        for (int i = 0; i < sockets.Count; i++)
            if (sockets[i] != null) sockets[i].OnPlaced -= HandleSocketInstalled;
    }

    private void OnDestroy() => UnbindSocketEvents();

    private void HandleSocketInstalled(ItemSocket socket) => CheckSolution();

    private void InitializePieces()
    {
        for (int i = 0; i < pieces.Count; i++)
            pieces[i]?.Init(defaultRotationAxis, defaultStartMode, snapAngle, defaultTolerance);

        if (AreAllPiecesCorrect())
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] != null && pieces[i].canRotate)
                {
                    pieces[i].ApplyDeltaAngle(snapAngle > 0f ? snapAngle : 45f, defaultRotationAxis);
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (!IsActive || IsCompleted) return;
        if (GameStateManager.HasActiveModal || (InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsSelectionMode)) return;
        if (Mouse.current == null) return;

        HandleDragInput();
    }

    private void HandleDragInput()
    {
        if (cam == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, maxRayDistance, discLayerMask);

            if (hitCount > 0)
            {
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
                    if (hitSocket != null && sockets != null && sockets.Contains(hitSocket))
                    {
                        if (!hitSocket.IsPlaced && InventoryUIManager.Instance != null)
                        {
                            InventoryUIManager.Instance.OpenForSelection(hitSocket);
                        }
                        return;
                    }
                }

                for (int i = 0; i < hitCount; i++)
                {
                    int pieceIdx = FindPieceIndexByCollider(hitBuffer[i].collider);
                    if (pieceIdx >= 0 && pieces[pieceIdx] != null)
                    {
                        DiscPiece clickedPiece = pieces[pieceIdx];
                        if (!clickedPiece.canRotate) return;

                        if (!CanRotatePieces)
                        {
                            TriggerSlotMissingFeedback();
                            return;
                        }

                        StopAllPieceSnaps();

                        activePieceIndex = pieceIdx;
                        Vector3 pivotPos = clickedPiece.pieceTransform != null ? clickedPiece.pieceTransform.position : transform.position;
                        cachedScreenPivot = cam.WorldToScreenPoint(pivotPos);
                        lastMouseAngle = CalculateMouseAngle(cachedScreenPivot);
                        isDragging = true;
                        return;
                    }
                }
            }
        }
        else if (isDragging && activePieceIndex >= 0 && activePieceIndex < pieces.Count)
        {
            DiscPiece activePiece = pieces[activePieceIndex];

            if (!Mouse.current.leftButton.isPressed)
            {
                if (useSnap && snapAngle > 0f)
                {
                    SnapAllAffectedPieces();
                }

                activePieceIndex = -1;
                isDragging = false;
                SaveCurrentPuzzleState(false);
                CheckSolution();
            }
            else
            {
                float currentAngle = CalculateMouseAngle(cachedScreenPivot);
                float delta = Mathf.DeltaAngle(lastMouseAngle, currentAngle);

                bool shouldInvert = invertAllRotations ^ (activePiece.overrideSettings && activePiece.invertRotation);
                if (shouldInvert) delta = -delta;

                visitedIndices.Clear();
                RotatePieceRecursive(activePieceIndex, delta);

                lastMouseAngle = currentAngle;
                CheckSolution();
            }
        }
    }

    private void RotatePieceRecursive(int pieceIdx, float delta)
    {
        if (pieceIdx < 0 || pieceIdx >= pieces.Count) return;
        if (visitedIndices.Contains(pieceIdx)) return;

        visitedIndices.Add(pieceIdx);
        DiscPiece piece = pieces[pieceIdx];
        if (piece == null || !piece.canRotate) return;

        piece.ApplyDeltaAngle(delta, defaultRotationAxis);

        if (piece.links != null)
        {
            for (int i = 0; i < piece.links.Count; i++)
            {
                DiscLink link = piece.links[i];
                RotatePieceRecursive(link.targetIndex, delta * link.ratio);
            }
        }
    }

    private void SnapAllAffectedPieces()
    {
        foreach (int idx in visitedIndices)
        {
            if (idx >= 0 && idx < pieces.Count && pieces[idx] != null && pieces[idx].canRotate)
            {
                DiscPiece p = pieces[idx];
                float targetSnap = Mathf.Round(p.currentAngle / snapAngle) * snapAngle;
                targetSnap = Mathf.Repeat(targetSnap, 360f);

                if (p.snapCoroutine != null) StopCoroutine(p.snapCoroutine);
                p.snapCoroutine = StartCoroutine(CoSmoothSnap(p, targetSnap));
            }
        }
    }

    private void StopAllPieceSnaps()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i]?.snapCoroutine == null) continue;
            StopCoroutine(pieces[i].snapCoroutine);
            pieces[i].snapCoroutine = null;
        }
    }

    private int FindPieceIndexByCollider(Collider col)
    {
        if (col == null) return -1;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;
            if (pieces[i].clickCollider == col) return i;
            if (pieces[i].pieceTransform != null && col.transform.IsChildOf(pieces[i].pieceTransform)) return i;
        }

        return -1;
    }

    private float CalculateMouseAngle(Vector2 screenPivot)
    {
        Vector2 dir = Mouse.current.position.ReadValue() - screenPivot;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private IEnumerator CoSmoothSnap(DiscPiece piece, float targetSnap)
    {
        while (Mathf.Abs(Mathf.DeltaAngle(piece.currentAngle, targetSnap)) > 0.1f)
        {
            float nextAngle = Mathf.MoveTowardsAngle(piece.currentAngle, targetSnap, smoothSnapSpeed * 30f * Time.deltaTime);
            piece.SetAngle(nextAngle, defaultRotationAxis);
            yield return null;
        }

        piece.SetAngle(targetSnap, defaultRotationAxis);
        piece.snapCoroutine = null;
        SaveCurrentPuzzleState(false);
        CheckSolution();
    }

    private void TriggerSlotMissingFeedback()
    {
        if (sockets == null) return;
        for (int i = 0; i < sockets.Count; i++)
        {
            if (sockets[i] != null && !sockets[i].IsPlaced)
            {
                sockets[i].TriggerMissingFeedback();
                return;
            }
        }
    }

    public bool AreAllPiecesCorrect()
    {
        if (pieces == null || pieces.Count == 0) return false;
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] == null || !pieces[i].CheckIsCorrect(defaultTolerance)) return false;
        return true;
    }

    public void CheckSolution()
    {
        if (IsCompleted || !AreSocketsFulfilled || !AreAllPiecesCorrect()) return;

        isDragging = false;
        activePieceIndex = -1;
        StopAllPieceSnaps();

        if (snapToTargetOnSolved && pieces != null)
        {
            for (int i = 0; i < pieces.Count; i++)
                pieces[i]?.SetAngle(pieces[i].targetAngle, defaultRotationAxis);
        }

        if (debugLogSuccess)
        {
            Debug.Log($"[DiscPuzzle] Successfully solved! All {pieces.Count} pieces aligned to target angles.");
            for (int i = 0; i < pieces.Count; i++)
                Debug.Log($"[DiscPuzzle] Piece #{i} ('{pieces[i]?.pieceTransform?.name}') Angle: {pieces[i]?.currentAngle:F1} deg (Target: {pieces[i]?.targetAngle:F1} deg)");
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
        StopAllPieceSnaps();
        for (int i = 0; i < pieces.Count; i++)
            pieces[i]?.SetAngle(pieces[i].targetAngle, defaultRotationAxis);

        CheckSolution();
        if (!IsCompleted && !AreSocketsFulfilled)
            Debug.LogWarning("[DiscPuzzle] ForceSolve: Rotated all pieces to target angles, but puzzle is not solved because required sockets are not filled.");
    }

    [ContextMenu("Debug: Reset To Start Angles")]
    public void ResetToStartAngles()
    {
        StopAllPieceSnaps();
        InitializePieces();
    }

    [ContextMenu("Debug: Shuffle Discs")]
    public void ShuffleDiscs()
    {
        StopAllPieceSnaps();
        float step = snapAngle > 0f ? snapAngle : 45f;
        int maxSteps = Mathf.Max(1, Mathf.RoundToInt(360f / step));
        for (int i = 0; i < pieces.Count; i++)
            if (pieces[i] != null && pieces[i].canRotate)
                pieces[i].SetAngle(UnityEngine.Random.Range(0, maxSteps) * step, defaultRotationAxis);
    }

    protected override void OnExit()
    {
        if (isDragging) { activePieceIndex = -1; isDragging = false; }
    }
}
