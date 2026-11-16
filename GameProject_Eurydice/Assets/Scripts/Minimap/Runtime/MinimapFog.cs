using System;
using UnityEngine;

public sealed class MinimapFog : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public Texture2D Texture { get; }
    public bool IsDirty { get; private set; }

    private readonly Color32[] pixels;
    private bool disposed;

    public MinimapFog(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        Texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        pixels = new Color32[Width * Height];
        Color32 opaque = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = opaque;
        Apply(false);
    }

    public void ClearDirty() => IsDirty = false;

    public bool RevealWorld(Vector2 tileMin, Vector2 tileSize, Vector2 worldXZ, float revealRadius, float innerPercent)
    {
        if (disposed || tileSize.x <= 0.001f || tileSize.y <= 0.001f || revealRadius <= 0.001f)
            return false;

        Vector2 tileMax = tileMin + tileSize;
        if (worldXZ.x + revealRadius < tileMin.x || worldXZ.x - revealRadius > tileMax.x ||
            worldXZ.y + revealRadius < tileMin.y || worldXZ.y - revealRadius > tileMax.y)
            return false;

        float pxX = Width / tileSize.x;
        float pxY = Height / tileSize.y;
        int minX = Mathf.Max(0, Mathf.FloorToInt((worldXZ.x - revealRadius - tileMin.x) * pxX));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt((worldXZ.x + revealRadius - tileMin.x) * pxX));
        int minY = Mathf.Max(0, Mathf.FloorToInt((worldXZ.y - revealRadius - tileMin.y) * pxY));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt((worldXZ.y + revealRadius - tileMin.y) * pxY));

        float radiusSq = revealRadius * revealRadius;
        float innerRadius = revealRadius * Mathf.Clamp01(innerPercent);
        float innerSq = innerRadius * innerRadius;
        float feather = Mathf.Max(0.0001f, revealRadius - innerRadius);
        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            float dz = tileMin.y + (y + 0.5f) / Height * tileSize.y - worldXZ.y;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = tileMin.x + (x + 0.5f) / Width * tileSize.x - worldXZ.x;
                float distSq = dx * dx + dz * dz;
                if (distSq >= radiusSq) continue;

                byte targetAlpha = distSq > innerSq
                    ? (byte)Mathf.RoundToInt(Mathf.Clamp01((Mathf.Sqrt(distSq) - innerRadius) / feather) * 255f)
                    : (byte)0;

                int idx = y * Width + x;
                if (pixels[idx].a > targetAlpha)
                {
                    pixels[idx].a = targetAlpha;
                    changed = true;
                }
            }
        }

        if (changed) Apply(true);
        return changed;
    }

    public bool IsWorldRevealed(Vector2 tileMin, Vector2 tileSize, Vector2 worldXZ, byte alphaThreshold = 200)
    {
        if (disposed || tileSize.x <= 0.001f || tileSize.y <= 0.001f) return false;
        int px = Mathf.Clamp(Mathf.FloorToInt((worldXZ.x - tileMin.x) / tileSize.x * Width), 0, Width - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt((worldXZ.y - tileMin.y) / tileSize.y * Height), 0, Height - 1);
        return pixels[py * Width + px].a <= alphaThreshold;
    }

    public byte[] GetAlpha()
    {
        byte[] result = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) result[i] = pixels[i].a;
        return result;
    }

    public void SetAlpha(byte[] data)
    {
        if (disposed || data == null || data.Length != pixels.Length) return;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, data[i]);
        Apply(false);
    }

    private void Apply(bool dirty)
    {
        Texture.SetPixels32(pixels);
        Texture.Apply(false);
        IsDirty = dirty;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (Texture != null) UnityEngine.Object.Destroy(Texture);
    }
}
