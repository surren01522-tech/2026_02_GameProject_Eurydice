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
public class PortalGateway : MonoBehaviour
{
    private static readonly Dictionary<string, PortalGateway> ActivePortalsById = new();
    private static readonly List<PortalGateway> AllActivePortals = new();
    private static readonly Plane[] CachedFrustumPlanes = new Plane[6];
    private static readonly List<Renderer> HiddenRenderersBuffer = new(16);

    [FoldGroup("포탈 연결 및 심리스 설정")]
    [SerializeField] private string portalId;
    [SerializeField] private string targetPortalId;
    [SerializeField] private string targetSceneName;
    [SerializeField] private PortalGateway linkedPortal;
    [SerializeField] private bool startOpen = false;
    [SerializeField] private bool syncStateWithLinked = true;
    [SerializeField] private bool mirrorForwardDirection = true;
    [SerializeField] private Renderer portalRenderer;

    [FoldGroup("개폐 연출 설정")]
    [SerializeField] private float openDuration = 1.2f;
    [SerializeField] private float closeDuration = 0.6f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool animateTransformScale = false;
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

    private Camera portalCamera;
    private UniversalAdditionalCameraData cameraData;
    private RenderTexture renderTexture2D;
    private Material runtimeMaterial;
    private Coroutine transitionCoroutine;
    private Vector3 defaultRendererScale = new Vector3(2.2f, 3.0f, 1f);
    private float currentProgress = 1f;
    private float lastTeleportTime = -10f;
    private bool isScenePinned;

    private PlayerController cachedPlayer;
    private Vector3 previousPlayerWorldPos;
    private bool hasPreviousPlayerPos;
    private PlayerController exitingPlayer;
    private float exitingPlayerSide;

    private static bool isRenderingAnyPortal;

    private static readonly int PortalTexId = Shader.PropertyToID("_PortalTex");
    private static readonly int HasPortalTexId = Shader.PropertyToID("_HasPortalTex");
    private static readonly int OpenProgressId = Shader.PropertyToID("_OpenProgress");
    private static readonly Quaternion HalfTurn = Quaternion.Euler(0f, 180f, 0f);

    private void Reset()
    {
        SetupDefaultQuadPortal();
    }

    private void Awake()
    {
        EnsureDefaultSetup();
        if (portalRenderer != null && portalRenderer.transform.localScale.sqrMagnitude > 0.001f)
            defaultRendererScale = portalRenderer.transform.localScale;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        RegisterPortalInRegistry();
        ItemSocket.OnAnySocketPlaced += HandleAnySocketPlaced;

        cachedPlayer = FindFirstObjectByType<PlayerController>();
        if (cachedPlayer != null)
        {
            previousPlayerWorldPos = cachedPlayer.transform.position;
            hasPreviousPlayerPos = true;
        }

        bool savedOpen = SaveManager.IsPortalOpenAnyScene(portalId) ||
                         (syncStateWithLinked && SaveManager.IsPortalOpenAnyScene(targetPortalId));
        bool fulfilledOnLoad = HasSocketRequirements() && AreSocketRequirementsMet();
        bool linkedAlreadyOpen = syncStateWithLinked && linkedPortal != null &&
                                 (linkedPortal.CurrentState == PortalState.Open || linkedPortal.CurrentState == PortalState.Opening);

        bool shouldOpen = startOpen || savedOpen || fulfilledOnLoad || linkedAlreadyOpen;
        SetOpenImmediate(shouldOpen, syncLinked: false, saveState: fulfilledOnLoad && !savedOpen);
    }

    private void OnEnable()
    {
        if (!AllActivePortals.Contains(this))
            AllActivePortals.Add(this);

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        EnsureDefaultSetup();
        RegisterPortalInRegistry();

        if (!Application.isPlaying)
        {
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null)
                portalRenderer.enabled = true;
            UpdateVisualProperties();
        }
    }

    private void OnDisable()
    {
        AllActivePortals.Remove(this);
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        UnregisterPortalFromRegistry();
        SetScenePinned(false);
        ClearExitingPlayerState();
        ReleaseRenderResources();
    }

    private void OnDestroy()
    {
        AllActivePortals.Remove(this);
        ItemSocket.OnAnySocketPlaced -= HandleAnySocketPlaced;
        UnregisterPortalFromRegistry();
        SetScenePinned(false);
        ClearExitingPlayerState();
        ReleaseRenderResources();

        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private void OnValidate()
    {
        if (portalRenderer == null)
            portalRenderer = GetComponentInChildren<Renderer>();

        if (!Application.isPlaying)
        {
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null)
                portalRenderer.enabled = true;
        }
        UpdateVisualProperties();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (linkedPortal != null && linkedPortal.linkedPortal == null)
                linkedPortal.linkedPortal = this;
            return;
        }

        if (linkedPortal == null && !string.IsNullOrEmpty(targetPortalId))
            TryResolveLinkedPortalById();

        if (debugToggleKey != Key.None && Keyboard.current != null && Keyboard.current[debugToggleKey].wasPressedThisFrame)
            Toggle();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<PlayerController>();

        if (cachedPlayer != null)
        {
            Vector3 currentPlayerPos = cachedPlayer.transform.position;
            if (hasPreviousPlayerPos && CurrentState == PortalState.Open && linkedPortal != null)
                CheckSegmentCrossing(cachedPlayer, previousPlayerWorldPos, currentPlayerPos);

            previousPlayerWorldPos = cachedPlayer.transform.position;
            hasPreviousPlayerPos = true;
        }

        if (exitingPlayer != null)
            UpdateExitingCameraPassThrough();
    }

    private void RegisterPortalInRegistry()
    {
        if (!string.IsNullOrEmpty(portalId))
            ActivePortalsById[portalId] = this;

        TryResolveLinkedPortalById();

        foreach (var kvp in ActivePortalsById)
        {
            var other = kvp.Value;
            if (other != null && other != this && !string.IsNullOrEmpty(portalId) && other.targetPortalId == portalId)
            {
                other.linkedPortal = this;
                if (linkedPortal == null && !string.IsNullOrEmpty(targetPortalId) && targetPortalId == other.portalId)
                    linkedPortal = other;
            }
        }
    }

    private void UnregisterPortalFromRegistry()
    {
        if (!string.IsNullOrEmpty(portalId) && ActivePortalsById.TryGetValue(portalId, out var existing) && existing == this)
            ActivePortalsById.Remove(portalId);

        if (linkedPortal != null && linkedPortal.linkedPortal == this)
            linkedPortal.linkedPortal = null;
    }

    private void TryResolveLinkedPortalById()
    {
        if (string.IsNullOrEmpty(targetPortalId)) return;
        if (ActivePortalsById.TryGetValue(targetPortalId, out var target) && target != null && target != this)
        {
            linkedPortal = target;
            if (target.linkedPortal == null && !string.IsNullOrEmpty(target.targetPortalId) && target.targetPortalId == portalId)
                target.linkedPortal = this;
        }
    }

    private void SetScenePinned(bool pinned)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(targetSceneName)) return;
        if (isScenePinned == pinned) return;

        if (SceneStreamer.Instance != null)
        {
            if (pinned)
                SceneStreamer.Instance.PinScene(targetSceneName);
            else
                SceneStreamer.Instance.UnpinScene(targetSceneName);
            isScenePinned = pinned;
        }
    }

    private void PersistPortalState(bool open)
    {
        if (!Application.isPlaying) return;

        if (!string.IsNullOrEmpty(portalId))
            SaveManager.SetPortalOpenState(portalId, open, gameObject.scene.name);

        if (syncStateWithLinked && !string.IsNullOrEmpty(targetPortalId))
        {
            string targetScene = !string.IsNullOrEmpty(targetSceneName) ? targetSceneName : gameObject.scene.name;
            SaveManager.SetPortalOpenState(targetPortalId, open, targetScene);
        }
    }

    /// <summary>
    /// 포탈 개방 연출을 시작합니다.
    /// </summary>
    public void Open() => SetOpen(true, syncStateWithLinked);

    /// <summary>
    /// 포탈 소멸(닫힘) 연출을 시작합니다.
    /// </summary>
    public void Close() => SetOpen(false, syncStateWithLinked);

    /// <summary>
    /// 포탈 개폐 상태를 토글합니다.
    /// </summary>
    public void Toggle()
    {
        if (CurrentState == PortalState.Open || CurrentState == PortalState.Opening)
            Close();
        else
            Open();
    }

    /// <summary>
    /// 애니메이션 없이 즉시 포탈의 개폐 상태를 설정합니다.
    /// </summary>
    public void SetOpenImmediate(bool open, bool syncLinked = true, bool saveState = true)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        SetScenePinned(open);
        TryResolveLinkedPortalById();

        CurrentState = open ? PortalState.Open : PortalState.Closed;
        currentProgress = open ? 1f : 0f;

        if (portalRenderer != null)
        {
            portalRenderer.enabled = open;
            if (animateTransformScale)
                portalRenderer.transform.localScale = open ? defaultRendererScale : Vector3.zero;
        }

        if (saveState)
            PersistPortalState(open);

        UpdateVisualProperties();

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
            if (portalRenderer != null)
                portalRenderer.enabled = true;
            UpdateVisualProperties();
            return;
        }

        if (open && (CurrentState == PortalState.Open || CurrentState == PortalState.Opening)) return;
        if (!open && (CurrentState == PortalState.Closed || CurrentState == PortalState.Closing)) return;

        if (open)
            SetScenePinned(true);

        PersistPortalState(open);

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionRoutine(open));

        if (syncLinked && linkedPortal != null)
            linkedPortal.SetOpen(open, syncLinked: false);
    }

    private IEnumerator TransitionRoutine(bool opening)
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
                portalRenderer.transform.localScale = defaultRendererScale * currentProgress;

            UpdateVisualProperties();
            yield return null;
        }

        TryResolveLinkedPortalById();
        if (opening && syncStateWithLinked && linkedPortal != null && !linkedPortal.IsOpen)
            linkedPortal.SetOpenImmediate(true, syncLinked: false, saveState: false);

        if (!opening)
            SetScenePinned(false);

        currentProgress = targetVal;
        CurrentState = opening ? PortalState.Open : PortalState.Closed;

        if (portalRenderer != null)
        {
            portalRenderer.enabled = opening;
            if (animateTransformScale)
                portalRenderer.transform.localScale = opening ? defaultRendererScale : Vector3.zero;
        }

        UpdateVisualProperties();
        transitionCoroutine = null;

        if (opening)
            onPortalOpened?.Invoke();
        else
            onPortalClosed?.Invoke();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (isRenderingAnyPortal) return;
        if (currentProgress <= 0.001f || linkedPortal == null || portalRenderer == null || !portalRenderer.enabled) return;
        if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection || cam == portalCamera) return;
        if (cam != Camera.main && cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;

        if (Application.isPlaying && exitingPlayer != null && cam == Camera.main && IsCameraBehindExitPlane(cam.transform))
        {
            UpdateVisualProperties(suppressSurface: true);
            return;
        }

        float sqrDist = (cam.transform.position - transform.position).sqrMagnitude;
        if (sqrDist > maxRenderDistance * maxRenderDistance) return;

        GeometryUtility.CalculateFrustumPlanes(cam, CachedFrustumPlanes);
        if (!GeometryUtility.TestPlanesAABB(CachedFrustumPlanes, portalRenderer.bounds)) return;

        EnsurePortalCamera(cam);
        if (portalCamera == null || renderTexture2D == null) return;

        Transform inTransform = transform;
        Transform outTransform = linkedPortal.transform;

        Vector3 localCamPos = inTransform.InverseTransformPoint(cam.transform.position);
        Quaternion localCamRot = Quaternion.Inverse(inTransform.rotation) * cam.transform.rotation;

        if (mirrorForwardDirection)
        {
            localCamPos = HalfTurn * localCamPos;
            localCamRot = HalfTurn * localCamRot;
        }

        Vector3 mappedCamPos = outTransform.TransformPoint(localCamPos);
        Quaternion mappedCamRot = outTransform.rotation * localCamRot;

        portalCamera.nearClipPlane = cam.nearClipPlane;
        portalCamera.farClipPlane = cam.farClipPlane;
        portalCamera.cullingMask = cullingMask;
        portalCamera.targetTexture = renderTexture2D;
        portalCamera.transform.SetPositionAndRotation(mappedCamPos, mappedCamRot);
        portalCamera.fieldOfView = cam.fieldOfView;
        portalCamera.aspect = cam.aspect;
        portalCamera.projectionMatrix = cam.projectionMatrix;

        if (useObliqueClip)
        {
            float distToOutPlane = Vector3.Distance(mappedCamPos, outTransform.position);
            if (distToOutPlane > 0.25f)
            {
                Vector3 normal = mirrorForwardDirection ? outTransform.forward : -outTransform.forward;
                float side = Mathf.Sign(Vector3.Dot(normal, outTransform.position - mappedCamPos));
                Vector3 camSpacePos = portalCamera.worldToCameraMatrix.MultiplyPoint(outTransform.position);
                Vector3 camSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector(normal) * side;
                float camSpaceDist = -Vector3.Dot(camSpacePos, camSpaceNormal) + clipPlaneOffset;

                if (Mathf.Abs(camSpaceDist) > 0.15f)
                {
                    Vector4 clipPlane = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDist);
                    portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlane);
                }
            }
        }

        HiddenRenderersBuffer.Clear();
        for (int i = 0; i < AllActivePortals.Count; i++)
        {
            var p = AllActivePortals[i];
            if (p != null && p.portalRenderer != null && p.portalRenderer.enabled)
            {
                p.portalRenderer.enabled = false;
                HiddenRenderersBuffer.Add(p.portalRenderer);
            }
        }

        isRenderingAnyPortal = true;
        try
        {
            RenderPipeline.SubmitRenderRequest(portalCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture2D });
        }
        finally
        {
            for (int i = 0; i < HiddenRenderersBuffer.Count; i++)
            {
                if (HiddenRenderersBuffer[i] != null)
                    HiddenRenderersBuffer[i].enabled = true;
            }
            HiddenRenderersBuffer.Clear();
            isRenderingAnyPortal = false;
        }

        UpdateVisualProperties(suppressSurface: false);
    }

    private void EnsurePortalCamera(Camera sourceCam)
    {
        RenderTextureFormat format = sourceCam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        int width = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelWidth * renderTextureScale));
        int height = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelHeight * renderTextureScale));

        if (renderTexture2D == null || renderTexture2D.width != width || renderTexture2D.height != height)
        {
            if (renderTexture2D != null)
            {
                if (portalCamera != null) portalCamera.targetTexture = null;
                renderTexture2D.Release();
                DestroyImmediate(renderTexture2D);
            }

            renderTexture2D = new RenderTexture(width, height, 24, format)
            {
                name = $"PortalRT_{gameObject.GetInstanceID()}",
                hideFlags = HideFlags.DontSave
            };
            renderTexture2D.Create();
        }

        if (portalCamera == null)
        {
            GameObject camObj = new GameObject($"PortalCam_{name}")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            camObj.transform.SetParent(transform, false);
            portalCamera = camObj.AddComponent<Camera>();
            portalCamera.enabled = false;
            portalCamera.cameraType = CameraType.Reflection;
            cameraData = camObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
        }
    }

    private void ReleaseRenderResources()
    {
        if (portalCamera != null)
        {
            portalCamera.targetTexture = null;
            if (Application.isPlaying)
                Destroy(portalCamera.gameObject);
            else
                DestroyImmediate(portalCamera.gameObject);
            portalCamera = null;
        }

        if (renderTexture2D != null)
        {
            renderTexture2D.Release();
            if (Application.isPlaying)
                Destroy(renderTexture2D);
            else
                DestroyImmediate(renderTexture2D);
            renderTexture2D = null;
        }
    }

    /// <summary>
    /// 포탈의 런타임 상태 프로퍼티(_PortalTex, _HasPortalTex, _OpenProgress)와 라이트 강도를 갱신합니다.
    /// </summary>
    public void UpdateVisualProperties(bool suppressSurface = false)
    {
        float effectiveProgress = Application.isPlaying ? currentProgress : editorPreviewProgress;

        if (portalLight != null)
        {
            portalLight.enabled = effectiveProgress > 0.01f;
            portalLight.intensity = effectiveProgress * maxLightIntensity;
        }

        if (portalRenderer == null) return;

        Material targetMat = EnsurePortalMaterial();
        if (targetMat == null) return;

        bool hasValidTex = linkedPortal != null && renderTexture2D != null;
        if (hasValidTex)
            targetMat.SetTexture(PortalTexId, renderTexture2D);

        targetMat.SetFloat(HasPortalTexId, hasValidTex ? 1f : 0f);
        targetMat.SetFloat(OpenProgressId, suppressSurface ? 0f : effectiveProgress);
    }

    private Material EnsurePortalMaterial()
    {
        if (portalRenderer == null) return null;

        Shader shader = Shader.Find("Shader Graphs/PortalGateway") ?? Shader.Find("Custom/PortalGateway");
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

        if (runtimeMaterial == null)
        {
            Material source = portalRenderer.sharedMaterial;
            if (source != null && source.shader == shader)
                runtimeMaterial = new Material(source);
            else
                runtimeMaterial = new Material(shader);

            runtimeMaterial.name = $"PortalGateway_RuntimeMat_{gameObject.GetInstanceID()}";
            runtimeMaterial.hideFlags = HideFlags.DontSave;
            runtimeMaterial.renderQueue = 2030;
            portalRenderer.sharedMaterial = runtimeMaterial;
        }

        return runtimeMaterial;
    }

    /// <summary>
    /// 이전 프레임과 현재 프레임 위치를 선분 보간하여 포탈 중심면 통과 여부와 잔여 이동량을 계산합니다.
    /// </summary>
    private void CheckSegmentCrossing(PlayerController player, Vector3 prevWorldPos, Vector3 currWorldPos)
    {
        if (Time.time - lastTeleportTime < teleportCooldown) return;

        Transform inT = transform;
        Vector3 prevLocal = inT.InverseTransformPoint(prevWorldPos);
        Vector3 currLocal = inT.InverseTransformPoint(currWorldPos);
        float zDelta = currLocal.z - prevLocal.z;

        bool crossesPlane = (prevLocal.z < 0f && currLocal.z >= 0f) ||
                            (prevLocal.z > 0f && currLocal.z <= 0f);
        if (!crossesPlane || Mathf.Abs(zDelta) < 0.00001f) return;

        float crossingT = Mathf.Clamp01(-prevLocal.z / zDelta);
        Vector3 pointOnPlane = Vector3.Lerp(prevLocal, currLocal, crossingT);

        float halfWidth = Mathf.Max(0.4f, defaultRendererScale.x * 0.55f);
        float halfHeight = Mathf.Max(0.6f, defaultRendererScale.y * 0.65f);
        if (Mathf.Abs(pointOnPlane.x) > halfWidth || Mathf.Abs(pointOnPlane.y) > halfHeight)
            return;

        ExecuteTeleport(player, prevLocal, currLocal);
    }

    private void ExecuteTeleport(PlayerController player, Vector3 prevLocal, Vector3 currLocal)
    {
        if (player == null || linkedPortal == null) return;

        float entrySide = Mathf.Abs(prevLocal.z) > 0.0001f ? Mathf.Sign(prevLocal.z) : 1f;
        lastTeleportTime = Time.time;
        ClearExitingPlayerState();

        Transform inT = transform;
        Transform outT = linkedPortal.transform;

        Vector3 mappedLocalPos = currLocal;
        Quaternion localRot = Quaternion.Inverse(inT.rotation) * player.transform.rotation;

        Transform camT = player.CameraTransform != null ? player.CameraTransform : (Camera.main != null ? Camera.main.transform : player.transform);
        Vector3 localCamPos = inT.InverseTransformPoint(camT.position);
        Quaternion localCamRot = Quaternion.Inverse(inT.rotation) * camT.rotation;

        if (mirrorForwardDirection)
        {
            mappedLocalPos = HalfTurn * mappedLocalPos;
            localRot = HalfTurn * localRot;
            localCamPos = HalfTurn * localCamPos;
            localCamRot = HalfTurn * localCamRot;
        }

        float exitSide = mirrorForwardDirection ? entrySide : -entrySide;
        if (Mathf.Abs(mappedLocalPos.z) < exitClearance)
            mappedLocalPos.z = exitSide * exitClearance;

        Vector3 targetPos = outT.TransformPoint(mappedLocalPos);
        Quaternion targetRot = outT.rotation * localRot;
        Vector3 targetCamPos = outT.TransformPoint(localCamPos);
        Quaternion targetCamRot = outT.rotation * localCamRot;

        linkedPortal.RegisterIncomingTeleport(player, exitSide);
        player.Teleport(targetPos, targetRot, targetCamPos, targetCamRot);
        previousPlayerWorldPos = targetPos;
        SceneStreamer.Instance?.RefreshZonesForPosition(targetPos);

        if (teleportSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(teleportSound, targetPos);

        onPlayerTeleported?.Invoke();
    }

    /// <summary>
    /// 반대편 포탈에서 플레이어가 도착했을 때 쿨타임 및 3인칭 카메라 후면 통과 상태를 등록합니다.
    /// </summary>
    public void RegisterIncomingTeleport(PlayerController player = null, float exitSide = 1f)
    {
        lastTeleportTime = Time.time;
        exitingPlayer = player;
        exitingPlayerSide = exitSide;

        if (player != null)
        {
            previousPlayerWorldPos = player.transform.position;
            hasPreviousPlayerPos = true;
            player.CameraModeController?.SetDeoccluderEnabled(false);
        }
    }

    private bool IsCameraBehindExitPlane(Transform camTransform)
    {
        if (exitingPlayer == null || camTransform == null)
            return false;

        float playerLocalZ = transform.InverseTransformPoint(exitingPlayer.transform.position).z;
        float camLocalZ = transform.InverseTransformPoint(camTransform.position).z;

        return (playerLocalZ * exitingPlayerSide >= -0.1f) && (camLocalZ * exitingPlayerSide < 0.05f);
    }

    private void UpdateExitingCameraPassThrough()
    {
        if (exitingPlayer == null) return;

        Transform camT = exitingPlayer.CameraTransform != null ? exitingPlayer.CameraTransform : (Camera.main != null ? Camera.main.transform : null);
        if (camT == null || !IsCameraBehindExitPlane(camT))
        {
            ClearExitingPlayerState();
            UpdateVisualProperties(suppressSurface: false);
        }
    }

    private void ClearExitingPlayerState()
    {
        if (exitingPlayer != null)
        {
            exitingPlayer.CameraModeController?.SetDeoccluderEnabled(true);
            exitingPlayer = null;
        }
    }

    private bool HasSocketRequirements()
    {
        if (requiredSocketIds == null) return false;
        for (int i = 0; i < requiredSocketIds.Count; i++)
        {
            if (!string.IsNullOrEmpty(requiredSocketIds[i]))
                return true;
        }
        return false;
    }

    private bool AreSocketRequirementsMet()
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

    private void HandleAnySocketPlaced(ItemSocket socket, ItemData item)
    {
        if (HasSocketRequirements() && AreSocketRequirementsMet())
            StartCoroutine(DelayedOpenRoutine());
    }

    private IEnumerator DelayedOpenRoutine()
    {
        if (socketActivationDelay > 0f)
            yield return new WaitForSeconds(socketActivationDelay);
        Open();
    }

    private void EnsureDefaultSetup()
    {
        if (portalRenderer == null)
            portalRenderer = GetComponentInChildren<Renderer>();

        if (portalRenderer == null)
            SetupDefaultQuadPortal();
        else
            UpdateVisualProperties();
    }

    /// <summary>
    /// 기본 평면(Quad) 포탈 렌더러 구성을 생성하거나 복구합니다.
    /// </summary>
    [ContextMenu("포탈 기본 세팅 생성 (Quad)")]
    public void SetupDefaultQuadPortal()
    {
        Vector3 visualScale = new Vector3(2.2f, 3.0f, 1f);
        Transform existingSurface = transform.Find("PortalSurface");
        if (existingSurface != null && portalRenderer == null)
            portalRenderer = existingSurface.GetComponent<Renderer>();

        GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Mesh targetMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;

        if (portalRenderer == null)
        {
            tempPrimitive.name = "PortalSurface";
            tempPrimitive.transform.SetParent(transform, false);
            tempPrimitive.transform.localPosition = Vector3.zero;
            tempPrimitive.transform.localRotation = Quaternion.identity;
            tempPrimitive.transform.localScale = visualScale;

            if (tempPrimitive.TryGetComponent<Collider>(out var col))
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            portalRenderer = tempPrimitive.GetComponent<Renderer>();
        }
        else
        {
            if (portalRenderer.TryGetComponent<MeshFilter>(out var mf))
                mf.sharedMesh = targetMesh;

            portalRenderer.transform.localScale = visualScale;

            if (Application.isPlaying) Destroy(tempPrimitive);
            else DestroyImmediate(tempPrimitive);
        }

        defaultRendererScale = visualScale;
        UpdateVisualProperties();
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
