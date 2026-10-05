using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SceneStreamZone : MonoBehaviour
{
    [SerializeField] private List<string> scenes = new List<string>();
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.4f, 0.25f);

    private Collider col;

    public IReadOnlyList<string> Scenes => scenes;

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public bool Contains(Vector3 point) => col != null && col.ClosestPoint(point) == point;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            SceneStreamer.Instance?.OnZoneEntered(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null)
            SceneStreamer.Instance?.OnZoneExited(this);
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
