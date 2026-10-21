using UnityEngine;
using UnityEngine.InputSystem;

namespace SeamlessWormhole
{
    /// <summary>
    /// 수업용 최소 1인칭 이동 컴포넌트입니다.
    ///
    /// 장애물이 없는 예제이므로 CharacterController 대신 Transform을 직접 움직입니다.
    /// 충돌체 안에서 시작하거나 중력 때문에 이동을 확인하기 어려운 문제를 피합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WormholeWalker : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkingSpeed = 5f;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;

        private Transform viewCamera;
        private float pitch;
        private float nextTeleportTime;
        private SphericalWormholePortal ignoredPortal;
        private bool legacyInputIsAvailable = true;

        private void Awake()
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            viewCamera = childCamera != null ? childCamera.transform : null;
            LockPointer();
        }

        private void Update()
        {
            UpdatePointerLock();
            UpdateLook();

            Vector3 positionBeforeMove = transform.position;
            Vector2 input = ReadMovementInput();
            if (input.sqrMagnitude > 0f)
            {
                // 위나 아래를 보더라도 시선 높이는 유지합니다. 이 예제에는 충돌체가 없습니다.
                Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
                transform.position += moveDirection.normalized * walkingSpeed * Time.deltaTime;
            }

            TryTeleport(positionBeforeMove);
        }

        private void UpdateLook()
        {
            if (viewCamera == null) return;

            Vector2 mouseDelta = ReadMouseDelta();
            transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -80f, 80f);
            viewCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void TryTeleport(Vector3 positionBeforeMove)
        {
            foreach (SphericalWormholePortal portal in SphericalWormholePortal.ActivePortals)
            {
                if (portal == null) continue;
                if (portal == ignoredPortal && Time.time < nextTeleportTime) continue;

                if (!portal.TryMapCrossing(positionBeforeMove, transform.position, transform.rotation,
                        out Vector3 mappedPosition, out Quaternion mappedRotation))
                    continue;

                transform.SetPositionAndRotation(mappedPosition, mappedRotation);
                // 출구 중심면 바로 옆에서 다시 입구로 되돌아오는 것을 잠시 막습니다.
                // 이후에는 정상적인 양방향 통과가 가능합니다.
                ignoredPortal = portal.LinkedPortal;
                nextTeleportTime = Time.time + 0.20f;
                return;
            }
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = ReadNewInputSystemKeys();
            input += ReadClassicInputManagerKeys();

            // 입력 시스템이 Both이면 같은 키가 두 API에서 모두 들어옵니다.
            // 길이를 제한해 이동 속도를 일정하게 유지합니다.
            return Vector2.ClampMagnitude(input, 1f);
        }

        private static Vector2 ReadNewInputSystemKeys()
        {
            if (Keyboard.current == null) return Vector2.zero;

            float horizontal = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                             - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
            float vertical = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f)
                           - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(horizontal, vertical);
        }

        private Vector2 ReadClassicInputManagerKeys()
        {
            if (!legacyInputIsAvailable) return Vector2.zero;

            try
            {
                float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                                 - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                               - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
                return new Vector2(horizontal, vertical);
            }
            catch (System.InvalidOperationException)
            {
                // New Input System 전용 프로젝트에서도 Keyboard.current 경로는 계속 작동합니다.
                legacyInputIsAvailable = false;
                return Vector2.zero;
            }
        }

        private Vector2 ReadMouseDelta()
        {
            if (Mouse.current != null) return Mouse.current.delta.ReadValue();
            if (!legacyInputIsAvailable) return Vector2.zero;

            try
            {
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            }
            catch (System.InvalidOperationException)
            {
                legacyInputIsAvailable = false;
                return Vector2.zero;
            }
        }

        private void UpdatePointerLock()
        {
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (escapePressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (leftClickPressed) LockPointer();
        }

        private static void LockPointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
