using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float range = 2.5f;
    [SerializeField] private LayerMask layer = ~0;

    private readonly Collider[] hits = new Collider[8];
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
            UpdateTarget();
        }
    }

    private void UpdateTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, hits, layer);
        IInteractable best = null;
        float minAngle = 75f;

        for (int i = 0; i < count; i++)
        {
            if (hits[i].gameObject == gameObject) continue;

            var interactables = hits[i].GetComponentsInParent<IInteractable>();
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

            Vector3 dir = hits[i].transform.position - transform.position;
            dir.y = 0;
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle < minAngle)
            {
                minAngle = angle;
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
