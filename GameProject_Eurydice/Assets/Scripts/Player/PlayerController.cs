using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Normal,
    Pickup,
}

public enum ControlMode
{
    FreeLook,
    Strafe,
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraModeController cameraModeController;

    [Header("카메라 모드")]
    [SerializeField] private ControlMode controlMode = ControlMode.FreeLook;

    [Header("이동 설정")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("점프 설정")]
    [SerializeField] private float jumpPower = 2f;

    [Header("바닥 설정")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float maxFallSpeed = -25f;
    [SerializeField] private float maxFrameDeltaTime = 0.1f;
    [SerializeField] private float voidRescueDropDistance = 15f;

    private const float MaxSubStepDelta = 0.02f;
    private const float GroundSearchDistance = 500f;

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private Vector3 moveVelocity;
    private float verticalVelocity;
    private Quaternion cameraBasisOffset = Quaternion.identity;
    private PlayerState currentState = PlayerState.Normal;
    private CameraZoom cameraZoom;
    private readonly RaycastHit[] groundHitBuffer = new RaycastHit[16];
    private Vector3 lastSafeGroundedPos;
    private Quaternion lastSafeGroundedRot = Quaternion.identity;
    private bool hasSafeGroundedPos;

    public InputSystem_Actions InputActions => inputActions;
    public Transform CameraTransform => cameraTransform;
    public CameraModeController CameraModeController => cameraModeController;

    private Quaternion EffectiveCameraRotation => cameraTransform != null ? cameraBasisOffset * cameraTransform.rotation : transform.rotation;

    public void SetCameraBasisOffset(Quaternion offset) => cameraBasisOffset = offset;
    public void ClearCameraBasisOffset() => cameraBasisOffset = Quaternion.identity;

    public void Warp(Vector3 targetPosition, Quaternion targetRotation)
    {
        bool isLargeJump = (targetPosition - transform.position).sqrMagnitude > 4f;
        Quaternion rotDelta = Quaternion.Euler(0f, targetRotation.eulerAngles.y - transform.eulerAngles.y, 0f);
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(targetPosition, Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f));
        moveVelocity = rotDelta * moveVelocity;
        if (verticalVelocity < -2f) verticalVelocity = -2f;
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;

        if (isLargeJump)
        {
            hasSafeGroundedPos = false;
            if (HasGroundBelow(transform.position))
                RecordSafePosition(transform.position, transform.rotation);
        }
    }

    public void Teleport(Vector3 targetPosition, Quaternion targetRotation, Vector3 targetCameraPosition, Quaternion targetCameraRotation)
    {
        cameraBasisOffset = Quaternion.identity;
        verticalVelocity = 0f;
        moveVelocity = Vector3.zero;
        Warp(targetPosition, targetRotation);

        float camYaw = targetCameraRotation.eulerAngles.y;
        float camPitch = targetCameraRotation.eulerAngles.x;
        if (camPitch > 180f) camPitch -= 360f;

        cameraModeController?.ForceCameraPose(targetCameraPosition, targetCameraRotation, camYaw, camPitch);
        if (cameraTransform != null)
            cameraTransform.SetPositionAndRotation(targetCameraPosition, targetCameraRotation);
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new InputSystem_Actions();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraModeController == null)
            cameraModeController = GetComponent<CameraModeController>();

        cameraZoom = GetComponent<CameraZoom>() ?? FindFirstObjectByType<CameraZoom>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        SettingManager.Instance?.ApplyBindings(inputActions.asset);

        if (SceneStreamer.Instance == null)
        {
            RestorePlayerTransform();
            EnableControl();
        }
        else if (!SceneStreamer.IsReady && controller != null)
        {
            controller.enabled = false;
        }
    }

    public void EnableControl()
    {
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
        if (HasGroundBelow(transform.position) && !hasSafeGroundedPos)
            RecordSafePosition(transform.position, transform.rotation);
    }

    private string ResolveActiveSceneName()
    {
        if (!string.IsNullOrEmpty(SceneEnvironmentManager.ActiveSceneName))
            return SceneEnvironmentManager.ActiveSceneName;
        return gameObject.scene.IsValid() ? gameObject.scene.name : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    public void RestorePlayerTransform()
    {
        string currentScene = ResolveActiveSceneName();
        var savedPlayer = SaveManager.GetPlayerTransform(currentScene);
        verticalVelocity = 0f;
        moveVelocity = Vector3.zero;

        if (savedPlayer != null)
        {
            Vector3 prevPos = transform.position;
            if (controller != null) controller.enabled = false;
            transform.position = savedPlayer.position;
            transform.rotation = Quaternion.Euler(0, savedPlayer.yRotation, 0);
            Physics.SyncTransforms();

            Vector3 delta = savedPlayer.position - prevPos;
            cameraModeController?.WarpCameras(transform, delta);

            controlMode = (ControlMode)savedPlayer.controlMode;
            cameraModeController?.SetCameraMode(controlMode);
            cameraModeController?.SetCameraOrientation(savedPlayer.cameraYaw, savedPlayer.cameraPitch);

            if (cameraTransform != null)
                cameraTransform.rotation = Quaternion.Euler(savedPlayer.cameraPitch, savedPlayer.cameraYaw, 0f);

            if (cameraZoom != null && savedPlayer.cameraZoom > 0f)
                cameraZoom.SetDistance(savedPlayer.cameraZoom, immediate: true);

            EnsureGroundSafetyOnSpawn();
            MinimapManager.Instance?.RevealAtWorldPosition(transform.position, force: true);
        }
        else
        {
            cameraModeController?.SetCameraMode(controlMode);
            EnsureGroundSafetyOnSpawn();
        }
    }

    public void EnsureGroundSafetyOnSpawn()
    {
        Physics.SyncTransforms();
        if (!HasGroundBelow(transform.position) && TryFindSurfaceFromAbove(transform.position, out float surfaceY))
        {
            float halfHeight = controller != null ? Mathf.Max(controller.height * 0.5f, controller.radius) : 1f;
            float centerOffsetY = controller != null ? controller.center.y : 0f;
            Vector3 fixedPos = new Vector3(transform.position.x, surfaceY + halfHeight - centerOffsetY + 0.05f, transform.position.z);
            Debug.LogWarning($"[PlayerController] Adjusted underground/unsupported spawn from {transform.position} to {fixedPos}.");

            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            transform.position = fixedPos;
            Physics.SyncTransforms();
            if (controller != null) controller.enabled = wasEnabled;
        }

        if (HasGroundBelow(transform.position))
            RecordSafePosition(transform.position, transform.rotation);
    }

    public void SavePlayerTransform()
    {
        Vector3 savePos = transform.position;
        Quaternion saveRot = transform.rotation;
        if (!HasGroundBelow(savePos))
        {
            if (!hasSafeGroundedPos) return;
            Debug.LogWarning($"[PlayerController] Saving lastSafeGroundedPos {lastSafeGroundedPos} instead of unsupported position {savePos}.");
            savePos = lastSafeGroundedPos;
            saveRot = lastSafeGroundedRot;
        }

        string sceneName = ResolveActiveSceneName();
        Quaternion camRot = EffectiveCameraRotation;
        float camYaw = camRot.eulerAngles.y;
        float camPitch = camRot.eulerAngles.x;
        float zoomDist = cameraZoom != null ? cameraZoom.CurrentDistance : 5f;

        SaveManager.SavePlayerTransform(
            savePos,
            saveRot.eulerAngles.y,
            sceneName,
            (int)controlMode,
            camYaw,
            camPitch,
            zoomDist
        );
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnBindingsChanged += SyncBindings;
    }

    private void OnDisable()
    {
        if (SettingManager.Instance != null)
            SettingManager.Instance.OnBindingsChanged -= SyncBindings;
        inputActions?.Disable();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SavePlayerTransform();
    }

    private void OnApplicationQuit()
    {
        SavePlayerTransform();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void SyncBindings() => SettingManager.Instance?.ApplyBindings(inputActions.asset);

    private void OnValidate()
    {
        if (Application.isPlaying)
            cameraModeController?.SetCameraMode(controlMode);
    }

    private void Update()
    {
        if (!SceneStreamer.IsReady || SceneLoader.IsLoading || ScreenFader.IsFading || GameStateManager.IsMainMenuActive)
            return;

        if (inputActions != null && inputActions.Player.ToggleView.WasPressedThisFrame())
            SetControlMode(controlMode == ControlMode.FreeLook ? ControlMode.Strafe : ControlMode.FreeLook);

        if (Keyboard.current != null)
        {
            bool isAlt = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
            GameStateManager.SetAltHeld(isAlt);
        }

        if (SceneStreamer.Instance != null && SceneStreamer.Instance.IsUpdating && !HasGroundBelow(transform.position))
        {
            moveVelocity = Vector3.zero;
            verticalVelocity = 0f;
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, maxFrameDeltaTime);

        if (currentState == PlayerState.Normal && GameStateManager.IsGamePlaying)
        {
            HandleMovement(dt);
            HandleJump();
        }
        else
        {
            moveVelocity = Vector3.zero;
        }

        ApplyMovementAndGravity(dt);
        TrackSafeGroundOrRescue();
    }

    private void HandleMovement(float dt)
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        bool isRunning = inputActions.Player.Sprint.IsPressed();
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Quaternion basisRot = EffectiveCameraRotation;

        if (controlMode == ControlMode.FreeLook)
        {
            Vector3 cameraForward = basisRot * Vector3.forward;
            Vector3 cameraRight = basisRot * Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            moveVelocity = moveDirection * currentSpeed;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * dt);
            }
        }
        else // Strafe 모드
        {
            Vector3 camForward = basisRot * Vector3.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(camForward);

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            moveVelocity = moveDirection * currentSpeed;
        }
    }

    private void HandleJump()
    {
        if (controller == null || !controller.enabled) return;

        if (inputActions.Player.Jump.WasPressedThisFrame() && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpPower * -2f * gravity);
    }

    private void ApplyMovementAndGravity(float remaining)
    {
        if (controller == null || !controller.enabled || remaining <= 0f) return;

        while (remaining > 0f)
        {
            float stepDt = Mathf.Min(remaining, MaxSubStepDelta);
            remaining -= stepDt;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity = Mathf.Max(maxFallSpeed, verticalVelocity + gravity * stepDt);

            float preCenterY = transform.position.y + controller.center.y;
            Vector3 delta = (moveVelocity + Vector3.up * verticalVelocity) * stepDt;
            controller.Move(delta);

            if (delta.y < 0f)
                EnforceFloorSweep(preCenterY);
        }
    }

    private void EnforceFloorSweep(float preCenterY)
    {
        GetProbeMetrics(out float probeRadius, out float halfHeight, out float topClearance);
        Vector3 postPos = transform.position;
        float postCenterY = postPos.y + controller.center.y;
        float startY = Mathf.Max(preCenterY, postCenterY) + topClearance;
        float postBottomY = postCenterY - halfHeight;
        float sweepDist = (startY - postBottomY) + controller.skinWidth + 0.05f;

        if (!TryProbeGround(new Vector3(postPos.x, startY, postPos.z), sweepDist, probeRadius, 0.05f, out RaycastHit hit))
            return;

        float surfaceY = startY - hit.distance - probeRadius;
        if (postBottomY < surfaceY - controller.skinWidth - 0.02f)
        {
            float safePosY = surfaceY + halfHeight - controller.center.y + 0.02f;
            controller.enabled = false;
            transform.position = new Vector3(postPos.x, safePosY, postPos.z);
            Physics.SyncTransforms();
            controller.enabled = true;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            controller.Move(Vector3.down * 0.025f);
        }
    }

    private void TrackSafeGroundOrRescue()
    {
        if (controller == null || !controller.enabled) return;

        GetProbeMetrics(out float probeRadius, out float halfHeight, out float topClearance);
        Vector3 pos = transform.position;
        float startY = pos.y + controller.center.y + topClearance;
        bool hasFloor = TryProbeGround(new Vector3(pos.x, startY, pos.z), GroundSearchDistance, probeRadius, 0f, out RaycastHit hit);

        if (controller.isGrounded && hasFloor && hit.normal.y > 0.4f && hit.distance <= halfHeight + 0.6f)
        {
            RecordSafePosition(pos, transform.rotation);
            return;
        }

        if (!hasFloor && hasSafeGroundedPos && pos.y < lastSafeGroundedPos.y - voidRescueDropDistance)
        {
            Debug.LogWarning($"[PlayerController] Player fell without ground collider at {pos}; rescued to {lastSafeGroundedPos}.");
            verticalVelocity = 0f;
            moveVelocity = Vector3.zero;
            Warp(lastSafeGroundedPos, lastSafeGroundedRot);
        }
    }

    private void RecordSafePosition(Vector3 pos, Quaternion rot)
    {
        lastSafeGroundedPos = pos;
        lastSafeGroundedRot = rot;
        hasSafeGroundedPos = true;
    }

    private void GetProbeMetrics(out float probeRadius, out float halfHeight, out float topClearance)
    {
        float r = controller != null ? controller.radius : 0.5f;
        float h = controller != null ? controller.height : 2f;
        probeRadius = Mathf.Max(0.05f, r * 0.75f);
        halfHeight = Mathf.Max(h * 0.5f, r);
        topClearance = Mathf.Max(0.2f, halfHeight - probeRadius);
    }

    private bool HasGroundBelow(Vector3 pos)
    {
        GetProbeMetrics(out float probeRadius, out _, out float topClearance);
        float centerY = pos.y + (controller != null ? controller.center.y : 0f);
        return TryProbeGround(new Vector3(pos.x, centerY + topClearance, pos.z), GroundSearchDistance, probeRadius, 0f, out _);
    }

    private bool TryProbeGround(Vector3 origin, float distance, float probeRadius, float minNormalY, out RaycastHit nearestHit)
    {
        nearestHit = default;
        if (controller == null) return false;
        int count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, groundHitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
        return TrySelectBestHit(count, 0.0001f, minNormalY, highestPoint: false, out nearestHit);
    }

    private bool TryFindSurfaceFromAbove(Vector3 pos, out float surfaceY)
    {
        surfaceY = pos.y;
        int count = Physics.RaycastNonAlloc(new Vector3(pos.x, pos.y + 100f, pos.z), Vector3.down, groundHitBuffer, 200f, ~0, QueryTriggerInteraction.Ignore);
        if (!TrySelectBestHit(count, 0f, 0.25f, highestPoint: true, out RaycastHit hit))
            return false;

        surfaceY = hit.point.y;
        return true;
    }

    private bool TrySelectBestHit(int count, float minDistance, float minNormalY, bool highestPoint, out RaycastHit bestHit)
    {
        bestHit = default;
        float bestMetric = highestPoint ? float.NegativeInfinity : float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHitBuffer[i];
            Collider col = hit.collider;
            if (col == null || col == controller || col.transform.IsChildOf(transform)) continue;
            if (hit.distance <= minDistance || hit.normal.y <= minNormalY) continue;

            float metric = highestPoint ? hit.point.y : hit.distance;
            if (highestPoint ? metric > bestMetric : metric < bestMetric)
            {
                bestMetric = metric;
                bestHit = hit;
                found = true;
            }
        }
        return found;
    }

    public void SetControlMode(ControlMode newMode)
    {
        controlMode = newMode;
        cameraModeController?.SetCameraMode(controlMode);
    }

    public void ChangeState(PlayerState newState)
    {
        currentState = newState;

        if (currentState != PlayerState.Normal && animator != null)
            animator.SetFloat("speed", 0);

        Debug.Log("현재 상태 : " + currentState);
    }
}
