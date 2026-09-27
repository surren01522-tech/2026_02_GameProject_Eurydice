using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SettingWindow : UIPanel
{
    [Serializable]
    public struct MenuEntry
    {
        public string title;
        public Button button;
        public GameObject panel;
    }

    [Header("인디케이터")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private float moveSpeed = 20f;

    [Header("메뉴 항목")]
    [SerializeField] private List<MenuEntry> menuEntries = new List<MenuEntry>();

    [Header("사운드")]
    [SerializeField] private UnityEvent onNavigateSound;
    [SerializeField] private UnityEvent onSubmitSound;
    [SerializeField] private UnityEvent onCancelSound;

    private int activeEntryIndex = -1;
    private GameObject lastSelected;
    private GameObject lastMenuButton;

    void Awake()
    {
        for (int i = 0; i < menuEntries.Count; i++)
        {
            int index = i;
            var entry = menuEntries[index];
            if (entry.button == null) continue;

            var trigger = entry.button.gameObject.GetComponent<EventTrigger>() ?? entry.button.gameObject.AddComponent<EventTrigger>();
            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener(_ => EventSystem.current?.SetSelectedGameObject(entry.button.gameObject));
            trigger.triggers.Add(hoverEntry);

            entry.button.onClick.AddListener(() => OnEntryClicked(index));
        }
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            if (lastSelected != null && lastSelected.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(lastSelected);
            }
            return;
        }

        if (currentSelected != lastSelected && currentSelected.transform.IsChildOf(transform))
        {
            onNavigateSound?.Invoke();
            lastSelected = currentSelected;
        }

        if (arrow != null && activeEntryIndex == -1 && IsMainMenuButton(currentSelected))
        {
            Vector3 targetLocalPos = CalculateArrowTargetLocalPosition(currentSelected);
            arrow.localPosition = Vector3.Lerp(arrow.localPosition, targetLocalPos, Time.unscaledDeltaTime * moveSpeed);
        }
    }

    public override void Open()
    {
        base.Open();
        CloseAllSubPanels();

        if (menuEntries.Count > 0 && menuEntries[0].button != null)
        {
            GameObject firstButton = menuEntries[0].button.gameObject;
            EventSystem.current?.SetSelectedGameObject(firstButton);
            lastSelected = firstButton;
            lastMenuButton = firstButton;

            if (arrow != null)
            {
                arrow.localPosition = CalculateArrowTargetLocalPosition(firstButton);
            }
        }
    }

    private Vector3 CalculateArrowTargetLocalPosition(GameObject target)
    {
        if (target == null || arrow == null) return arrow != null ? arrow.localPosition : Vector3.zero;

        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null) return arrow.localPosition;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 leftCenterWorld = (corners[0] + corners[1]) * 0.5f;

        float arrowWidth = arrow.rect.width * arrow.lossyScale.x;
        float arrowHeight = arrow.rect.height * arrow.lossyScale.y;

        float pivotToRightEdge = (1f - arrow.pivot.x) * arrowWidth;
        float pivotToCenterY = (arrow.pivot.y - 0.5f) * arrowHeight;

        Vector3 finalWorldPos = leftCenterWorld 
            - targetRect.right * (spacing + pivotToRightEdge) 
            + targetRect.up * pivotToCenterY;

        return arrow.parent.InverseTransformPoint(finalWorldPos);
    }

    private void OnEntryClicked(int index)
    {
        if (index < 0 || index >= menuEntries.Count) return;

        onSubmitSound?.Invoke();

        var entry = menuEntries[index];
        if (entry.panel != null)
        {
            lastMenuButton = entry.button.gameObject;
            OpenSubPanel(index);
        }
        else
        {
            if (!OnBackPressed())
            {
                UiManager.Instance.PopPanel();
            }
        }
    }

    public void OpenSubPanel(int index)
    {
        activeEntryIndex = index;
        for (int i = 0; i < menuEntries.Count; i++)
        {
            if (menuEntries[i].panel != null)
            {
                menuEntries[i].panel.SetActive(i == index);
            }
        }

        var targetPanel = menuEntries[index].panel;
        if (targetPanel != null)
        {
            var firstSelectable = targetPanel.GetComponentInChildren<Selectable>();
            if (firstSelectable != null)
            {
                EventSystem.current?.SetSelectedGameObject(firstSelectable.gameObject);
            }
        }
    }

    public void CloseAllSubPanels()
    {
        activeEntryIndex = -1;
        for (int i = 0; i < menuEntries.Count; i++)
        {
            if (menuEntries[i].panel != null)
            {
                menuEntries[i].panel.SetActive(false);
            }
        }

        if (lastMenuButton != null && lastMenuButton.activeInHierarchy)
        {
            EventSystem.current?.SetSelectedGameObject(lastMenuButton);
        }
    }

    public override bool OnBackPressed()
    {
        onCancelSound?.Invoke();

        if (activeEntryIndex >= 0)
        {
            CloseAllSubPanels();
            return true;
        }

        return false;
    }

    private bool IsMainMenuButton(GameObject go)
    {
        for (int i = 0; i < menuEntries.Count; i++)
        {
            if (menuEntries[i].button != null && menuEntries[i].button.gameObject == go)
            {
                return true;
            }
        }
        return false;
    }
}
