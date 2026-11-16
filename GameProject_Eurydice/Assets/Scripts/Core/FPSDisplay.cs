using UnityEngine;
using UnityEngine.InputSystem;

public class FPSDisplay : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private Key toggleKey = Key.F1;
    [SerializeField] private bool showOnStart = true;

    private float _deltaTime = 0.0f;
    private bool _isVisible;

    void Awake()
    {
        _isVisible = showOnStart;
    }

    void Update()
    {
        if (toggleKey != Key.None && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            _isVisible = !_isVisible;
            if (!_isVisible) _deltaTime = 0.0f;
        }

        if (!_isVisible) return;

        _deltaTime = _deltaTime <= 0.0f
            ? Time.unscaledDeltaTime
            : _deltaTime + (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!_isVisible) return;

        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 10, w, h * 5 / 100);
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = h * 2 / 50; 
        style.normal.textColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

        float msec = _deltaTime * 1000.0f;
        float fps = _deltaTime > 0.0f ? 1.0f / _deltaTime : 0.0f;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        GUI.Label(rect, text, style);
    }
#endif
}
