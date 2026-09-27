using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIDebugger : MonoBehaviour
{
    private GameObject _lastHovered;

    void Update()
    {
        if (EventSystem.current == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        var pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        GameObject currentHovered = results.Count > 0 ? results[0].gameObject : null;
        if (currentHovered != _lastHovered)
        {
            _lastHovered = currentHovered;
            if (_lastHovered != null)
            {
                Debug.Log($"[UI Hover] {_lastHovered.name}");
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (results.Count > 0)
            {
                Debug.Log($"[UI Click] 레이캐스트 감지된 UI ({results.Count}개):");
                for (int i = 0; i < results.Count; i++)
                {
                    Debug.Log($"  {i + 1}. {results[i].gameObject.name} (깊이: {results[i].depth})");
                }
            }
            else
            {
                Debug.Log("[UI Click] 감지된 UI 없음");
            }
        }
    }
}
