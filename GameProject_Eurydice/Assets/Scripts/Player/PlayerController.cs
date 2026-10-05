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

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private Vector3 moveVelocity;
    private float verticalVelocity;

    public InputSystem_Actions InputActions => inputActions;

    private PlayerState currentState = PlayerState.Normal;

    private CameraZoom cameraZoom;

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
        RestorePlayerTransform();
    }

    private void RestorePlayerTransform()
    {
        var savedPlayer = SaveManager.GetPlayerTransform();

        if (savedPlayer != null)
        {
            Vector3 prevPos = transform.position;
            if (controller != null) controller.enabled = false;
            transform.position = savedPlayer.position;
            transform.rotation = Quaternion.Euler(0, savedPlayer.yRotation, 0);
            if (controller != null) controller.enabled = true;

            Vector3 delta = savedPlayer.position - prevPos;
            cameraModeController?.WarpCameras(transform, delta);

            controlMode = (ControlMode)savedPlayer.controlMode;
            cameraModeController?.SetCameraMode(controlMode);
            cameraModeController?.SetCameraOrientation(savedPlayer.cameraYaw, savedPlayer.cameraPitch);

            if (cameraTransform != null)
            {
                cameraTransform.rotation = Quaternion.Euler(savedPlayer.cameraPitch, savedPlayer.cameraYaw, 0f);
            }

            if (cameraZoom != null && savedPlayer.cameraZoom > 0f)
            {
                cameraZoom.SetDistance(savedPlayer.cameraZoom, immediate: true);
            }
        }
        else
        {
            cameraModeController?.SetCameraMode(controlMode);
        }
    }

    public void SavePlayerTransform()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        float camYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
        float camPitch = cameraTransform != null ? cameraTransform.eulerAngles.x : 0f;
        float zoomDist = cameraZoom != null ? cameraZoom.CurrentDistance : 5f;

        SaveManager.SavePlayerTransform(
            transform.position,
            transform.eulerAngles.y,
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
        if (inputActions != null && inputActions.Player.ToggleView.WasPressedThisFrame())
        {
            SetControlMode(controlMode == ControlMode.FreeLook ? ControlMode.Strafe : ControlMode.FreeLook);
        }

        if (Keyboard.current != null)
        {
            bool isAlt = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
            GameStateManager.SetAltHeld(isAlt);
        }

        if (currentState == PlayerState.Normal && GameStateManager.IsGamePlaying)
        {
            HandleMovement();
            HandleJump();
        }
        else
        {
            moveVelocity = Vector3.zero;
        }

        ApplyGravity();
    }

    private void HandleMovement()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        input = Vector2.ClampMagnitude(input, 1f);

        bool isRunning = inputActions.Player.Sprint.IsPressed();
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (controlMode == ControlMode.FreeLook)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            moveVelocity = moveDirection * currentSpeed;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else // Strafe 모드
        {                                 
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(camForward);

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            moveVelocity = moveDirection * currentSpeed;
        }

        // Idle, Wlak, Run
        /* float animationSpeed = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            animationSpeed = isRunning ? 1f : 0.5f;
        }

        animator.SetFloat("speed", animationSpeed, 0.1f, Time.deltaTime);
        */
    }

    private void HandleJump()
    {
        if (inputActions.Player.Jump.WasPressedThisFrame() && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpPower * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMovement = moveVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);
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
        {
            animator.SetFloat("speed", 0);
        }

        Debug.Log("현재 상태 : " + currentState);
    }
}