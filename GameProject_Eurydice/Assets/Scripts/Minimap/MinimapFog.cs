using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class MinimapFog : IDisposable
{
    public Texture2D Texture { get; private set; }
    public int Resolution { get; private set; }

    private readonly Color32[] colors;

    public MinimapFog(int resolution)
    {
        Resolution = resolution;
        colors = new Color32[resolution * resolution];
        Texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Clear();
    }

    public void Clear()
    {
        Color32 black = new Color32(0, 0, 0, 255);
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = black;
        }

        Apply();
    }

    public void Reveal(Vector2 normalizedPos, float radiusPx, float innerPercent)
    {
        int maxIndex = Resolution - 1;
        int centerX = Mathf.RoundToInt(normalizedPos.x * maxIndex);
        int centerY = Mathf.RoundToInt(normalizedPos.y * maxIndex);
        int radiusInt = Mathf.CeilToInt(radiusPx);
        float radiusSqr = radiusPx * radiusPx;

        float clampedInner = Mathf.Clamp01(innerPercent);
        float outerRange = Mathf.Max(0.001f, 1f - clampedInner);
        bool useFade = clampedInner < 0.999f;
        bool isModified = false;

        int startX = Mathf.Clamp(centerX - radiusInt, 0, maxIndex);
        int endX = Mathf.Clamp(centerX + radiusInt, 0, maxIndex);
        int startY = Mathf.Clamp(centerY - radiusInt, 0, maxIndex);
        int endY = Mathf.Clamp(centerY + radiusInt, 0, maxIndex);

        for (int y = startY; y <= endY; y++)
        {
            float dy = y - centerY;
            float dySqr = dy * dy;

            for (int x = startX; x <= endX; x++)
            {
                float dx = x - centerX;
                float distSqr = dx * dx + dySqr;

                if (distSqr > radiusSqr) continue;

                byte targetAlpha = 0;
                if (useFade)
                {
                    float normDist = Mathf.Sqrt(distSqr) / radiusPx;
                    if (normDist > clampedInner)
                    {
                        float fade = (normDist - clampedInner) / outerRange;
                        targetAlpha = (byte)(fade * fade * 255f);
                    }
                }

                int pixelIndex = y * Resolution + x;
                if (colors[pixelIndex].a > targetAlpha)
                {
                    colors[pixelIndex].a = targetAlpha;
                    isModified = true;
                }
            }
        }

        if (isModified)
        {
            Apply();
        }
    }

    public byte[] GetAlpha()
    {
        byte[] alphaBytes = new byte[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            alphaBytes[i] = colors[i].a;
        }
        return alphaBytes;
    }

    public void SetAlpha(byte[] alphaBytes)
    {
        if (alphaBytes == null || alphaBytes.Length != colors.Length) return;

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i].a = alphaBytes[i];
        }

        Apply();
    }

    public void Dispose()
    {
        if (Texture != null)
        {
            Object.Destroy(Texture);
            Texture = null;
        }
    }

    private void Apply()
    {
        if (Texture != null)
        {
            Texture.SetPixels32(colors);
            Texture.Apply(false);
        }
    }
}
