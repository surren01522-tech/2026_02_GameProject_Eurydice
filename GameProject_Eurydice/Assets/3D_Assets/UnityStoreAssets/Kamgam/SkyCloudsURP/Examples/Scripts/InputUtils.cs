using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kamgam.SkyClouds
{
    public static class InputUtils 
    {
        public static bool WasLeftMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
        
        public static bool WasLeftMouseButtonReleased()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }
        
        public static bool IsLeftMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
        
        public static bool WasRightMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }
        
        public static bool WasRightMouseButtonReleased()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.rightButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(1);
#endif
        }
        
        public static bool IsRightMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }
        
        public static bool WasMiddleMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }
        
        public static bool WasMiddleMouseButtonReleased()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.middleButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(2);
#endif
        }
        
        public static bool IsMiddleMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.middleButton.isPressed;
#else
            return Input.GetMouseButton(2);
#endif
        }
        
        public static Vector3 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            
            return Mouse.current.position.value;
#else
            return Input.mousePosition;
#endif
        }
        
        public static float GetScrollWheelValue()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.scroll.y.ReadValue();
            
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }
        
        public static float GetMouseAxisX()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.delta.ReadValue().x;
#else
            return Input.GetAxis("Mouse X");
#endif
        }
        
        public static float GetMouseAxisY()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.delta.ReadValue().y;
#else
            return Input.GetAxis("Mouse Y");
#endif
        }
        
        public static bool IsLeftShiftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }
        
        public static bool IsWPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.wKey.isPressed;
#else
            return Input.GetKey(KeyCode.W);
#endif
        }
        
        public static bool IsAPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.aKey.isPressed;
#else
            return Input.GetKey(KeyCode.A);
#endif
        }
        
        public static bool IsSPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.sKey.isPressed;
#else
            return Input.GetKey(KeyCode.S);
#endif
        }
        
        public static bool IsDPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.dKey.isPressed;
#else
            return Input.GetKey(KeyCode.D);
#endif
        }
        
        public static bool IsQPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.qKey.isPressed;
#else
            return Input.GetKey(KeyCode.Q);
#endif
        }
        
        public static bool IsEPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current.eKey.isPressed;
#else
            return Input.GetKey(KeyCode.E);
#endif
        }
    }
}
