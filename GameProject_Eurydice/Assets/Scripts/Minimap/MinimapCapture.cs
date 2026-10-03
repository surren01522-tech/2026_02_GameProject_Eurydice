using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class MinimapCapture : IDisposable
{
    public RenderTexture Texture { get; private set; }

    private Camera captureCamera;

    public MinimapCapture(int resolution, Color clearColor, LayerMask cullingMask)
    {
        Texture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32);
        Texture.Create();

        GameObject cameraObj = new GameObject("MinimapCaptureCamera");
        captureCamera = cameraObj.AddComponent<Camera>();
        captureCamera.enabled = false;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = clearColor;
        captureCamera.cullingMask = cullingMask;
        captureCamera.orthographic = true;
        captureCamera.targetTexture = Texture;
        captureCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void Render(Vector2 center, Vector2 size, float maxWorldY, bool useCutPlane, float cutY)
    {
        if (captureCamera == null) return;

        float cameraY = maxWorldY + 100f;
        captureCamera.orthographicSize = Mathf.Max(size.x, size.y) * 0.5f;
        captureCamera.transform.position = new Vector3(center.x, cameraY, center.y);

        if (useCutPlane)
        {
            captureCamera.nearClipPlane = Mathf.Max(0.1f, cameraY - cutY);
        }
        else
        {
            captureCamera.nearClipPlane = 0.1f;
        }

        captureCamera.farClipPlane = cameraY + 200f;
        captureCamera.Render();
    }

    public void Dispose()
    {
        if (Texture != null)
        {
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }

        if (captureCamera != null)
        {
            Object.Destroy(captureCamera.gameObject);
            captureCamera = null;
        }
    }

    public static bool TryCalculateBounds(Transform mapRoot, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        if (mapRoot != null)
        {
            Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
        }
        else if (Terrain.activeTerrains != null && Terrain.activeTerrains.Length > 0)
        {
            for (int i = 0; i < Terrain.activeTerrains.Length; i++)
            {
                Terrain t = Terrain.activeTerrains[i];
                Vector3 size = t.terrainData.size;
                Bounds terrainBounds = new Bounds(t.transform.position + size * 0.5f, size);

                if (!hasBounds)
                {
                    bounds = terrainBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(terrainBounds);
                }
            }
        }
        else
        {
            Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger) continue;

                if (!hasBounds)
                {
                    bounds = colliders[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }
        }

        return hasBounds;
    }
}
