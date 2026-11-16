using UnityEngine;

public class WaterParticleRig : MonoBehaviour
{
    [SerializeField] private ParticleSystem targetParticleSystem;
    [SerializeField] private GameObject targetParticleCamera;

    private PlayerWaterParticle boundPlayer;

    private void OnEnable()
    {
        boundPlayer = FindFirstObjectByType<PlayerWaterParticle>();
        if (boundPlayer != null)
            boundPlayer.BindWaterRig(targetParticleSystem, targetParticleCamera);
    }

    private void OnDisable()
    {
        if (boundPlayer != null)
        {
            boundPlayer.UnbindWaterRig(targetParticleSystem);
            boundPlayer = null;
        }
    }
}
