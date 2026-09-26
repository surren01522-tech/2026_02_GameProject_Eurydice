using UnityEngine;
using Unity.Cinemachine;
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

    [Header("카메라")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera strafeCamera;

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

    private PlayerState currentState = PlayerState.Normal;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new InputSystem_Actions();
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start() => UpdateCameraState();

    private void OnEnable() => inputActions.Player.Enable();
    private void OnDisable() => inputActions.Player.Disable();
    private void OnDestroy() => inputActions.Dispose();

    private void OnValidate()
    {
        if (Application.isPlaying)
            UpdateCameraState();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            SetControlMode(controlMode == ControlMode.FreeLook ? ControlMode.Strafe : ControlMode.FreeLook);
        }

        if (currentState == PlayerState.Normal)
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
        UpdateCameraState();
    }

    private void UpdateCameraState()
    {
        bool isFreeLook = controlMode == ControlMode.FreeLook;

        if (freeLookCamera != null)
            freeLookCamera.Priority.Value = isFreeLook ? 10 : 0;

        if (strafeCamera != null)
            strafeCamera.Priority.Value = isFreeLook ? 0 : 10;
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