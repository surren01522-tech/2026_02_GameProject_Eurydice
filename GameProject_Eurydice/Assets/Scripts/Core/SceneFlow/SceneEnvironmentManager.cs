using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SceneEnvironmentManager
{
    public readonly struct PortalRenderScope : IDisposable
    {
        private readonly int token;
        internal PortalRenderScope(int token) => this.token = token;
        public void Dispose() => ExitPortalRenderScope(token);
    }

    private struct RenderSettingsSnapshot
    {
        public Material skybox;
        public Light sun;
        public AmbientMode ambientMode;
        public Color ambientSkyColor;
        public Color ambientEquatorColor;
        public Color ambientGroundColor;
        public float ambientIntensity;
        public SphericalHarmonicsL2 ambientProbe;
        public Color subtractiveShadowColor;
        public DefaultReflectionMode defaultReflectionMode;
        public Texture customReflectionTexture;
        public float reflectionIntensity;
        public int reflectionBounces;
        public bool fog;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity;
        public float fogStartDistance;
        public float fogEndDistance;

        public static RenderSettingsSnapshot Capture()
        {
            AmbientMode mode = RenderSettings.ambientMode;
            var snapshot = new RenderSettingsSnapshot
            {
                skybox = RenderSettings.skybox,
                sun = RenderSettings.sun,
                ambientMode = mode,
                ambientSkyColor = RenderSettings.ambientSkyColor,
                ambientEquatorColor = RenderSettings.ambientEquatorColor,
                ambientGroundColor = RenderSettings.ambientGroundColor,
                ambientIntensity = RenderSettings.ambientIntensity,
                subtractiveShadowColor = RenderSettings.subtractiveShadowColor,
                defaultReflectionMode = RenderSettings.defaultReflectionMode,
                customReflectionTexture = RenderSettings.customReflectionTexture,
                reflectionIntensity = RenderSettings.reflectionIntensity,
                reflectionBounces = RenderSettings.reflectionBounces,
                fog = RenderSettings.fog,
                fogMode = RenderSettings.fogMode,
                fogColor = RenderSettings.fogColor,
                fogDensity = RenderSettings.fogDensity,
                fogStartDistance = RenderSettings.fogStartDistance,
                fogEndDistance = RenderSettings.fogEndDistance
            };

            if (mode == AmbientMode.Custom || mode == AmbientMode.Skybox)
                snapshot.ambientProbe = RenderSettings.ambientProbe;

            return snapshot;
        }

        public void Restore()
        {
            if (RenderSettings.skybox != skybox) RenderSettings.skybox = skybox;
            if (RenderSettings.sun != sun) RenderSettings.sun = sun;
            if (RenderSettings.subtractiveShadowColor != subtractiveShadowColor) RenderSettings.subtractiveShadowColor = subtractiveShadowColor;

            if (RenderSettings.ambientMode != ambientMode) RenderSettings.ambientMode = ambientMode;
            if (RenderSettings.ambientSkyColor != ambientSkyColor) RenderSettings.ambientSkyColor = ambientSkyColor;
            if (RenderSettings.ambientEquatorColor != ambientEquatorColor) RenderSettings.ambientEquatorColor = ambientEquatorColor;
            if (RenderSettings.ambientGroundColor != ambientGroundColor) RenderSettings.ambientGroundColor = ambientGroundColor;
            if (!Mathf.Approximately(RenderSettings.ambientIntensity, ambientIntensity)) RenderSettings.ambientIntensity = ambientIntensity;
            if ((ambientMode == AmbientMode.Custom || ambientMode == AmbientMode.Skybox) && !RenderSettings.ambientProbe.Equals(ambientProbe))
                RenderSettings.ambientProbe = ambientProbe;

            if (RenderSettings.defaultReflectionMode != defaultReflectionMode) RenderSettings.defaultReflectionMode = defaultReflectionMode;
            if (RenderSettings.customReflectionTexture != customReflectionTexture) RenderSettings.customReflectionTexture = customReflectionTexture;
            if (!Mathf.Approximately(RenderSettings.reflectionIntensity, reflectionIntensity)) RenderSettings.reflectionIntensity = reflectionIntensity;
            if (RenderSettings.reflectionBounces != reflectionBounces) RenderSettings.reflectionBounces = reflectionBounces;

            if (RenderSettings.fog != fog) RenderSettings.fog = fog;
            if (RenderSettings.fogMode != fogMode) RenderSettings.fogMode = fogMode;
            if (RenderSettings.fogColor != fogColor) RenderSettings.fogColor = fogColor;
            if (!Mathf.Approximately(RenderSettings.fogDensity, fogDensity)) RenderSettings.fogDensity = fogDensity;
            if (!Mathf.Approximately(RenderSettings.fogStartDistance, fogStartDistance)) RenderSettings.fogStartDistance = fogStartDistance;
            if (!Mathf.Approximately(RenderSettings.fogEndDistance, fogEndDistance)) RenderSettings.fogEndDistance = fogEndDistance;
        }
    }

    private static readonly Dictionary<string, SceneEnvironment> Environments = new();
    private static readonly HashSet<string> WarnedMissingScenes = new();

    private static string activeSceneName = string.Empty;
    private static string blendTargetSceneName = string.Empty;
    private static float blendFactor;

    private static int portalRenderDepth;
    private static int activeScopeToken;
    private static int tokenCounter;
    private static string scopedTargetSceneName = string.Empty;
    private static RenderSettingsSnapshot prePortalSnapshot;

    public static string ActiveSceneName => activeSceneName;
    public static event Action<string> OnActiveSceneChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Environments.Clear();
        WarnedMissingScenes.Clear();
        activeSceneName = string.Empty;
        blendTargetSceneName = string.Empty;
        OnActiveSceneChanged = null;
        blendFactor = 0f;
        portalRenderDepth = 0;
        activeScopeToken = 0;
        scopedTargetSceneName = string.Empty;

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public static void Register(SceneEnvironment env)
    {
        if (env == null) return;
        string sceneName = env.SceneName;
        if (string.IsNullOrEmpty(sceneName)) return;

        Environments[sceneName] = env;
        WarnedMissingScenes.Remove(sceneName);

        if (!Application.isPlaying) return;

        if (string.IsNullOrEmpty(activeSceneName))
            activeSceneName = SceneManager.GetActiveScene().name;

        bool isPrimary = sceneName == activeSceneName;
        if (isPrimary)
        {
            Scene envScene = env.gameObject.scene;
            if (envScene.IsValid() && envScene.isLoaded && SceneManager.GetActiveScene() != envScene)
                SceneManager.SetActiveScene(envScene);
        }

        env.SetVolumeRole(isPrimary);
        env.ApplyLights(isPrimary);

        if (isPrimary)
        {
            env.ApplyRenderSettings();
            AudioZone.RefreshActiveAudio();
        }
        else if (sceneName == blendTargetSceneName && blendFactor > 0f)
            env.SetVolumeBlendWeight(blendFactor);
    }

    public static void Unregister(SceneEnvironment env)
    {
        if (env == null) return;
        string sceneName = env.SceneName;
        if (string.IsNullOrEmpty(sceneName)) return;

        if (Environments.TryGetValue(sceneName, out var existing) && existing == env)
            Environments.Remove(sceneName);

        if (!Application.isPlaying) return;

        if (blendTargetSceneName == sceneName)
        {
            blendTargetSceneName = string.Empty;
            blendFactor = 0f;
        }

        if (activeSceneName == sceneName && portalRenderDepth == 0)
            FallbackActiveEnvironment(sceneName);
    }

    private static void OnSceneUnloaded(Scene unloaded)
    {
        if (!unloaded.IsValid() || string.IsNullOrEmpty(unloaded.name)) return;
        string sceneName = unloaded.name;

        Environments.Remove(sceneName);
        if (blendTargetSceneName == sceneName)
        {
            blendTargetSceneName = string.Empty;
            blendFactor = 0f;
        }

        if (activeSceneName == sceneName && portalRenderDepth == 0)
            FallbackActiveEnvironment(sceneName);
    }

    private static void FallbackActiveEnvironment(string removedScene)
    {
        string currentUnityActive = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentUnityActive) && currentUnityActive != removedScene && Environments.ContainsKey(currentUnityActive))
        {
            SwitchEnvironment(removedScene, currentUnityActive, saveScene: false);
            return;
        }

        foreach (var kvp in Environments)
        {
            if (kvp.Value != null)
            {
                SwitchEnvironment(removedScene, kvp.Key, saveScene: false);
                return;
            }
        }

        activeSceneName = string.Empty;
    }

    public static void InitializeCurrentScene(string sceneName)
    {
        if (!Application.isPlaying) return;
        if (string.IsNullOrEmpty(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        if (activeSceneName == sceneName && Environments.ContainsKey(sceneName))
            return;

        SwitchEnvironment(activeSceneName, sceneName, saveScene: false);
    }

    public static void SwitchEnvironment(string fromScene, string toScene, bool saveScene = true)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(toScene) || portalRenderDepth > 0) return;

        SetProximityBlend(null, 0f);
        activeSceneName = toScene;

        Scene targetS = SceneManager.GetSceneByName(toScene);
        if (targetS.IsValid() && targetS.isLoaded && SceneManager.GetActiveScene() != targetS)
            SceneManager.SetActiveScene(targetS);

        foreach (var kvp in Environments)
        {
            var env = kvp.Value;
            if (env == null) continue;

            bool isPrimary = kvp.Key == toScene;
            env.SetVolumeRole(isPrimary);
            env.ApplyLights(isPrimary);
            if (isPrimary)
                env.ApplyRenderSettings();
        }

        if (saveScene)
            SaveManager.SaveActiveScene(toScene);

        AudioZone.RefreshActiveAudio();
        OnActiveSceneChanged?.Invoke(toScene);
    }

    public static void ApplyActiveSceneAudio(bool includeBgm = true, bool includeAmbience = true)
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(activeSceneName)) return;
        if (!Environments.TryGetValue(activeSceneName, out var env) || env == null) return;

        if (includeBgm)
            AudioManager.Instance.FadeBGM(env.BgmClip, env.AudioFadeDuration);
        if (includeAmbience)
            AudioManager.Instance.PlayAmbience(env.AmbienceClip, env.AudioFadeDuration);
    }

    public static void RefreshSceneLights(string sceneName)
    {
        if (!Application.isPlaying || portalRenderDepth > 0 || string.IsNullOrEmpty(sceneName)) return;
        if (Environments.TryGetValue(sceneName, out var env) && env != null)
        {
            env.InvalidateSavedLightCache();
            env.ApplyLights(sceneName == activeSceneName);
        }
    }

    private static void SetSceneBlendWeight(string sceneName, float weight)
    {
        if (!string.IsNullOrEmpty(sceneName) && Environments.TryGetValue(sceneName, out var env) && env != null)
            env.SetVolumeBlendWeight(weight);
    }

    public static void SetProximityBlend(string targetScene, float t)
    {
        if (!Application.isPlaying || portalRenderDepth > 0) return;

        t = Mathf.Clamp01(t);
        if (string.IsNullOrEmpty(targetScene) || targetScene == activeSceneName || t <= 0.0001f)
        {
            if (!string.IsNullOrEmpty(blendTargetSceneName))
            {
                if (blendTargetSceneName != activeSceneName)
                    SetSceneBlendWeight(blendTargetSceneName, 0f);
                blendTargetSceneName = string.Empty;
                blendFactor = 0f;
            }
            return;
        }

        if (blendTargetSceneName != targetScene)
        {
            if (!string.IsNullOrEmpty(blendTargetSceneName) && blendTargetSceneName != activeSceneName)
                SetSceneBlendWeight(blendTargetSceneName, 0f);
            blendTargetSceneName = targetScene;
        }

        blendFactor = t;
        SetSceneBlendWeight(targetScene, t);
    }

    public static PortalRenderScope EnterPortalRenderScope(string targetSceneName, out bool environmentSwapped)
    {
        environmentSwapped = false;
        if (!Application.isPlaying || portalRenderDepth > 0 || string.IsNullOrEmpty(targetSceneName))
            return new PortalRenderScope(0);

        if (targetSceneName == activeSceneName)
        {
            if (blendFactor <= 0f || string.IsNullOrEmpty(blendTargetSceneName))
                return new PortalRenderScope(0);

            int sameSceneToken = NextValidToken();
            portalRenderDepth = 1;
            activeScopeToken = sameSceneToken;
            scopedTargetSceneName = targetSceneName;
            SetSceneBlendWeight(blendTargetSceneName, 0f);
            environmentSwapped = true;
            return new PortalRenderScope(sameSceneToken);
        }

        if (!Environments.TryGetValue(targetSceneName, out var targetEnv) || targetEnv == null)
        {
            Scene targetS = SceneManager.GetSceneByName(targetSceneName);
            if (targetS.IsValid() && targetS.isLoaded && WarnedMissingScenes.Add(targetSceneName))
            {
                Debug.LogWarning("[SceneEnvironmentManager] SceneEnvironment 컴포넌트가 등록되지 않았습니다: " + targetSceneName + " (Tools > Environment 메뉴에서 캡처 필요)");
            }
            return new PortalRenderScope(0);
        }

        int token = NextValidToken();
        portalRenderDepth = 1;
        activeScopeToken = token;
        scopedTargetSceneName = targetSceneName;
        prePortalSnapshot = RenderSettingsSnapshot.Capture();

        if (!string.IsNullOrEmpty(activeSceneName) && Environments.TryGetValue(activeSceneName, out var activeEnv) && activeEnv != null)
        {
            activeEnv.ApplyLights(false);
            activeEnv.SetVolumeBlendWeight(0f);
        }

        if (blendTargetSceneName != targetSceneName)
            SetSceneBlendWeight(blendTargetSceneName, 0f);

        targetEnv.ApplyRenderSettings();
        targetEnv.ApplyLights(true);
        targetEnv.SetVolumeBlendWeight(1f);

        environmentSwapped = true;
        return new PortalRenderScope(token);
    }

    private static void ExitPortalRenderScope(int token)
    {
        if (token == 0 || token != activeScopeToken) return;

        string targetName = scopedTargetSceneName;
        activeScopeToken = 0;
        portalRenderDepth = 0;
        scopedTargetSceneName = string.Empty;

        if (targetName == activeSceneName)
        {
            SetSceneBlendWeight(blendTargetSceneName, blendFactor);
            return;
        }

        prePortalSnapshot.Restore();

        if (!string.IsNullOrEmpty(targetName) && Environments.TryGetValue(targetName, out var targetEnv) && targetEnv != null)
        {
            targetEnv.ApplyLights(false);
            targetEnv.SetVolumeBlendWeight(targetName == blendTargetSceneName ? blendFactor : 0f);
        }

        if (!string.IsNullOrEmpty(activeSceneName) && Environments.TryGetValue(activeSceneName, out var activeEnv) && activeEnv != null)
        {
            activeEnv.ApplyLights(true);
            activeEnv.SetVolumeBlendWeight(1f);
        }

        if (blendTargetSceneName != activeSceneName && blendTargetSceneName != targetName)
            SetSceneBlendWeight(blendTargetSceneName, blendFactor);
    }

    private static int NextValidToken()
    {
        int t = ++tokenCounter;
        return t == 0 ? ++tokenCounter : t;
    }
}
