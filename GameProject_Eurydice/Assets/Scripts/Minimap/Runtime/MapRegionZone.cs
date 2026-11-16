using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MapRegionZone : MonoBehaviour
{
    [SerializeField] private string mapId;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.25f);

    public string MapId => mapId;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) MinimapManager.Instance?.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) MinimapManager.Instance?.ExitZone(this);
    }

    private void OnDisable() => MinimapManager.Instance?.ExitZone(this);

    private void OnDrawGizmos()
    {
        if (!TryGetComponent(out Collider col)) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
