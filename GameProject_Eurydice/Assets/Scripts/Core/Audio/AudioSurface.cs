using UnityEngine;

public class AudioSurface : MonoBehaviour
{
    public enum SurfaceType
    {
        Default,
        Grass,
        Dirt,
        Stone,
        Wood,
        Water
    }

    [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;

    public SurfaceType Type => surfaceType;
}
