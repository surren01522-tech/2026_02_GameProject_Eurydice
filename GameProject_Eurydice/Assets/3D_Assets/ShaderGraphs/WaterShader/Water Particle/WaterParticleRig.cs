using UnityEngine;

public class WaterParticleRig : MonoBehaviour
{
    [SerializeField] private ParticleSystem targetParticleSystem;
    [SerializeField] private GameObject targetParticleCamera;

    private PlayerWaterParticle boundPlayer;

    private void OnEnable()
    {
        SceneEnvironmentManager.OnActiveSceneChanged -= HandleActiveSceneChanged;
        SceneEnvironmentManager.OnActiveSceneChanged += HandleActiveSceneChanged;
        SyncActiveState();
    }

    private void OnDisable()
    {
        SceneEnvironmentManager.OnActiveSceneChanged -= HandleActiveSceneChanged;
        if (boundPlayer != null)
        {
            boundPlayer.UnbindWaterRig(targetParticleSystem);
            boundPlayer = null;
        }
    }

    private void HandleActiveSceneChanged(string _) => SyncActiveState();

    private void SyncActiveState()
    {
        bool isActive = SceneEnvironmentManager.IsSceneActive(gameObject.scene);

        if (targetParticleCamera != null && targetParticleCamera.activeSelf != isActive)
            targetParticleCamera.SetActive(isActive);

        if (isActive)
        {
            if (boundPlayer == null)
                boundPlayer = FindFirstObjectByType<PlayerWaterParticle>();
            if (boundPlayer != null)
                boundPlayer.BindWaterRig(targetParticleSystem, targetParticleCamera);
        }
        else if (boundPlayer != null)
        {
            boundPlayer.UnbindWaterRig(targetParticleSystem);
            boundPlayer = null;
        }
    }
}
