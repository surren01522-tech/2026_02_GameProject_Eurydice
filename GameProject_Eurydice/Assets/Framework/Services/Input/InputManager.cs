using UnityEngine;
using UnityEngine.InputSystem;
using GameFramework.Core;

namespace GameFramework.Services
{
    /// <summary>
    /// New Input System 기반 입력 매니저. (구 Input Manager 미사용)
    /// 코드로 InputAction을 정의하므로 .inputactions 에셋 없이 바로 동작한다.
    /// - 이동: WASD / 방향키 / 게임패드 왼쪽 스틱
    /// - 점프: Space / 게임패드 South(A)
    /// - 상호작용: E / 게임패드 West(X)
    /// - 뒤로가기: ESC / 게임패드 Start (BackPressedEvent 발행 → UIManager가 구독)
    /// 사용 예)
    ///   Vector2 move = InputManager.Instance.Move;
    ///   if (InputManager.Instance.JumpPressedThisFrame) Jump();
    ///   EventBus.Subscribe<InteractPressedEvent>(OnInteract);
    /// 키 변경: OnInitialize의 바인딩 문자열만 수정하면 된다.
    /// </summary>
    public class InputManager : MonoSingleton<InputManager>
    {
        private InputAction _move;
        private InputAction _jump;
        private InputAction _interact;
        private InputAction _back;

        /// <summary>현재 이동 입력 (정규화된 Vector2)</summary>
        public Vector2 Move => _move?.ReadValue<Vector2>() ?? Vector2.zero;

        public bool JumpPressedThisFrame => _jump != null && _jump.WasPressedThisFrame();
        public bool JumpHeld => _jump != null && _jump.IsPressed();
        public bool InteractPressedThisFrame => _interact != null && _interact.WasPressedThisFrame();

        protected override void OnInitialize()
        {
            // ===== 이동 (2D Vector 컴포지트) =====
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            // ===== 점프 =====
            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");
            _jump.performed += _ => EventBus.Publish(new JumpPressedEvent());

            // ===== 상호작용 =====
            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonWest");
            _interact.performed += _ => EventBus.Publish(new InteractPressedEvent());

            // ===== 뒤로가기 (ESC / 모바일 백버튼 / 패드 Start) =====
            _back = new InputAction("Back", InputActionType.Button);
            _back.AddBinding("<Keyboard>/escape"); // Android 백버튼도 escape로 들어온다
            _back.AddBinding("<Gamepad>/start");
            _back.performed += _ => EventBus.Publish(new BackPressedEvent());

            EnableAll();
        }

        public void EnableAll()
        {
            _move.Enable(); _jump.Enable(); _interact.Enable(); _back.Enable();
        }

        /// <summary>컷씬 등에서 게임플레이 입력만 차단 (뒤로가기는 유지)</summary>
        public void DisableGameplay()
        {
            _move.Disable(); _jump.Disable(); _interact.Disable();
        }

        public void EnableGameplay()
        {
            _move.Enable(); _jump.Enable(); _interact.Enable();
        }

        protected override void OnDestroy()
        {
            _move?.Dispose(); _jump?.Dispose(); _interact?.Dispose(); _back?.Dispose();
            base.OnDestroy();
        }
    }
}
