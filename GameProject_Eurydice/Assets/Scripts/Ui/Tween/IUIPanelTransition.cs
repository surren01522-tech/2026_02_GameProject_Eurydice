using System;
using UnityEngine;

/// <summary>
/// UI 패널의 열기/닫기 연출을 위한 추상 인터페이스
/// </summary>
public interface IUIPanelTransition
{
    void PlayOpen(Vector3? originScreenPos = null, Action onComplete = null);
    void PlayClose(Vector3? targetScreenPos = null, Action onComplete = null);
    void StopImmediate();
}
