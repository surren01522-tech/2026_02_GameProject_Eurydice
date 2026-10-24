using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TimeoutZone : MonoBehaviour
{
    [Header("체류 시간 설정")]
    [Tooltip("구역 내 체류 허용 시간 (초)")]
    [SerializeField] private float timeoutDuration = 5f;

    [Header("스폰 대상 위치")]
    [Tooltip("시간 초과 시 이동시킬 목표 트랜스폼")]
    [SerializeField] private Transform targetSpawnPoint;

    [Header("페이드 & 사운드 연출")]
    [Tooltip("화면이 어두워지고 밝아지는 시간 (초)")]
    [SerializeField] private float fadeDuration = 0.8f;
    [Tooltip("화면이 완전히 암전된 상태로 머무는 시간 (초)")]
    [SerializeField] private float blackHoldDuration = 0.4f;
    [SerializeField] private AudioClip teleportSound;

    [Header("이벤트")]
    [SerializeField] private UnityEvent onTimeoutTriggered;
    [SerializeField] private UnityEvent onTeleportCompleted;

    private float stayTimer;
    private bool isPlayerInside;
    private bool isTeleporting;
    private PlayerController player;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (!isPlayerInside || isTeleporting || player == null) return;

        stayTimer += Time.deltaTime;
        if (stayTimer >= timeoutDuration)
        {
            StartCoroutine(ExecuteTimeoutTeleportRoutine());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        if (TryGetPlayer(other, out var player))
        {
            this.player = player;
            isPlayerInside = true;
            stayTimer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isTeleporting) return;

        if (TryGetPlayer(other, out _))
        {
            isPlayerInside = false;
            stayTimer = 0f;
            player = null;
        }
    }

    private IEnumerator ExecuteTimeoutTeleportRoutine()
    {
        isTeleporting = true;
        onTimeoutTriggered?.Invoke();

        if (player != null)
            player.enabled = false;

        yield return ScreenFader.FadeOutRoutine(fadeDuration);

        if (player != null && targetSpawnPoint != null)
        {
            Vector3 targetPos = targetSpawnPoint.position;
            Quaternion targetRot = targetSpawnPoint.rotation;
            Vector3 camPos = targetPos - targetSpawnPoint.forward * 3.5f + Vector3.up * 1.5f;

            player.Teleport(targetPos, targetRot, camPos, targetRot);
            SceneStreamer.Instance?.RefreshZonesForPosition(targetPos);

            if (teleportSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAt(teleportSound, targetPos);
            }
        }

        if (blackHoldDuration > 0f)
            yield return new WaitForSeconds(blackHoldDuration);

        onTeleportCompleted?.Invoke();

        yield return ScreenFader.FadeInRoutine(fadeDuration);

        if (player != null)
            player.enabled = true;

        stayTimer = 0f;
        isTeleporting = false;
        isPlayerInside = false;
    }

    private bool TryGetPlayer(Collider other, out PlayerController player)
    {
        player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        return player != null;
    }
}
