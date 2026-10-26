using System;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class SceneEnvironment : MonoBehaviour
{
    private const float InactivePriorityOffset = 1000f;

    [Serializable]
    public struct LightBinding
    {
        public Light light;
        public bool defaultActive;
    }

    [Serializable]
    public struct VolumeBinding
    {
        public Volume volume;
        public float originalWeight;
        public float originalPriority;
    }

    [FoldGroup("스카이박스 및 환경광")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private AmbientMode ambientMode = AmbientMode.Skybox;
    [SerializeField] private Color ambientSkyColor = new Color(0.212f, 0.227f, 0.259f, 1f);
    [SerializeField] private Color ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
    [SerializeField] private Color ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);
    [SerializeField] private float ambientIntensity = 1f;
    [SerializeField] private float[] bakedAmbientSH = new float[27];
    [SerializeField] private Color subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);

    [FoldGroup("환경 반사")]
    [SerializeField] private DefaultReflectionMode defaultReflectionMode = DefaultReflectionMode.Skybox;
    [SerializeField] private Texture customReflectionTexture;
    [SerializeField] private float reflectionIntensity = 1f;
    [SerializeField] private int reflectionBounces = 1;

    [FoldGroup("기본 안개 (RenderSettings)")]
    [SerializeField] private bool fogEnabled;
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
    [SerializeField] private Color fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float fogDensity = 0.01f;
    [SerializeField] private float fogStartDistance;
    [SerializeField] private float fogEndDistance = 300f;

    [FoldGroup("씬 조명 및 볼륨 바인딩")]
    [SerializeField] private LightBinding[] directionalLights = Array.Empty<LightBinding>();
    [SerializeField] private VolumeBinding[] volumes = Array.Empty<VolumeBinding>();
    [SerializeField] private bool isCaptured;

    private SphericalHarmonicsL2 cachedProbe;
    private bool probeInitialized;

    public string SceneName => gameObject.scene.name;
    public bool IsCaptured => isCaptured;

    private void Awake()
    {
        RebuildCachedProbe();
    }

    private void OnEnable()
    {
        RebuildCachedProbe();
        SceneEnvironmentManager.Register(this);
    }

    private void OnDisable()
    {
        SceneEnvironmentManager.Unregister(this);
    }

    public void RebuildCachedProbe()
    {
        cachedProbe = default;
        if (bakedAmbientSH != null && bakedAmbientSH.Length == 27)
        {
            for (int rgb = 0; rgb < 3; rgb++)
            {
                for (int c = 0; c < 9; c++)
                    cachedProbe[rgb, c] = bakedAmbientSH[rgb * 9 + c];
            }
            probeInitialized = true;
        }
        else
        {
            probeInitialized = false;
        }
    }

    public void ApplyRenderSettings()
    {
        if (!probeInitialized)
            RebuildCachedProbe();

        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.subtractiveShadowColor = subtractiveShadowColor;

        RenderSettings.fog = fogEnabled;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;

        // Skybox 환경광은 런타임 DynamicGI 재계산 스파이크를 피하기 위해 직렬화된 SH 계수를 Custom 모드로 즉시 주입
        bool useCustomProbe = (ambientMode == AmbientMode.Skybox || ambientMode == AmbientMode.Custom) && probeInitialized;
        RenderSettings.ambientMode = useCustomProbe ? AmbientMode.Custom : ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = ambientIntensity;
        if (useCustomProbe)
            RenderSettings.ambientProbe = cachedProbe;

        RenderSettings.defaultReflectionMode = defaultReflectionMode;
        RenderSettings.customReflectionTexture = customReflectionTexture;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.reflectionBounces = reflectionBounces;
    }

    public void ApplyLights(bool sceneActive)
    {
        if (directionalLights == null || directionalLights.Length == 0) return;

        string savedLight = Application.isPlaying ? SaveManager.GetActiveLight(SceneName) : null;
        bool hasSavedMatch = false;
        if (!string.IsNullOrEmpty(savedLight))
        {
            for (int i = 0; i < directionalLights.Length; i++)
            {
                var l = directionalLights[i].light;
                if (l != null && l.name == savedLight)
                {
                    hasSavedMatch = true;
                    break;
                }
            }
        }

        Light activeSun = null;
        for (int i = 0; i < directionalLights.Length; i++)
        {
            var binding = directionalLights[i];
            if (binding.light == null) continue;

            if (!sceneActive)
            {
                binding.light.enabled = false;
                continue;
            }

            bool shouldEnable;
            if (hasSavedMatch)
            {
                shouldEnable = binding.light.name == savedLight;
                if (directionalLights.Length > 1 && binding.light.gameObject.activeSelf != shouldEnable)
                    binding.light.gameObject.SetActive(shouldEnable);
            }
            else if (directionalLights.Length == 1)
            {
                shouldEnable = binding.defaultActive;
                if (shouldEnable && !binding.light.gameObject.activeSelf)
                    binding.light.gameObject.SetActive(true);
            }
            else
            {
                shouldEnable = binding.light.gameObject.activeSelf;
            }

            binding.light.enabled = shouldEnable;
            if (shouldEnable && activeSun == null)
                activeSun = binding.light;
        }

        if (sceneActive)
            RenderSettings.sun = activeSun;
    }

    public void SetVolumeRole(bool isPrimaryZone)
    {
        if (volumes == null) return;

        for (int i = 0; i < volumes.Length; i++)
        {
            var b = volumes[i];
            if (b.volume == null) continue;

            float targetPriority = isPrimaryZone ? b.originalPriority : b.originalPriority + InactivePriorityOffset;
            if (!Mathf.Approximately(b.volume.priority, targetPriority))
            {
                b.volume.priority = targetPriority;
                if (b.volume.isActiveAndEnabled)
                {
                    VolumeManager.instance.Unregister(b.volume);
                    VolumeManager.instance.Register(b.volume);
                }
            }

            b.volume.weight = isPrimaryZone ? b.originalWeight : 0f;
        }
    }

    public void SetVolumeBlendWeight(float factor)
    {
        if (volumes == null) return;
        factor = Mathf.Clamp01(factor);

        for (int i = 0; i < volumes.Length; i++)
        {
            var b = volumes[i];
            if (b.volume != null)
                b.volume.weight = b.originalWeight * factor;
        }
    }

#if UNITY_EDITOR
    public void CaptureFromActiveSceneInEditor()
    {
        var scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded || UnityEngine.SceneManagement.SceneManager.GetActiveScene() != scene)
        {
            Debug.LogError("[SceneEnvironment] 캡처 실패: 먼저 해당 씬을 Active Scene으로 설정하세요: " + scene.name);
            return;
        }

        DynamicGI.UpdateEnvironment();

        skyboxMaterial = RenderSettings.skybox;
        ambientMode = RenderSettings.ambientMode;
        ambientSkyColor = RenderSettings.ambientSkyColor;
        ambientEquatorColor = RenderSettings.ambientEquatorColor;
        ambientGroundColor = RenderSettings.ambientGroundColor;
        ambientIntensity = RenderSettings.ambientIntensity;
        subtractiveShadowColor = RenderSettings.subtractiveShadowColor;

        SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
        if (bakedAmbientSH == null || bakedAmbientSH.Length != 27)
            bakedAmbientSH = new float[27];

        for (int rgb = 0; rgb < 3; rgb++)
        {
            for (int c = 0; c < 9; c++)
                bakedAmbientSH[rgb * 9 + c] = probe[rgb, c];
        }

        defaultReflectionMode = RenderSettings.defaultReflectionMode;
        customReflectionTexture = RenderSettings.customReflectionTexture;
        reflectionIntensity = RenderSettings.reflectionIntensity;
        reflectionBounces = RenderSettings.reflectionBounces;

        fogEnabled = RenderSettings.fog;
        fogMode = RenderSettings.fogMode;
        fogColor = RenderSettings.fogColor;
        fogDensity = RenderSettings.fogDensity;
        fogStartDistance = RenderSettings.fogStartDistance;
        fogEndDistance = RenderSettings.fogEndDistance;

        var roots = scene.GetRootGameObjects();
        var lightList = new System.Collections.Generic.List<LightBinding>();
        var volumeList = new System.Collections.Generic.List<VolumeBinding>();

        for (int i = 0; i < roots.Length; i++)
        {
            var foundLights = roots[i].GetComponentsInChildren<Light>(true);
            for (int j = 0; j < foundLights.Length; j++)
            {
                var l = foundLights[j];
                if (l.type != LightType.Directional) continue;
                lightList.Add(new LightBinding
                {
                    light = l,
                    defaultActive = l.enabled && l.gameObject.activeInHierarchy
                });
            }

            var foundVolumes = roots[i].GetComponentsInChildren<Volume>(true);
            for (int j = 0; j < foundVolumes.Length; j++)
            {
                var v = foundVolumes[j];
                float basePriority = v.priority >= InactivePriorityOffset ? v.priority - InactivePriorityOffset : v.priority;
                volumeList.Add(new VolumeBinding
                {
                    volume = v,
                    originalWeight = v.weight,
                    originalPriority = basePriority
                });
            }
        }

        if (lightList.Count == 1 && !lightList[0].defaultActive)
        {
            var b = lightList[0];
            b.defaultActive = true;
            lightList[0] = b;
        }

        directionalLights = lightList.ToArray();
        volumes = volumeList.ToArray();
        isCaptured = true;
        RebuildCachedProbe();

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
#endif
}
