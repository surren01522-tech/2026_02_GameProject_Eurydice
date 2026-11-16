using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum DiscRotationAxis
{
    LocalX,
    LocalY,
    LocalZ
}

public enum DiscStartAngleMode
{
    UseGlobalDefault = 0,
    SpecificAngle = 1,
    RandomSnap = 2,
    AsIs = 4
}

[Serializable]
public struct DiscLink
{
    [Tooltip("타겟 인덱스의 요소를 같이 회전")]
    public int targetIndex;
    [Tooltip("회전 비율(1: 1대1, 2: 1대2)")]
    public float ratio;

    public DiscLink(int targetIndex, float ratio = 1f)
    {
        this.targetIndex = targetIndex;
        this.ratio = ratio;
    }
}

[Serializable]
public class DiscPiece
{
    public Transform pieceTransform;
    public DiscStartAngleMode startMode = DiscStartAngleMode.UseGlobalDefault;
    public float startAngle = 0f;
    public float targetAngle = 0f;
    public bool canRotate = true;
    public List<DiscLink> links = new List<DiscLink>();

    [FoldGroup("Overrides (Optional)", defaultExpanded: false)]
    public bool overrideSettings = false;
    public float customTolerance = 5f;
    public Collider clickCollider;

    [FoldGroup("소켓 연동 (선택)", defaultExpanded: false)]
    public ItemSocket boundSocket;
    public bool hideUntilSocketPlaced = false;

    [NonSerialized] public float logicalAngle;
    [NonSerialized] public Tween snapTween;

    [NonSerialized] private float visualAngle;
    [NonSerialized] private float tweenEndAngle;

    public Transform TargetTransform => pieceTransform;
    public bool IsSlotted => boundSocket == null || boundSocket.IsCorrect;
    public bool IsTweening => snapTween != null && snapTween.IsActive();

    public bool CheckIsCorrect(float defaultTolerance) =>
        Mathf.Abs(Mathf.DeltaAngle(logicalAngle, targetAngle)) <= (overrideSettings ? customTolerance : defaultTolerance);

    public void Init(DiscRotationAxis axis, DiscStartAngleMode globalMode, float stepAngle, float defaultTol)
    {
        if (pieceTransform != null && clickCollider == null)
            clickCollider = pieceTransform.GetComponentInChildren<Collider>();

        if (boundSocket != null && hideUntilSocketPlaced && pieceTransform != null)
            pieceTransform.gameObject.SetActive(boundSocket.IsCorrect);

        DiscStartAngleMode resolvedMode = (startMode == DiscStartAngleMode.UseGlobalDefault) ? globalMode : startMode;
        float tol = overrideSettings ? customTolerance : defaultTol;

        switch (resolvedMode)
        {
            case DiscStartAngleMode.SpecificAngle:
                SetAngle(startAngle, axis);
                break;

            case DiscStartAngleMode.RandomSnap:
                SetAngle(PickRandomStepAngle(stepAngle, tol), axis);
                break;

            case DiscStartAngleMode.AsIs:
                SyncFromTransform(axis);
                break;

            default:
                Debug.LogWarning($"[DiscPiece] 지원하지 않는 StartMode({(int)resolvedMode})입니다. RandomSnap으로 설정하세요.");
                SyncFromTransform(axis);
                break;
        }
    }

    private float PickRandomStepAngle(float stepAngle, float tol)
    {
        float step = stepAngle > 0f ? stepAngle : 45f;
        int maxSteps = Mathf.Max(1, Mathf.RoundToInt(360f / step));
        if (maxSteps <= 1) return targetAngle;

        int startOffset = UnityEngine.Random.Range(1, maxSteps);
        float selected = targetAngle;
        for (int i = 0; i < maxSteps - 1; i++)
        {
            int offset = 1 + (startOffset + i - 1) % (maxSteps - 1);
            float candidate = targetAngle - offset * step;
            selected = candidate;
            if (Mathf.Abs(Mathf.DeltaAngle(candidate, targetAngle)) > tol)
                break;
        }
        return selected;
    }

    private void SyncFromTransform(DiscRotationAxis axis)
    {
        logicalAngle = Mathf.Repeat(GetAxisAngle(axis), 360f);
        visualAngle = logicalAngle;
        tweenEndAngle = logicalAngle;
    }

    public float GetAxisAngle(DiscRotationAxis axis)
    {
        if (pieceTransform == null) return 0f;
        Vector3 euler = pieceTransform.localEulerAngles;
        return axis switch
        {
            DiscRotationAxis.LocalX => euler.x,
            DiscRotationAxis.LocalY => euler.y,
            DiscRotationAxis.LocalZ => euler.z,
            _ => 0f
        };
    }

    // 연타 중에도 스텝이 어긋나지 않도록 논리 목표 각도와 트윈 목표 각도를 별도로 누적 관리
    public void RotateStep(float deltaAngle, DiscRotationAxis axis, float duration, GameObject linkTarget, Action onComplete = null)
    {
        if (!canRotate || Mathf.Approximately(deltaAngle, 0f)) return;

        logicalAngle = Mathf.Repeat(logicalAngle + deltaAngle, 360f);
        float baseEnd = IsTweening ? tweenEndAngle : visualAngle;
        tweenEndAngle = baseEnd + deltaAngle;

        KillTween();

        if (duration <= 0f || pieceTransform == null)
        {
            SetAngle(logicalAngle, axis);
            onComplete?.Invoke();
            return;
        }

        float startVal = visualAngle;
        float endVal = tweenEndAngle;

        snapTween = DOVirtual.Float(startVal, endVal, duration, val =>
        {
            visualAngle = val;
            ApplyRotation(Mathf.Repeat(val, 360f), axis);
        })
        .SetEase(Ease.OutBack)
        .SetLink(linkTarget)
        .OnComplete(() =>
        {
            visualAngle = logicalAngle;
            tweenEndAngle = logicalAngle;
            ApplyRotation(logicalAngle, axis);
            snapTween = null;
            onComplete?.Invoke();
        });
    }

    public void SetAngle(float angle, DiscRotationAxis axis)
    {
        KillTween();
        logicalAngle = Mathf.Repeat(angle, 360f);
        visualAngle = logicalAngle;
        tweenEndAngle = logicalAngle;
        ApplyRotation(logicalAngle, axis);
    }

    public void StopTween(DiscRotationAxis axis, bool snapToLogical)
    {
        bool wasTweening = snapTween != null;
        KillTween();
        if (snapToLogical && wasTweening)
            SetAngle(logicalAngle, axis);
    }

    public void KillTween()
    {
        if (snapTween == null) return;
        snapTween.Kill();
        snapTween = null;
    }

    private void ApplyRotation(float angle, DiscRotationAxis axis)
    {
        if (pieceTransform == null) return;
        Vector3 euler = pieceTransform.localEulerAngles;
        if (axis == DiscRotationAxis.LocalX) euler.x = angle;
        else if (axis == DiscRotationAxis.LocalY) euler.y = angle;
        else if (axis == DiscRotationAxis.LocalZ) euler.z = angle;
        pieceTransform.localEulerAngles = euler;
    }
}
