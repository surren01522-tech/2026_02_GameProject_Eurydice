using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum PortalState
{
    Closed,
    Opening,
    Open,
    Closing
}

[ExecuteAlways]
[DefaultExecutionOrder(-10)]
public class PortalGateway : MonoBehaviour
{
    private const float PortalCamNearClip = 0.01f;
    private const float ProximityStartDist = 1.5f;
    private const float ProximityFullDist = 0.05f;
    private static readonly Quaternion HalfTurn = Quaternion.Euler(0f, 180f, 0f);

    private static readonly int PortalTexId = Shader.PropertyToID("_PortalTex");
    private static readonly int HasPortalTexId = Shader.PropertyToID("_HasPortalTex");
    private static readonly int OpenProgressId = Shader.PropertyToID("_OpenProgress");
    private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
    private static readonly int LensCurvatureId = Shader.PropertyToID("_LensCurvature");
    private static readonly int InnerGlowColorId = Shader.PropertyToID("_InnerGlowColor");
    private static readonly int TargetBrightnessId = Shader.PropertyToID("_TargetBrightness");

    [FoldGroup("포탈 연결 및 심리스 설정")]
    [SerializeField] private string portalId;
    [SerializeField] private string targetPortalId;
    [SerializeField] private string targetSceneName;
    [SerializeField] private PortalGateway linkedPortal;
    [SerializeField] private bool startOpen;
    [SerializeField] private bool syncStateWithLinked = true;
    [SerializeField] private bool mirrorForwardDirection = true;
    [SerializeField] private Renderer portalRenderer;
    [SerializeField] private Shader portalShader;

    [FoldGroup("개폐 연출 설정")]
    [SerializeField] private float openDuration = 1.2f;
    [SerializeField] private float closeDuration = 0.6f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool animateTransformScale;
    [SerializeField] private ParticleSystem activationParticle;
    [SerializeField] private Light portalLight;
    [SerializeField] private float maxLightIntensity = 3f;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip teleportSound;
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewProgress = 1f;

    [FoldGroup("기믹 연동 (소켓 ID)")]
    [SerializeField] private List<string> requiredSocketIds = new();
    [SerializeField] private float socketActivationDelay = 0.25f;
    [SerializeField] private UnityEvent onPortalOpened;
    [SerializeField] private UnityEvent onPortalClosed;
    [SerializeField] private UnityEvent onPlayerTeleported;

    [FoldGroup("렌더링 및 텔레포트 설정")]
    [Range(0.25f, 1f)]
    [SerializeField] private float renderTextureScale = 0.85f;
    [SerializeField] private float maxRenderDistance = 60f;
    [SerializeField] private float shadowRenderDistance = 50f;
    [SerializeField] private LayerMask cullingMask = ~0;
    [SerializeField] private bool useObliqueClip = true;
    [SerializeField] private float clipPlaneOffset = 0.02f;
    [SerializeField] private float exitClearance = 0.06f;
    [SerializeField] private float teleportCooldown = 0.2f;
    [SerializeField] private Key debugToggleKey = Key.None;

    public string PortalId => portalId;
    public string TargetPortalId => targetPortalId;
    public PortalState CurrentState { get; private set; } = PortalState.Closed;
    public bool IsOpen => CurrentState == PortalState.Open;
    public PortalGateway LinkedPortal => linkedPortal;
    public string DestinationSceneName => linkedPortal != null ? linkedPortal.gameObject.scene.name : targetSceneName;
    public Vector3 ApertureSize => defaultScale;
    public bool MirrorForward => mirrorForwardDirection;
    public Renderer PortalRenderer => portalRenderer;
    public Camera PortalCamera => portalCamera;
    public float MaxRenderDistance => maxRenderDistance;
    public float ExitClearance => exitClearance;
    public float TeleportCooldown => teleportCooldown;
    public float LastTeleportTime => lastTeleportTime;

    private Camera portalCamera;
    private UniversalAdditionalCameraData cameraData;
    private RenderTexture renderTex;
    private Material runtimeMat;
    private Coroutine transitionRoutine;
    private Vector3 defaultScale = new Vector3(2.2f, 3.0f, 1f);
    private float currentProgress = 1f;
    private float lastTeleportTime = -10f;
    private bool isScenePinned;

    private bool hasBaseMatProps;
    private float baseRipple;
    private float baseCurvature;
    private Color baseInnerGlow;
    private float baseBrightness = 1f;
    private float lastFlattenT = -1f;

    public void SetLastTeleportTime(float time) => lastTeleportTime = time;
    public void SetLinkedPortal(PortalGateway target) => linkedPortal = target;

    public static Vector3 MapLocal(Vector3 localPos, bool mirror) => mirror ? HalfTurn * localPos : localPos;

    public static Quaternion GetRotationDelta(PortalGateway from, PortalGateway to, bool mirror)
    {
        Quaternion turn = mirror ? HalfTurn : Quaternion.identity;
        return to.transform.rotation * turn * Quaternion.Inverse(from.transform.rotation);
    }

    private void Reset() => SetupDefaultQuadPortal();

    private void Awake()
    {
        EnsureSetup();
        if (portalRenderer != null && portalRenderer.transform.localScale.sqrMagnitude > 0.001f)
            defaultScale = portalRenderer.transform.localScale;
    }

    public static void SwitchZoneEnvironment(PortalGateway from, PortalGateway to)
    {
        if (from == null || to == null) return;
        SceneEnvironmentManager.SwitchEnvironment(from.gameObject.scene.name, to.gameObject.scene.name);
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        PortalManager.Register(this);
        ItemSocket.OnAnySocketPlaced += OnSocketPlaced;

        if (string.IsNullOrEmpty(SceneEnvironmentManager.ActiveSceneName))
            SceneEnvironmentManager.InitializeCurrentScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        bool savedOpen = SaveManager.IsPortalOpenAnyScene(portalId) ||
                         (syncStateWithLinked && SaveManager.IsPortalOpenAnyScene(targetPortalId));
        bool socketsMet = HasRequiredSockets() && AreSocketsMet();
        bool linkedOpen = syncStateWithLinked && linkedPortal != null &&
                          (linkedPortal.CurrentState == PortalState.Open || linkedPortal.CurrentState == PortalState.Opening);

        SetOpenImmediate(startOpen || savedOpen || socketsMet || linkedOpen, syncLinked: false, saveState: socketsMet && !savedOpen);
    }

    private void OnEnable()
    {
        PortalManager.Register(this);
        EnsureSetup();

        if (!Application.isPlaying)
        {
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null) portalRenderer.enabled = true;
            RefreshVisuals();
        }
    }

    private void OnDisable()
    {
        if (PortalCameraController.IsSplitActive && (PortalCameraController.SplitEntry == this || PortalCameraController.SplitExit == this))
            PortalCameraController.CompleteSplit(Camera.main);

        PortalManager.Unregister(this);
        PinTargetScene(false);
        ReleaseCamera();
    }

    private void OnDestroy()
    {
        ItemSocket.OnAnySocketPlaced -= OnSocketPlaced;
        ReleaseCamera();

        if (runtimeMat != null)
        {
            if (Application.isPlaying) Destroy(runtimeMat);
            else DestroyImmediate(runtimeMat);
            runtimeMat = null;
        }
    }

    private void OnValidate()
    {
        if (portalRenderer == null)
            portalRenderer = GetComponentInChildren<Renderer>();

        if (portalShader == null)
            portalShader = Shader.Find("Shader Graphs/PortalGateway");

        if (!Application.isPlaying)
        {
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null) portalRenderer.enabled = true;
        }
        RefreshVisuals();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (linkedPortal != null && linkedPortal.linkedPortal == null)
                linkedPortal.linkedPortal = this;
            return;
        }

        if (debugToggleKey != Key.None && Keyboard.current != null && Keyboard.current[debugToggleKey].wasPressedThisFrame)
            Toggle();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            PortalManager.UpdateManager();
    }

    public void Open() => SetOpen(true, syncStateWithLinked);
    public void Close() => SetOpen(false, syncStateWithLinked);

    public void Toggle()
    {
        if (CurrentState == PortalState.Open || CurrentState == PortalState.Opening) Close();
        else Open();
    }

    public void SetOpenImmediate(bool open, bool syncLinked = true, bool saveState = true)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        PinTargetScene(open);

        CurrentState = open ? PortalState.Open : PortalState.Closed;
        currentProgress = open ? 1f : 0f;

        if (portalRenderer != null)
        {
            portalRenderer.enabled = open;
            if (animateTransformScale)
                portalRenderer.transform.localScale = open ? defaultScale : Vector3.zero;
        }

        if (saveState)
            SaveOpenState(open);

        RefreshVisuals();

        if (syncLinked && linkedPortal != null && linkedPortal.IsOpen != open)
            linkedPortal.SetOpenImmediate(open, syncLinked: false, saveState: saveState);
    }

    private void SetOpen(bool open, bool syncLinked)
    {
        if (!Application.isPlaying)
        {
            startOpen = open;
            editorPreviewProgress = open ? 1f : 0f;
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null) portalRenderer.enabled = true;
            RefreshVisuals();
            return;
        }

        if (open && (CurrentState == PortalState.Open || CurrentState == PortalState.Opening)) return;
        if (!open && (CurrentState == PortalState.Closed || CurrentState == PortalState.Closing)) return;

        if (open) PinTargetScene(true);
        SaveOpenState(open);

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(AnimateOpenRoutine(open));

        if (syncLinked && linkedPortal != null)
            linkedPortal.SetOpen(open, syncLinked: false);
    }

    private IEnumerator AnimateOpenRoutine(bool opening)
    {
        CurrentState = opening ? PortalState.Opening : PortalState.Closing;

        if (opening)
        {
            if (portalRenderer != null) portalRenderer.enabled = true;
            if (activationParticle != null) activationParticle.Play();
            if (openSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXAt(openSound, transform.position);
        }
        else if (closeSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAt(closeSound, transform.position);
        }

        float duration = Mathf.Max(0.05f, opening ? openDuration : closeDuration);
        float startVal = currentProgress;
        float targetVal = opening ? 1f : 0f;
        float elapsed = 0f;
        AnimationCurve curve = opening ? openCurve : closeCurve;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            currentProgress = Mathf.Lerp(startVal, targetVal, curvedT);

            if (opening && currentProgress > 0.35f && !string.IsNullOrEmpty(targetSceneName) &&
                SceneStreamer.Instance != null && !SceneStreamer.Instance.IsSceneLoaded(targetSceneName))
            {
                currentProgress = 0.35f;
                elapsed = duration * 0.35f;
            }

            if (animateTransformScale && portalRenderer != null)
                portalRenderer.transform.localScale = defaultScale * currentProgress;

            RefreshVisuals();
            yield return null;
        }

        if (opening && syncStateWithLinked && linkedPortal != null && !linkedPortal.IsOpen)
            linkedPortal.SetOpenImmediate(true, syncLinked: false, saveState: false);

        if (!opening)
            PinTargetScene(false);

        currentProgress = targetVal;
        CurrentState = opening ? PortalState.Open : PortalState.Closed;

        if (portalRenderer != null)
        {
            portalRenderer.enabled = opening;
            if (animateTransformScale)
                portalRenderer.transform.localScale = opening ? defaultScale : Vector3.zero;
        }

        RefreshVisuals();
        transitionRoutine = null;

        if (opening) onPortalOpened?.Invoke();
        else onPortalClosed?.Invoke();
    }

    public void RenderPortalView(Camera cam, bool isSplitEntry)
    {
        if (currentProgress <= 0.001f || linkedPortal == null || portalRenderer == null || !portalRenderer.enabled) return;

        Transform inT = transform;
        Transform outT = linkedPortal.transform;
        Vector3 localCamPos = inT.InverseTransformPoint(cam.transform.position);
        float viewSide = isSplitEntry ? PortalCameraController.SplitEntrySide : (localCamPos.z >= 0f ? 1f : -1f);
        float sqrDist = (cam.transform.position - inT.position).sqrMagnitude;

        EnsureCamera(cam);
        if (portalCamera == null || renderTex == null) return;

        Vector3 mappedCamPos = outT.TransformPoint(MapLocal(localCamPos, mirrorForwardDirection));
        Quaternion mappedCamRot = GetRotationDelta(this, linkedPortal, mirrorForwardDirection) * cam.transform.rotation;

        if (cameraData != null)
            cameraData.renderShadows = isSplitEntry || sqrDist < shadowRenderDistance * shadowRenderDistance;

        portalCamera.nearClipPlane = PortalCamNearClip;
        portalCamera.farClipPlane = cam.farClipPlane;
        portalCamera.cullingMask = cullingMask;
        portalCamera.targetTexture = renderTex;
        portalCamera.transform.SetPositionAndRotation(mappedCamPos, mappedCamRot);
        portalCamera.fieldOfView = cam.fieldOfView;
        portalCamera.aspect = cam.aspect;
        portalCamera.ResetProjectionMatrix();

        if (useObliqueClip && localCamPos.z * viewSide > 0.04f)
        {
            Vector3 clipNormalWorld = (mirrorForwardDirection ? viewSide : -viewSide) * outT.forward;
            Vector3 camSpacePos = portalCamera.worldToCameraMatrix.MultiplyPoint(outT.position);
            Vector3 camSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector(clipNormalWorld);
            float offset = Mathf.Max(0.02f, clipPlaneOffset);
            float camSpaceDist = -Vector3.Dot(camSpacePos, camSpaceNormal) - offset;

            if (camSpaceDist < -PortalCamNearClip * 2f)
            {
                Vector4 clipPlane = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDist);
                portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlane);
            }
        }

        PortalManager.HideAllPortalRenderers();
        try
        {
            using (SceneEnvironmentManager.EnterPortalRenderScope(DestinationSceneName, out _))
            {
                RenderPipeline.SubmitRenderRequest(portalCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = renderTex });
            }
        }
        finally
        {
            PortalManager.RestorePortalRenderers();
        }

        RefreshVisuals();
    }

    public float EvaluateCameraProximity(Transform camTransform, bool isSplitEntry)
    {
        if (camTransform == null || currentProgress <= 0.001f) return 0f;

        Vector3 localCam = transform.InverseTransformPoint(camTransform.position);
        float dz = Mathf.Abs(localCam.z);

        if (isSplitEntry)
            return 1f - Mathf.InverseLerp(ProximityFullDist, ProximityStartDist, dz);

        float halfW = defaultScale.x * 0.5f;
        float halfH = defaultScale.y * 0.5f;
        float dx = Mathf.Max(0f, Mathf.Abs(localCam.x) - halfW);
        float dy = Mathf.Max(0f, Mathf.Abs(localCam.y) - halfH);
        float apertureDist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        if (apertureDist >= ProximityStartDist) return 0f;

        float rawT = 1f - Mathf.InverseLerp(ProximityFullDist, ProximityStartDist, apertureDist);
        Vector3 closestApertureWorld = transform.TransformPoint(new Vector3(
            Mathf.Clamp(localCam.x, -halfW, halfW),
            Mathf.Clamp(localCam.y, -halfH, halfH),
            0f));
        Vector3 toPortal = closestApertureWorld - camTransform.position;
        float distSq = toPortal.sqrMagnitude;
        if (distSq > 0.0001f)
        {
            float facing = Vector3.Dot(camTransform.forward, toPortal / Mathf.Sqrt(distSq));
            rawT *= Mathf.Clamp01(facing * 2f + 0.2f);
        }

        return rawT;
    }

    public void RefreshVisuals()
    {
        float progress = Application.isPlaying ? currentProgress : editorPreviewProgress;

        if (portalLight != null)
        {
            portalLight.enabled = progress > 0.01f;
            portalLight.intensity = progress * maxLightIntensity;
        }

        if (portalRenderer == null) return;

        Material mat = EnsureMaterial();
        if (mat == null) return;

        bool hasTex = linkedPortal != null && renderTex != null;
        if (hasTex)
            mat.SetTexture(PortalTexId, renderTex);

        mat.SetFloat(HasPortalTexId, hasTex ? 1f : 0f);
        mat.SetFloat(OpenProgressId, progress);
    }

    public void SetProximityFlatten(float t)
    {
        if (runtimeMat == null || !hasBaseMatProps) return;

        t = Mathf.Clamp01(t);
        if (Mathf.Abs(t - lastFlattenT) < 0.0005f) return;
        lastFlattenT = t;

        if (runtimeMat.HasProperty(RippleStrengthId))
            runtimeMat.SetFloat(RippleStrengthId, Mathf.Lerp(baseRipple, 0f, t));
        if (runtimeMat.HasProperty(LensCurvatureId))
            runtimeMat.SetFloat(LensCurvatureId, Mathf.Lerp(baseCurvature, 0f, t));
        if (runtimeMat.HasProperty(InnerGlowColorId))
            runtimeMat.SetColor(InnerGlowColorId, Color.Lerp(baseInnerGlow, Color.clear, t));
        if (runtimeMat.HasProperty(TargetBrightnessId))
            runtimeMat.SetFloat(TargetBrightnessId, Mathf.Lerp(baseBrightness, 1f, t));
    }

    public void PlayTeleportAudioAndEvent(Vector3 targetPos)
    {
        if (teleportSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(teleportSound, targetPos);

        onPlayerTeleported?.Invoke();
    }

    private Material EnsureMaterial()
    {
        if (portalRenderer == null) return null;

        Shader shader = portalShader;
        if (shader == null && portalRenderer.sharedMaterial != null)
            shader = portalRenderer.sharedMaterial.shader;
        if (shader == null)
            shader = Shader.Find("Shader Graphs/PortalGateway");
        if (shader == null) return portalRenderer.sharedMaterial;

        if (!Application.isPlaying)
        {
            Material shared = portalRenderer.sharedMaterial;
            if (shared == null || shared.shader != shader)
            {
                shared = new Material(shader)
                {
                    name = $"PortalGateway_Mat_{name}",
                    renderQueue = 2030
                };
                portalRenderer.sharedMaterial = shared;
            }
            return shared;
        }

        if (runtimeMat == null)
        {
            Material source = portalRenderer.sharedMaterial;
            runtimeMat = (source != null && source.shader == shader) ? new Material(source) : new Material(shader);
            runtimeMat.name = $"PortalGateway_RuntimeMat_{gameObject.GetInstanceID()}";
            runtimeMat.hideFlags = HideFlags.DontSave;
            runtimeMat.renderQueue = 2030;
            portalRenderer.sharedMaterial = runtimeMat;

            if (runtimeMat.HasProperty(RippleStrengthId)) baseRipple = runtimeMat.GetFloat(RippleStrengthId);
            if (runtimeMat.HasProperty(LensCurvatureId)) baseCurvature = runtimeMat.GetFloat(LensCurvatureId);
            if (runtimeMat.HasProperty(InnerGlowColorId)) baseInnerGlow = runtimeMat.GetColor(InnerGlowColorId);
            if (runtimeMat.HasProperty(TargetBrightnessId))
            {
                baseBrightness = runtimeMat.GetFloat(TargetBrightnessId);
                if (Mathf.Approximately(baseBrightness, 1.05f))
                {
                    baseBrightness = 1f;
                    runtimeMat.SetFloat(TargetBrightnessId, 1f);
                }
            }
            lastFlattenT = -1f;
            hasBaseMatProps = true;
        }

        return runtimeMat;
    }

    private void EnsureCamera(Camera sourceCam)
    {
        int width = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelWidth * renderTextureScale));
        int height = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelHeight * renderTextureScale));

        if (renderTex == null || renderTex.width != width || renderTex.height != height)
        {
            if (renderTex != null)
            {
                if (portalCamera != null) portalCamera.targetTexture = null;
                renderTex.Release();
                if (Application.isPlaying) Destroy(renderTex);
                else DestroyImmediate(renderTex);
            }

            renderTex = new RenderTexture(width, height, 32, RenderTextureFormat.DefaultHDR)
            {
                name = $"PortalRT_{gameObject.GetInstanceID()}",
                hideFlags = HideFlags.DontSave
            };
            renderTex.Create();
        }

        if (portalCamera == null)
        {
            var camObj = new GameObject($"PortalCam_{name}") { hideFlags = HideFlags.HideAndDontSave };
            camObj.transform.SetParent(transform, false);
            portalCamera = camObj.AddComponent<Camera>();
            portalCamera.enabled = false;
            portalCamera.cameraType = CameraType.Game;
            portalCamera.allowHDR = true;
            portalCamera.allowMSAA = false;
            cameraData = camObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            cameraData.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            VolumetricFogRendererFeature.RegisterFogCamera(portalCamera);
        }
    }

    private void ReleaseCamera()
    {
        if (portalCamera != null)
        {
            VolumetricFogRendererFeature.UnregisterFogCamera(portalCamera);
            portalCamera.targetTexture = null;
            if (Application.isPlaying) Destroy(portalCamera.gameObject);
            else DestroyImmediate(portalCamera.gameObject);
            portalCamera = null;
        }

        if (renderTex != null)
        {
            renderTex.Release();
            if (Application.isPlaying) Destroy(renderTex);
            else DestroyImmediate(renderTex);
            renderTex = null;
        }
    }

    private void PinTargetScene(bool pinned)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(targetSceneName) || isScenePinned == pinned) return;
        if (SceneStreamer.Instance == null) return;

        if (pinned) SceneStreamer.Instance.PinScene(targetSceneName);
        else SceneStreamer.Instance.UnpinScene(targetSceneName);
        isScenePinned = pinned;
    }

    private void SaveOpenState(bool open)
    {
        if (!Application.isPlaying) return;

        if (!string.IsNullOrEmpty(portalId))
            SaveManager.SetPortalOpenState(portalId, open, gameObject.scene.name);

        if (syncStateWithLinked && !string.IsNullOrEmpty(targetPortalId))
        {
            string sceneName = !string.IsNullOrEmpty(targetSceneName) ? targetSceneName : gameObject.scene.name;
            SaveManager.SetPortalOpenState(targetPortalId, open, sceneName);
        }
    }

    private bool HasRequiredSockets()
    {
        if (requiredSocketIds == null) return false;
        for (int i = 0; i < requiredSocketIds.Count; i++)
        {
            if (!string.IsNullOrEmpty(requiredSocketIds[i]))
                return true;
        }
        return false;
    }

    private bool AreSocketsMet()
    {
        if (requiredSocketIds == null) return true;
        for (int i = 0; i < requiredSocketIds.Count; i++)
        {
            string id = requiredSocketIds[i];
            if (!string.IsNullOrEmpty(id) && !SaveManager.IsSocketPlacedAnyScene(id))
                return false;
        }
        return true;
    }

    private void OnSocketPlaced(ItemSocket socket, ItemData item)
    {
        if (HasRequiredSockets() && AreSocketsMet())
            StartCoroutine(OpenDelayedRoutine());
    }

    private IEnumerator OpenDelayedRoutine()
    {
        if (socketActivationDelay > 0f)
            yield return new WaitForSeconds(socketActivationDelay);
        Open();
    }

    private void EnsureSetup()
    {
        if (portalRenderer == null)
            portalRenderer = GetComponentInChildren<Renderer>();

        if (portalRenderer == null)
            SetupDefaultQuadPortal();
        else
            RefreshVisuals();
    }

    [ContextMenu("포탈 기본 세팅 생성 (Quad)")]
    public void SetupDefaultQuadPortal()
    {
        Vector3 visualScale = new Vector3(2.2f, 3.0f, 1f);
        Transform existing = transform.Find("PortalSurface");
        if (existing != null && portalRenderer == null)
            portalRenderer = existing.GetComponent<Renderer>();

        GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;

        if (portalRenderer == null)
        {
            tempQuad.name = "PortalSurface";
            tempQuad.transform.SetParent(transform, false);
            tempQuad.transform.localPosition = Vector3.zero;
            tempQuad.transform.localRotation = Quaternion.identity;
            tempQuad.transform.localScale = visualScale;

            if (tempQuad.TryGetComponent<Collider>(out var col))
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            portalRenderer = tempQuad.GetComponent<Renderer>();
        }
        else
        {
            if (portalRenderer.TryGetComponent<MeshFilter>(out var mf))
                mf.sharedMesh = quadMesh;

            portalRenderer.transform.localScale = visualScale;

            if (Application.isPlaying) Destroy(tempQuad);
            else DestroyImmediate(tempQuad);
        }

        defaultScale = visualScale;
        RefreshVisuals();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
        Vector3 pos = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.DrawRay(pos, forward * 1.2f);
        Gizmos.DrawWireSphere(pos + forward * 1.2f, 0.1f);

        if (linkedPortal != null)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.6f);
            Gizmos.DrawLine(pos, linkedPortal.transform.position);
        }
    }
}
