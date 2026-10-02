using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [Header("오디오 믹서")]
    [SerializeField] private AudioMixer audioMixer;

    public GameSettingsData CurrentSettings { get; private set; }

    public event Action OnBindingsChanged;
    private InputActionRebindingExtensions.RebindingOperation rebindOp;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        CurrentSettings = SaveManager.LoadSettings();
    }

    private void Start() => ApplyAll();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        rebindOp?.Dispose();
    }

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphic();
        ApplyCamera();
    }

    public void ApplyAudio()
    {
        bool mute = CurrentSettings.masterMute;
        SetMixerVolume("Master", mute ? 0f : CurrentSettings.masterVolume);
        SetMixerVolume("BGM", (mute || CurrentSettings.bgmMute) ? 0f : CurrentSettings.bgmVolume);
        SetMixerVolume("Ambience", (mute || CurrentSettings.bgmMute) ? 0f : CurrentSettings.bgmVolume);
        SetMixerVolume("SFX", (mute || CurrentSettings.sfxMute) ? 0f : CurrentSettings.sfxVolume);
        SetMixerVolume("UI", (mute || CurrentSettings.uiMute) ? 0f : CurrentSettings.uiVolume);
    }

    public void ApplyGraphic()
    {
        Screen.SetResolution(CurrentSettings.resolutionWidth, CurrentSettings.resolutionHeight, CurrentSettings.fullScreenMode);
        QualitySettings.SetQualityLevel(CurrentSettings.qualityLevel, true);
        QualitySettings.vSyncCount = CurrentSettings.vSyncCount;
        Application.targetFrameRate = CurrentSettings.targetFrameRate;
    }

    public void ApplyCamera()
    {
        var camSens = FindFirstObjectByType<CameraSensitivity>();
        camSens?.SetSens(CurrentSettings.mouseSensitivity, CurrentSettings.invertY);
    }

    public void SetMasterVol(float val) { CurrentSettings.masterVolume = val; ApplyAudio(); Save(); }
    public void SetMasterMute(bool mute) { CurrentSettings.masterMute = mute; ApplyAudio(); Save(); }
    public void SetBGMVol(float val) { CurrentSettings.bgmVolume = val; ApplyAudio(); Save(); }
    public void SetBGMMute(bool mute) { CurrentSettings.bgmMute = mute; ApplyAudio(); Save(); }
    public void SetSFXVol(float val) { CurrentSettings.sfxVolume = val; ApplyAudio(); Save(); }
    public void SetSFXMute(bool mute) { CurrentSettings.sfxMute = mute; ApplyAudio(); Save(); }
    public void SetUIVol(float val) { CurrentSettings.uiVolume = val; ApplyAudio(); Save(); }
    public void SetUIMute(bool mute) { CurrentSettings.uiMute = mute; ApplyAudio(); Save(); }

    public void SetRes(int width, int height, FullScreenMode? mode = null)
    {
        CurrentSettings.resolutionWidth = width;
        CurrentSettings.resolutionHeight = height;
        if (mode.HasValue) CurrentSettings.fullScreenMode = mode.Value;
        ApplyGraphic();
        Save();
    }

    public void SetFullScreen(FullScreenMode mode)
    {
        CurrentSettings.fullScreenMode = mode;
        ApplyGraphic();
        Save();
    }

    public void SetQuality(int level)
    {
        CurrentSettings.qualityLevel = level;
        ApplyGraphic();
        Save();
    }

    public void SetVSync(int count)
    {
        CurrentSettings.vSyncCount = count;
        ApplyGraphic();
        Save();
    }

    public void SetFPS(int fps)
    {
        CurrentSettings.targetFrameRate = fps;
        ApplyGraphic();
        Save();
    }

    public void SetSens(float val)
    {
        CurrentSettings.mouseSensitivity = val;
        ApplyCamera();
        Save();
    }

    public void SetInvertY(bool invert)
    {
        CurrentSettings.invertY = invert;
        ApplyCamera();
        Save();
    }

    public void ApplyBindings(InputActionAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(CurrentSettings.keyBindingsJson)) return;
        asset.LoadBindingOverridesFromJson(CurrentSettings.keyBindingsJson);
    }

    public void SaveBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        CurrentSettings.keyBindingsJson = asset.SaveBindingOverridesAsJson();
        Save();
        OnBindingsChanged?.Invoke();
    }

    public void ResetBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        asset.RemoveAllBindingOverrides();
        SaveBindings(asset);
    }

    public void Rebind(InputAction action, int bindingIndex, Action onComplete = null, Action onCancel = null)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return;
        if (action.bindings[bindingIndex].isComposite)
        {
            Debug.LogWarning($"[SettingManager] Cannot rebind composite header '{action.name}' at index {bindingIndex}.");
            return;
        }

        action.Disable();
        rebindOp?.Cancel();

        rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                action.Enable();
                op.Dispose();
                rebindOp = null;
                SaveBindings(action.actionMap?.asset);
                onComplete?.Invoke();
            })
            .OnCancel(op =>
            {
                action.Enable();
                op.Dispose();
                rebindOp = null;
                onCancel?.Invoke();
            });

        rebindOp.Start();
    }

    public void Save() => SaveManager.SaveSettings(CurrentSettings);

    private void SetMixerVolume(string paramName, float linearVolume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMixerVolume(paramName, linearVolume);
            return;
        }

        if (audioMixer != null)
        {
            float dB = linearVolume <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat(paramName, dB);
        }
    }
}