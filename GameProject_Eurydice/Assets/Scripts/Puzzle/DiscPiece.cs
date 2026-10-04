using System;
using System.Collections.Generic;
using UnityEngine;

public enum DiscRotationAxis
{
    LocalX,
    LocalY,
    LocalZ
}

public enum DiscStartAngleMode
{
    UseGlobalDefault,
    SpecificAngle,
    RandomSnap,
    FullRandom,
    AsIs
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
    public bool invertRotation = false;
    public Collider clickCollider;

    [NonSerialized] public float currentAngle;
    [NonSerialized] public Coroutine snapCoroutine;

    public Transform TargetTransform => pieceTransform;

    /// <summary>
    /// 목표 각도와의 오차 범위를 비교하여 올바른 위치에 도달했는지 확인합니다.
    /// </summary>
    public bool CheckIsCorrect(float defaultTolerance) =>
        Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= (overrideSettings ? customTolerance : defaultTolerance);

    public void Init(DiscRotationAxis axis, DiscStartAngleMode globalMode, float snapStep, float defaultTol)
    {
        if (pieceTransform != null && clickCollider == null)
            clickCollider = pieceTransform.GetComponentInChildren<Collider>();

        DiscStartAngleMode resolvedMode = (startMode == DiscStartAngleMode.UseGlobalDefault) ? globalMode : startMode;
        float tol = overrideSettings ? customTolerance : defaultTol;

        switch (resolvedMode)
        {
            case DiscStartAngleMode.SpecificAngle:
                SetAngle(startAngle, axis);
                break;

            case DiscStartAngleMode.RandomSnap:
                float step = snapStep > 0f ? snapStep : 45f;
                int maxSteps = Mathf.Max(1, Mathf.RoundToInt(360f / step));
                float selectedSnap = 0f;
                int attempts = 20;
                while (attempts-- > 0)
                {
                    selectedSnap = UnityEngine.Random.Range(0, maxSteps) * step;
                    if (Mathf.Abs(Mathf.DeltaAngle(selectedSnap, targetAngle)) > tol)
                        break;
                }
                SetAngle(selectedSnap, axis);
                break;

            case DiscStartAngleMode.FullRandom:
                float randAngle = UnityEngine.Random.Range(0f, 360f);
                if (Mathf.Abs(Mathf.DeltaAngle(randAngle, targetAngle)) <= tol)
                    randAngle = Mathf.Repeat(randAngle + 180f, 360f);
                SetAngle(randAngle, axis);
                break;

            case DiscStartAngleMode.AsIs:
            default:
                currentAngle = GetAxisAngle(axis);
                break;
        }
    }

    /// <summary>
    /// 지정된 축의 로컬 오일러 각도를 반환합니다.
    /// </summary>
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

    /// <summary>
    /// 상대 각도 변화량을 더해 회전을 적용합니다.
    /// </summary>
    public void ApplyDeltaAngle(float deltaAngle, DiscRotationAxis axis)
    {
        if (canRotate) SetAngle(currentAngle + deltaAngle, axis);
    }

    /// <summary>
    /// 절대 각도를 지정하여 회전을 적용합니다.
    /// </summary>
    public void SetAngle(float angle, DiscRotationAxis axis)
    {
        currentAngle = Mathf.Repeat(angle, 360f);
        ApplyRotation(currentAngle, axis);
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
