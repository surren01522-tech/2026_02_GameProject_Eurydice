using UnityEngine;
using UnityEngine.InputSystem;

public class UiManager : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Previous.performed += OnBackPressed;
    }

    void OnDisable()
    {
        inputActions.UI.Previous.performed -= OnBackPressed;
        inputActions.UI.Disable();
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    void OnBackPressed(InputAction.CallbackContext context)
    {
        
    }
}
