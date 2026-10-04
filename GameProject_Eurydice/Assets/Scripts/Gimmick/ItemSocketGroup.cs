using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ItemSocketGroup : MonoBehaviour
{
    [Header("소켓 설정")]
    [SerializeField] private ItemSocket[] sockets;
    [SerializeField] private bool requireOrder = false;

    [Header("상태")]
    [SerializeField] private bool isAllPlaced = false;

    public bool IsAllPlaced => isAllPlaced;
    public IReadOnlyList<ItemSocket> Sockets => sockets;

    public event Action OnAllPlaced;
    public UnityEvent onAllPlacedEvent;

    private void Awake()
    {
        if (sockets == null || sockets.Length == 0)
            sockets = GetComponentsInChildren<ItemSocket>(true);

        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] != null)
                sockets[i].OnPlaced += HandleSocketPlaced;
        }

        UpdateOrderLock();
        CheckAllPlaced(notify: false);
    }

    private void OnDestroy()
    {
        if (sockets == null) return;

        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] != null)
                sockets[i].OnPlaced -= HandleSocketPlaced;
        }
    }

    private void HandleSocketPlaced(ItemSocket socket)
    {
        UpdateOrderLock();
        CheckAllPlaced(notify: true);
    }

    /// <summary>
    /// 순차 설치 옵션일 경우 다음 차례가 아닌 소켓들을 잠금 처리합니다.
    /// </summary>
    private void UpdateOrderLock()
    {
        if (!requireOrder || sockets == null) return;

        bool foundFirstPending = false;
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null) continue;

            if (sockets[i].IsPlaced)
            {
                sockets[i].IsLocked = false;
            }
            else if (!foundFirstPending)
            {
                sockets[i].IsLocked = false;
                foundFirstPending = true;
            }
            else
            {
                sockets[i].IsLocked = true;
            }
        }
    }

    public void CheckAllPlaced(bool notify = true)
    {
        if (isAllPlaced || sockets == null || sockets.Length == 0) return;

        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null || !sockets[i].IsPlaced)
                return;
        }

        isAllPlaced = true;

        if (notify)
        {
            OnAllPlaced?.Invoke();
            onAllPlacedEvent?.Invoke();
        }
    }
}
