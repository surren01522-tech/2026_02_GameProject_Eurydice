using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MinimapBakeData", menuName = "Minimap Bake Data")]
public class MinimapBakeData : ScriptableObject
{
    [Header("Bake Settings")]
    public List<string> scenes = new List<string>();
    public LayerMask cullingMask = ~0;
    public int resolution = 2048;
    public float padding = 5f;
    public Color clearColor = new Color(0.12f, 0.12f, 0.15f, 1f);

    [Header("Bake Output")]
    public Texture2D mapTexture;
    public Vector2 worldCenter;
    public Vector2 worldSize = new Vector2(200f, 200f);
}
