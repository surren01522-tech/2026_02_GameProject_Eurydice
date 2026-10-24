using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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
    private struct SplitSession
    {
        public bool active;
        public PortalGateway entry;
        public PortalGateway exit;
        public PlayerController player;
        public float entrySide;
        public float exitSide;
        public bool mirror;
        public Vector3 lastGhostWorld;
        public float funnelPull;
        public string pinnedEntryScene;
        public string pinnedExitScene;
    }

    private const float PortalCamNearClip = 0.01f;
    private const float SplitCrossDist = 0.025f;
    private const float FrontCameraClearance = 0.045f;
    private const float MinPlayerClearance = 0.005f;
    private const float ProximityFadeRange = 0.65f;

    private static readonly Dictionary<string, PortalGateway> PortalsById = new();
    private static readonly List<PortalGateway> ActivePortals = new();
    private static readonly Plane[] FrustumPlanes = new Plane[6];
    private static readonly List<Renderer> HiddenRenderers = new(16);
    private static readonly PortalTravellerVisual TravellerVisual = new();
    private static readonly Quaternion HalfTurn = Quaternion.Euler(0f, 180f, 0f);

    private static readonly int PortalTexId = Shader.PropertyToID("_PortalTex");
    private static readonly int HasPortalTexId = Shader.PropertyToID("_HasPortalTex");
    private static readonly int OpenProgressId = Shader.PropertyToID("_OpenProgress");
    private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
    private static readonly int LensCurvatureId = Shader.PropertyToID("_LensCurvature");
    private static readonly int InnerGlowColorId = Shader.PropertyToID("_InnerGlowColor");
    private static readonly int TargetBrightnessId = Shader.PropertyToID("_TargetBrightness");

    private static SplitSession split;
    private static PlayerController trackedPlayer;
    private static Vector3 prevPlayerPos;
    private static bool hasPrevPlayerPos;
    private static int lastSteppedFrame = -1;
    private static bool isRenderingPortal;
    private static bool isBrainHooked;
    private static bool isNearAnyPortal;

    [FoldGroup("포탈 연결 및 심리스 설정")]
    [SerializeField] private string portalId;
    [SerializeField] private string targetPortalId;
    [SerializeField] private string targetSceneName;
    [SerializeField] private PortalGateway linkedPortal;
    [SerializeField] private bool startOpen;
    [SerializeField] private bool syncStateWithLinked = true;
    [SerializeField] private bool mirrorForwardDirection = true;
    [SerializeField] private Renderer portalRenderer;

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
    public Vector3 ApertureSize => defaultScale;
    public bool MirrorForward => mirrorForwardDirection;

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

    private void Reset() => SetupDefaultQuadPortal();

    private void Awake()
    {
        EnsureSetup();
        if (portalRenderer != null && portalRenderer.transform.localScale.sqrMagnitude > 0.001f)
            defaultScale = portalRenderer.transform.localScale;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        Register();
        ItemSocket.OnAnySocketPlaced += OnSocketPlaced;

        bool savedOpen = SaveManager.IsPortalOpenAnyScene(portalId) ||
                         (syncStateWithLinked && SaveManager.IsPortalOpenAnyScene(targetPortalId));
        bool socketsMet = HasRequiredSockets() && AreSocketsMet();
        bool linkedOpen = syncStateWithLinked && linkedPortal != null &&
                          (linkedPortal.CurrentState == PortalState.Open || linkedPortal.CurrentState == PortalState.Opening);

        SetOpenImmediate(startOpen || savedOpen || socketsMet || linkedOpen, syncLinked: false, saveState: socketsMet && !savedOpen);
    }

    private void OnEnable()
    {
        if (!ActivePortals.Contains(this))
            ActivePortals.Add(this);

        RenderPipelineManager.beginCameraRendering += RenderPortal;
        EnsureSetup();
        Register();

        if (Application.isPlaying && !isBrainHooked)
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnBrainUpdated);
            isBrainHooked = true;
        }

        if (!Application.isPlaying)
        {
            currentProgress = editorPreviewProgress;
            if (portalRenderer != null) portalRenderer.enabled = true;
            RefreshVisuals();
        }
    }

    private void OnDisable()
    {
        if (split.active && (split.entry == this || split.exit == this))
            CompleteSplit(Camera.main);

        ActivePortals.Remove(this);
        RenderPipelineManager.beginCameraRendering -= RenderPortal;
        Unregister();
        PinTargetScene(false);
        ReleaseCamera();

        if (ActivePortals.Count == 0 && isBrainHooked)
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnBrainUpdated);
            if (trackedPlayer != null)
                trackedPlayer.CameraModeController?.SetNearPortalClip(false, Camera.main);

            TravellerVisual.Dispose();
            isBrainHooked = false;
            isNearAnyPortal = false;
            trackedPlayer = null;
            hasPrevPlayerPos = false;
            lastSteppedFrame = -1;
        }
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

        if (linkedPortal == null && !string.IsNullOrEmpty(targetPortalId))
            TryLink();

        if (debugToggleKey != Key.None && Keyboard.current != null && Keyboard.current[debugToggleKey].wasPressedThisFrame)
            Toggle();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            StepPortals();
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
        TryLink();

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

        TryLink();
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

    private void RenderPortal(ScriptableRenderContext context, Camera cam)
    {
        if (isRenderingPortal) return;
        if (currentProgress <= 0.001f || linkedPortal == null || portalRenderer == null || !portalRenderer.enabled) return;
        if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection || cam == portalCamera) return;
        if (cam != Camera.main && cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;

        Transform inT = transform;
        Transform outT = linkedPortal.transform;
        Vector3 localCamPos = inT.InverseTransformPoint(cam.transform.position);
        float planeDist = Mathf.Abs(localCamPos.z);
        bool isSplitEntry = Application.isPlaying && split.active && split.entry == this && cam == Camera.main;
        float viewSide = isSplitEntry ? split.entrySide : (localCamPos.z >= 0f ? 1f : -1f);

        float sqrDist = (cam.transform.position - inT.position).sqrMagnitude;
        if (!isSplitEntry)
        {
            if (sqrDist > maxRenderDistance * maxRenderDistance) return;
            GeometryUtility.CalculateFrustumPlanes(cam, FrustumPlanes);
            if (!GeometryUtility.TestPlanesAABB(FrustumPlanes, portalRenderer.bounds)) return;
        }

        EnsureCamera(cam);
        if (portalCamera == null || renderTex == null) return;

        Vector3 mappedCamPos = outT.TransformPoint(MapLocal(localCamPos, mirrorForwardDirection));
        Quaternion mappedCamRot = GetRotationDelta(this, linkedPortal, mirrorForwardDirection) * cam.transform.rotation;

        if (cameraData != null)
            cameraData.renderShadows = isSplitEntry || sqrDist < 225f;

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
            // 출구 포탈 뒷벽이 비치지 않도록 클립 평면을 출구 포탈 약간 앞(-offset)으로 유지 (전 거리 연속 투영 행렬)
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

        HiddenRenderers.Clear();
        for (int i = 0; i < ActivePortals.Count; i++)
        {
            var p = ActivePortals[i];
            if (p != null && p.portalRenderer != null && p.portalRenderer.enabled)
            {
                p.portalRenderer.enabled = false;
                HiddenRenderers.Add(p.portalRenderer);
            }
        }

        isRenderingPortal = true;
        try
        {
            RenderPipeline.SubmitRenderRequest(portalCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = renderTex });
        }
        finally
        {
            for (int i = 0; i < HiddenRenderers.Count; i++)
            {
                if (HiddenRenderers[i] != null)
                    HiddenRenderers[i].enabled = true;
            }
            HiddenRenderers.Clear();
            isRenderingPortal = false;
        }

        SetProximityFlatten(isSplitEntry ? 1f - Mathf.InverseLerp(0f, ProximityFadeRange, planeDist) : 0f);
        RefreshVisuals();
    }

    private static void StepPortals()
    {
        if (lastSteppedFrame == Time.frameCount) return;
        lastSteppedFrame = Time.frameCount;

        if (trackedPlayer == null)
        {
            trackedPlayer = FindFirstObjectByType<PlayerController>();
            hasPrevPlayerPos = false;
            if (trackedPlayer == null) return;
        }

        Vector3 currPos = trackedPlayer.transform.position;
        if (!hasPrevPlayerPos)
        {
            prevPlayerPos = currPos;
            hasPrevPlayerPos = true;
            TravellerVisual.Sync(trackedPlayer, null, 1f, ActivePortals);
            return;
        }

        if (split.active)
        {
            if (split.entry == null || split.exit == null || !split.entry.IsOpen || !split.exit.IsOpen)
            {
                CompleteSplit(Camera.main);
            }
            else if (TryDetectDirectionalCross(split.exit, prevPlayerPos, currPos, split.exitSide, out Vector3 exitCurrLocal))
            {
                CancelSplit(exitCurrLocal);
            }
            else
            {
                for (int i = 0; i < ActivePortals.Count; i++)
                {
                    var p = ActivePortals[i];
                    if (p == null || p == split.exit || !p.IsOpen || p.linkedPortal == null) continue;
                    if (Time.time - p.lastTeleportTime < p.teleportCooldown) continue;

                    if (TryDetectCross(p, prevPlayerPos, currPos, out float side, out Vector3 currLocal))
                    {
                        CompleteSplit(Camera.main);
                        BeginCross(p, trackedPlayer, side, currLocal);
                        break;
                    }
                }
            }

            if (split.active)
                UpdateSplitGhost();
        }
        else
        {
            for (int i = 0; i < ActivePortals.Count; i++)
            {
                var p = ActivePortals[i];
                if (p == null || !p.IsOpen || p.linkedPortal == null) continue;
                if (Time.time - p.lastTeleportTime < p.teleportCooldown) continue;

                if (TryDetectCross(p, prevPlayerPos, currPos, out float side, out Vector3 currLocal))
                {
                    BeginCross(p, trackedPlayer, side, currLocal);
                    break;
                }
            }
        }

        prevPlayerPos = trackedPlayer.transform.position;
        TravellerVisual.Sync(trackedPlayer, split.active ? split.exit : null, split.exitSide, ActivePortals);
    }

    private static bool TryDetectCross(PortalGateway portal, Vector3 prevWorld, Vector3 currWorld, out float entrySide, out Vector3 currLocal)
    {
        Vector3 prevLocal = portal.transform.InverseTransformPoint(prevWorld);
        currLocal = portal.transform.InverseTransformPoint(currWorld);
        entrySide = Mathf.Abs(prevLocal.z) > 0.0001f ? Mathf.Sign(prevLocal.z) : 1f;

        bool crossed = (prevLocal.z < 0f && currLocal.z >= 0f) || (prevLocal.z > 0f && currLocal.z <= 0f);
        if (!crossed) return false;

        return IsWithinAperture(portal, prevLocal, currLocal);
    }

    private static bool TryDetectDirectionalCross(PortalGateway portal, Vector3 prevWorld, Vector3 currWorld, float fromSide, out Vector3 currLocal)
    {
        Vector3 prevLocal = portal.transform.InverseTransformPoint(prevWorld);
        currLocal = portal.transform.InverseTransformPoint(currWorld);

        if (prevLocal.z * fromSide <= 0f || currLocal.z * fromSide > 0f)
            return false;

        return IsWithinAperture(portal, prevLocal, currLocal);
    }

    private static bool IsWithinAperture(PortalGateway portal, Vector3 prevLocal, Vector3 currLocal)
    {
        float dz = currLocal.z - prevLocal.z;
        if (Mathf.Abs(dz) < 0.00001f) return false;

        Vector3 hit = Vector3.Lerp(prevLocal, currLocal, Mathf.Clamp01(-prevLocal.z / dz));
        float halfW = Mathf.Max(0.4f, portal.defaultScale.x * 0.55f);
        float halfH = Mathf.Max(0.6f, portal.defaultScale.y * 0.65f);
        return Mathf.Abs(hit.x) <= halfW && Mathf.Abs(hit.y) <= halfH;
    }

    private static void BeginCross(PortalGateway entry, PlayerController player, float entrySide, Vector3 currLocal)
    {
        PortalGateway exit = entry.linkedPortal;
        if (player == null || exit == null) return;

        bool mirror = entry.mirrorForwardDirection;
        float exitSide = mirror ? entrySide : -entrySide;

        Vector3 outLocal = MapLocal(currLocal, mirror);
        if (outLocal.z * exitSide < MinPlayerClearance)
            outLocal.z = exitSide * MinPlayerClearance;

        Vector3 targetPos = exit.transform.TransformPoint(outLocal);
        Quaternion rotDelta = GetRotationDelta(entry, exit, mirror);
        Quaternion targetRot = rotDelta * player.transform.rotation;

        entry.lastTeleportTime = Time.time;
        exit.lastTeleportTime = Time.time;

        Transform camT = player.CameraTransform;
        var camMode = player.CameraModeController;
        bool shouldSplit = camT != null && camMode != null &&
                           entry.transform.InverseTransformPoint(camT.position).z * entrySide > 0f;

        string entryScene = entry.gameObject.scene.name;
        string exitScene = exit.gameObject.scene.name;
        bool crossScene = entryScene != exitScene;

        if (shouldSplit)
        {
            if (crossScene)
            {
                SceneStreamer.Instance?.PinScene(entryScene);
                SceneStreamer.Instance?.PinScene(exitScene);
            }

            player.Warp(targetPos, targetRot);
            player.SetCameraBasisOffset(rotDelta);

            Transform realTarget = camMode.RealTarget != null ? camMode.RealTarget : player.transform;
            Vector3 ghostPos = entry.transform.TransformPoint(MapLocal(exit.transform.InverseTransformPoint(realTarget.position), mirror));
            Quaternion ghostRot = GetRotationDelta(exit, entry, mirror) * realTarget.rotation;

            camMode.BeginSplit(ghostPos, ghostRot);

            split = new SplitSession
            {
                active = true,
                entry = entry,
                exit = exit,
                player = player,
                entrySide = entrySide,
                exitSide = exitSide,
                mirror = mirror,
                lastGhostWorld = ghostPos,
                funnelPull = 0f,
                pinnedEntryScene = crossScene ? entryScene : null,
                pinnedExitScene = crossScene ? exitScene : null
            };
        }
        else
        {
            Vector3 camOutLocal = camT != null ? MapLocal(entry.transform.InverseTransformPoint(camT.position), mirror) : outLocal;
            float minZ = Mathf.Max(FrontCameraClearance, exit.exitClearance);
            if (camOutLocal.z * exitSide < minZ)
                camOutLocal.z = exitSide * minZ;

            Vector3 targetCamPos = exit.transform.TransformPoint(camOutLocal);
            Quaternion targetCamRot = camT != null ? rotDelta * camT.rotation : targetRot;
            player.Teleport(targetPos, targetRot, targetCamPos, targetCamRot);
        }

        SceneStreamer.Instance?.RefreshZonesForPosition(targetPos);

        if (entry.teleportSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(entry.teleportSound, targetPos);

        entry.onPlayerTeleported?.Invoke();
    }

    private static void CancelSplit(Vector3 exitCurrLocal)
    {
        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        PlayerController player = split.player;

        Vector3 returnLocal = MapLocal(exitCurrLocal, split.mirror);
        if (returnLocal.z * split.entrySide < MinPlayerClearance)
            returnLocal.z = split.entrySide * MinPlayerClearance;

        Vector3 returnPos = entry.transform.TransformPoint(returnLocal);
        Quaternion returnRot = GetRotationDelta(exit, entry, split.mirror) * player.transform.rotation;

        entry.lastTeleportTime = Time.time;
        exit.lastTeleportTime = Time.time;

        player.ClearCameraBasisOffset();
        player.Warp(returnPos, returnRot);
        player.CameraModeController?.EndSplit(Camera.main);

        entry.SetProximityFlatten(0f);
        CleanupSplit();
        SceneStreamer.Instance?.RefreshZonesForPosition(returnPos);
    }

    private static void UpdateSplitGhost()
    {
        var camMode = split.player != null ? split.player.CameraModeController : null;
        if (camMode == null) return;

        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        Transform realTarget = camMode.RealTarget != null ? camMode.RealTarget : split.player.transform;

        Vector3 ghostLocal = MapLocal(exit.transform.InverseTransformPoint(realTarget.position), split.mirror);
        Quaternion ghostRot = GetRotationDelta(exit, entry, split.mirror) * realTarget.rotation;

        Transform camT = split.player.CameraTransform;
        if (camT != null)
        {
            Vector3 camLocal = entry.transform.InverseTransformPoint(camT.position);
            float innerW = Mathf.Max(0.35f, entry.defaultScale.x * 0.4f);
            float innerH = Mathf.Max(0.5f, entry.defaultScale.y * 0.42f);

            float dz = camLocal.z - ghostLocal.z;
            float tPlane = Mathf.Abs(dz) > 0.0001f ? Mathf.Clamp01(camLocal.z / dz) : 0f;
            float hitX = Mathf.Lerp(camLocal.x, ghostLocal.x, tPlane);
            float hitY = Mathf.Lerp(camLocal.y, ghostLocal.y, tPlane);

            if (Mathf.Abs(hitX) > innerW || Mathf.Abs(hitY) > innerH)
            {
                split.funnelPull = Mathf.Clamp01(split.funnelPull + Time.deltaTime * 3.5f);
                Vector3 clampedAperture = new Vector3(
                    Mathf.Clamp(hitX, -innerW * 0.8f, innerW * 0.8f),
                    Mathf.Clamp(hitY, -innerH * 0.8f, innerH * 0.8f),
                    -split.entrySide * 0.15f
                );
                Vector3 orbitLocal = entry.transform.InverseTransformVector(camT.position - split.lastGhostWorld);
                ghostLocal = Vector3.Lerp(ghostLocal, clampedAperture - orbitLocal, split.funnelPull);
            }
            else
            {
                split.funnelPull = Mathf.MoveTowards(split.funnelPull, 0f, Time.deltaTime * 4f);
            }
        }

        Vector3 ghostWorld = entry.transform.TransformPoint(ghostLocal);
        split.lastGhostWorld = ghostWorld;
        camMode.UpdateGhost(ghostWorld, ghostRot);
    }

    private static void OnBrainUpdated(CinemachineBrain brain)
    {
        if (brain == null) return;
        Camera outCam = brain.OutputCamera;
        if (outCam == null || outCam != Camera.main) return;

        if (trackedPlayer == null)
            trackedPlayer = FindFirstObjectByType<PlayerController>();

        Transform camT = outCam.transform;

        if (split.active)
        {
            Vector3 camLocal = split.entry.transform.InverseTransformPoint(camT.position);
            if (camLocal.z * split.entrySide <= SplitCrossDist)
                CompleteSplit(outCam);

            UpdateNearPortalClipState(outCam, camT.position);
            return;
        }

        if (trackedPlayer != null)
            GuardPortalCameraPlane(camT, trackedPlayer);

        UpdateNearPortalClipState(outCam, camT.position);
    }

    private static void GuardPortalCameraPlane(Transform camT, PlayerController player)
    {
        var camMode = player.CameraModeController;
        Transform targetT = (camMode != null && camMode.RealTarget != null) ? camMode.RealTarget : player.transform;
        Vector3 targetWorld = targetT.position;

        for (int i = 0; i < ActivePortals.Count; i++)
        {
            var p = ActivePortals[i];
            if (p == null || !p.IsOpen || p.linkedPortal == null) continue;
            if ((targetWorld - p.transform.position).sqrMagnitude > 81f) continue;

            Vector3 targetLocal = p.transform.InverseTransformPoint(targetWorld);
            Vector3 camLocal = p.transform.InverseTransformPoint(camT.position);
            float side = targetLocal.z >= 0f ? 1f : -1f;
            float targetSignedZ = targetLocal.z * side;
            float camSignedZ = camLocal.z * side;

            float halfW = p.defaultScale.x * 0.52f;
            float halfH = p.defaultScale.y * 0.52f;

            if (targetSignedZ > FrontCameraClearance + 0.02f && camSignedZ < FrontCameraClearance)
            {
                float planeZ = side * FrontCameraClearance;
                float dz = camLocal.z - targetLocal.z;
                if (Mathf.Abs(dz) < 0.0001f) continue;

                float tHit = (planeZ - targetLocal.z) / dz;
                Vector3 hitLocal = Vector3.Lerp(targetLocal, camLocal, tHit);

                float weightX = 1f - Mathf.InverseLerp(halfW, halfW + 0.30f, Mathf.Abs(hitLocal.x));
                float weightY = 1f - Mathf.InverseLerp(halfH, halfH + 0.30f, Mathf.Abs(hitLocal.y));
                float weight = weightX * weightY;
                if (weight <= 0.001f) continue;

                Vector3 clampedLocal = Vector3.Lerp(camLocal, hitLocal, weight);
                if (Mathf.Abs(clampedLocal.x) < halfW && Mathf.Abs(clampedLocal.y) < halfH && clampedLocal.z * side < FrontCameraClearance)
                    clampedLocal.z = planeZ;

                camT.position = p.transform.TransformPoint(clampedLocal);
            }
            else if (Mathf.Abs(camLocal.z) < 0.03f && Mathf.Abs(camLocal.x) < halfW && Mathf.Abs(camLocal.y) < halfH)
            {
                float camSide = Mathf.Abs(camLocal.z) > 0.0001f ? Mathf.Sign(camLocal.z) : side;
                camLocal.z = camSide * 0.03f;
                camT.position = p.transform.TransformPoint(camLocal);
            }
        }
    }

    private static void UpdateNearPortalClipState(Camera outCam, Vector3 camWorldPos)
    {
        if (trackedPlayer == null || trackedPlayer.CameraModeController == null) return;

        float threshold = isNearAnyPortal ? 1.05f : 0.85f;
        bool near = false;

        for (int i = 0; i < ActivePortals.Count; i++)
        {
            var p = ActivePortals[i];
            if (p == null || !p.IsOpen) continue;

            Vector3 local = p.transform.InverseTransformPoint(camWorldPos);
            if (Mathf.Abs(local.z) < threshold &&
                Mathf.Abs(local.x) < p.defaultScale.x * 0.75f + 0.5f &&
                Mathf.Abs(local.y) < p.defaultScale.y * 0.75f + 0.5f)
            {
                near = true;
                break;
            }
        }

        if (near != isNearAnyPortal)
        {
            isNearAnyPortal = near;
            trackedPlayer.CameraModeController.SetNearPortalClip(near, outCam);
        }
        else if (near || split.active)
        {
            outCam.nearClipPlane = PortalCamNearClip;
        }
    }

    private static void CompleteSplit(Camera mainCam)
    {
        if (!split.active) return;

        PortalGateway entry = split.entry;
        PortalGateway exit = split.exit;
        PlayerController player = split.player;
        var camMode = player != null ? player.CameraModeController : null;
        Transform camT = mainCam != null ? mainCam.transform : (player != null ? player.CameraTransform : null);

        if (camT != null && entry != null && exit != null)
        {
            Vector3 inLocal = entry.transform.InverseTransformPoint(camT.position);
            float maxW = Mathf.Max(0.35f, entry.defaultScale.x * 0.45f);
            float maxH = Mathf.Max(0.5f, entry.defaultScale.y * 0.48f);
            inLocal.x = Mathf.Clamp(inLocal.x, -maxW, maxW);
            inLocal.y = Mathf.Clamp(inLocal.y, -maxH, maxH);

            Vector3 outLocal = MapLocal(inLocal, split.mirror);
            float minOutZ = Mathf.Max(FrontCameraClearance, exit.exitClearance);
            if (outLocal.z * split.exitSide < minOutZ)
                outLocal.z = split.exitSide * minOutZ;

            Vector3 targetCamPos = exit.transform.TransformPoint(outLocal);
            Quaternion targetCamRot = GetRotationDelta(entry, exit, split.mirror) * camT.rotation;

            if (camMode != null)
            {
                isNearAnyPortal = true;
                camMode.SetNearPortalClip(true, mainCam);
                camMode.EndSplit(mainCam);

                float yaw = targetCamRot.eulerAngles.y;
                float pitch = targetCamRot.eulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
                camMode.ForceCameraPose(targetCamPos, targetCamRot, yaw, pitch, cutDamping: false);
            }

            camT.SetPositionAndRotation(targetCamPos, targetCamRot);
        }
        else
        {
            camMode?.EndSplit(mainCam);
        }

        if (player != null)
            player.ClearCameraBasisOffset();

        if (entry != null)
            entry.SetProximityFlatten(0f);

        CleanupSplit();
    }

    private static void CleanupSplit()
    {
        if (!string.IsNullOrEmpty(split.pinnedEntryScene))
            SceneStreamer.Instance?.UnpinScene(split.pinnedEntryScene);
        if (!string.IsNullOrEmpty(split.pinnedExitScene))
            SceneStreamer.Instance?.UnpinScene(split.pinnedExitScene);

        split = default;
    }

    public static Vector3 MapLocal(Vector3 localPos, bool mirror) => mirror ? HalfTurn * localPos : localPos;

    public static Quaternion GetRotationDelta(PortalGateway from, PortalGateway to, bool mirror)
    {
        Quaternion turn = mirror ? HalfTurn : Quaternion.identity;
        return to.transform.rotation * turn * Quaternion.Inverse(from.transform.rotation);
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

    private void SetProximityFlatten(float t)
    {
        if (runtimeMat == null || !hasBaseMatProps) return;

        t = Mathf.Clamp01(t);
        if (runtimeMat.HasProperty(RippleStrengthId))
            runtimeMat.SetFloat(RippleStrengthId, Mathf.Lerp(baseRipple, 0f, t));
        if (runtimeMat.HasProperty(LensCurvatureId))
            runtimeMat.SetFloat(LensCurvatureId, Mathf.Lerp(baseCurvature, 0.01f, t));
    }

    private Material EnsureMaterial()
    {
        if (portalRenderer == null) return null;

        Shader shader = Shader.Find("Shader Graphs/PortalGateway");
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
            if (runtimeMat.HasProperty(TargetBrightnessId)) baseBrightness = runtimeMat.GetFloat(TargetBrightnessId);
            hasBaseMatProps = true;
        }

        return runtimeMat;
    }

    private void EnsureCamera(Camera sourceCam)
    {
        RenderTextureFormat format = sourceCam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        int width = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelWidth * renderTextureScale));
        int height = Mathf.Max(16, Mathf.RoundToInt(sourceCam.pixelHeight * renderTextureScale));

        if (renderTex == null || renderTex.width != width || renderTex.height != height)
        {
            if (renderTex != null)
            {
                if (portalCamera != null) portalCamera.targetTexture = null;
                renderTex.Release();
                DestroyImmediate(renderTex);
            }

            renderTex = new RenderTexture(width, height, 24, format)
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
            portalCamera.cameraType = CameraType.Reflection;
            cameraData = camObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
        }
    }

    private void ReleaseCamera()
    {
        if (portalCamera != null)
        {
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

    private void Register()
    {
        if (!string.IsNullOrEmpty(portalId))
            PortalsById[portalId] = this;

        TryLink();

        foreach (var kvp in PortalsById)
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

    private void Unregister()
    {
        if (!string.IsNullOrEmpty(portalId) && PortalsById.TryGetValue(portalId, out var existing) && existing == this)
            PortalsById.Remove(portalId);

        if (linkedPortal != null && linkedPortal.linkedPortal == this)
            linkedPortal.linkedPortal = null;
    }

    private void TryLink()
    {
        if (string.IsNullOrEmpty(targetPortalId)) return;
        if (PortalsById.TryGetValue(targetPortalId, out var target) && target != null && target != this)
        {
            linkedPortal = target;
            if (target.linkedPortal == null && !string.IsNullOrEmpty(target.targetPortalId) && target.targetPortalId == portalId)
                target.linkedPortal = this;
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
