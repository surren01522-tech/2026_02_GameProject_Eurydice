using System;
using System.Collections.Generic;
using UnityEngine;

public class MapMarker : MonoBehaviour
{
    public static readonly List<MapMarker> Active = new();
    public static event Action OnChanged;

    [SerializeField] private MapStaticMarkerData data = new();

    public string SceneName => gameObject.scene.name;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(data.markerId))
        {
            Debug.LogError($"[MapMarker] markerId is empty on '{name}'.", this);
            return;
        }
        if (!Active.Contains(this))
        {
            Active.Add(this);
            OnChanged?.Invoke();
        }
    }

    private void OnDisable()
    {
        if (Active.Remove(this))
            OnChanged?.Invoke();
    }

    public MapStaticMarkerData ToStaticData()
    {
        data.worldPosition = transform.position;
        return data.Clone();
    }

    private void Reset()
    {
        data.markerId = gameObject.name;
        data.label = gameObject.name;
        data.markerType = MapMarkerType.Landmark;
    }
}
