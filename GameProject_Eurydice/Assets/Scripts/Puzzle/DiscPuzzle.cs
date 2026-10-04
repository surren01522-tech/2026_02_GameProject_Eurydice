using UnityEngine;

public class DiscPuzzle : PuzzleBase
{
    [Header("원판 퍼즐 설정")]
    [SerializeField] private bool lockRotationUntilSlotted = false;

    public bool LockRotationUntilSlotted => lockRotationUntilSlotted;

    protected override void OnEnter()
    {
        // 원판 퍼즐 진입 시 고유 초기화 (추후 회전 조작 연동)
    }

    protected override void OnExit()
    {
        // 원판 퍼즐 퇴장 시 고유 정리
    }
}
