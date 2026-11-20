using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private LayerMask layer = ~0;
    [SerializeField] private float maxAngle = 75f;

    private Collider[] hits = new Collider[64];
    private PlayerController player;
    private IInteractable target;

    public event Action<IInteractable> OnTargetChanged;

    private void Awake() => player = GetComponent<PlayerController>();

    private void Update()
    {
        if (!GameStateManager.IsGamePlaying || GameStateManager.HasActiveModal)
        {
            SetTarget(null);
            return;
        }

        if (target is UnityEngine.Object u && u == null)
            SetTarget(null);

        UpdateTarget();

        if (target != null && player?.InputActions != null && player.InputActions.Player.Interact.WasPressedThisFrame())
        {
            var interactTarget = target;
            SetTarget(null);
            interactTarget.Interact(player);
            if (GameStateManager.IsGamePlaying && !GameStateManager.HasActiveModal)
                UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, hits, layer);
        while (count == hits.Length && hits.Length < 256)
        {
            hits = new Collider[hits.Length * 2];
            count = Physics.OverlapSphereNonAlloc(transform.position, range, hits, layer);
        }

        IInteractable best = null;
        float minAngle = maxAngle;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            if (col == null || col.transform.IsChildOf(transform)) continue;

            var interactables = col.GetComponentsInParent<IInteractable>();
            IInteractable interactable = null;
            for (int j = 0; j < interactables.Length; j++)
            {
                if (interactables[j] != null && interactables[j].CanInteract && !(interactables[j] is UnityEngine.Object u && u == null))
                {
                    interactable = interactables[j];
                    break;
                }
            }
            if (interactable == null) continue;

            Vector3 targetPoint = col.bounds.ClosestPoint(transform.position);
            Vector3 dir = targetPoint - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
            {
                Transform refT = interactable.TargetTransform != null ? interactable.TargetTransform : col.transform;
                dir = refT.position - transform.position;
                dir.y = 0f;
            }

            float sqrDist = dir.sqrMagnitude;
            float angle = sqrDist < 0.0001f ? 0f : Vector3.Angle(transform.forward, dir);

            if (angle < minAngle - 0.5f || (Mathf.Abs(angle - minAngle) <= 0.5f && sqrDist < minSqrDist))
            {
                minAngle = angle;
                minSqrDist = sqrDist;
                best = interactable;
            }
        }

        SetTarget(best);
    }

    private void SetTarget(IInteractable newTarget)
    {
        bool isDestroyed = target is UnityEngine.Object u && u == null;
        if (target == newTarget && !isDestroyed) return;

        target = newTarget;
        OnTargetChanged?.Invoke(target);
    }

    private void OnDisable() => SetTarget(null);
}
