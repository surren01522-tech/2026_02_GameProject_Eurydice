using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMarkerStyleSet", menuName = "Minimap/Marker Style Set")]
public class MapMarkerStyleSet : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public MapMarkerType type;
        public Sprite icon;
        public Color color;
        [Min(8f)] public float size;
    }

    [SerializeField] private List<Entry> entries = new();

    public bool TryGetStyle(MapMarkerType type, out Entry style)
    {
        foreach (Entry entry in entries)
        {
            if (entry.type == type)
            {
                style = entry;
                return true;
            }
        }
        style = default;
        return false;
    }
}
