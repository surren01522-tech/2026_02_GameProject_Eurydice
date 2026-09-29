using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingWindow : UIPanel
{
    public static SettingWindow Instance { get; private set; }

    [Serializable]
    public struct MenuEntry
    {
        public string title;
        public Button button;
        public GameObject panel;
    }

    [Header("메인 요소")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button settingButton;

    [Header("인디케이터")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private float moveSpeed = 20f;

    [Header("메뉴 항목")]
    [SerializeField] private List<MenuEntry> menuList = new();

    [Header("사운드")]
    [SerializeField] private UnityEvent onNavigateSound;
    [SerializeField] private UnityEvent onSubmitSound;
    [SerializeField] private UnityEvent onCancelSound;

    private readonly Vector3[] corners = new Vector3[4];
    private int activeEntryIndex = -1;
    private GameObject lastSelected;
    private GameObject lastMenuButton;
    private Coroutine arrowCoroutine;

    public bool IsOpen => settingPanel != null && settingPanel.activeSelf;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (settingPanel == null)
        {
            var panelTransform = transform.Find("Setting Panel");
            if (panelTransform != null) settingPanel = panelTransform.gameObject;
        }

        if (settingButton != null)
        {
            settingButton.onClick.AddListener(OnSettingButtonClicked);
        }

        for (int i = 0; i < menuList.Count; i++)
        {
            var button = menuList[i].button;
            if (button == null) continue;

            BindButtonEvents(button, i);
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(OnSettingButtonClicked);
        }
    }

    private void Update()
    {
        if (!IsOpen || EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    private void OnDisable()
    {
        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }
    }

    public override void Open()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }

        GameStateManager.SetModalActive(true);
        HideAllSubPanels();

        if (arrowCoroutine != null)
        {
            StopCoroutine(arrowCoroutine);
            arrowCoroutine = null;
        }

        if (menuList.Count > 0 && menuList[0].button != null)
        {
            var firstButton = menuList[0].button.gameObject;
            lastSelected = firstButton;
            lastMenuButton = firstButton;
            EventSystem.current?.SetSelectedGameObject(firstButton);

            StartCoroutine(CoInitArrowPosition(firstButton));
        }
    }

    public override void Close()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        GameStateManager.SetModalActive(false);
        HideAllSubPanels();
    }
    
    public override bool OnBackPressed()
    {
        if (activeEntryIndex >= 0)
        {
            onCancelSound?.Invoke();
            CloseAllSubPanels();
            return true;
        }

        if (IsOpen)
        {
            onCancelSound?.Invoke();
            Close();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 지정한 인덱스의 서브패널을 엽니다.
    /// </summary>
    public void OpenSubPanel(int index)
    {
        activeEntryIndex = index;
        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].panel != null)
            {
                menuList[i].panel.SetActive(i == index);
            }
        }

        var targetPanel = menuList[index].panel;
        if (targetPanel != null)
        {
            var firstSelectable = targetPanel.GetComponentInChildren<Selectable>();
            if (firstSelectable != null)
            {
                EventSystem.current?.SetSelectedGameObject(firstSelectable.gameObject);
                lastSelected = firstSelectable.gameObject;
            }
        }
    }

    /// <summary>
    /// 모든 서브패널을 닫고 이전 메뉴 버튼으로 포커스를 복구합니다.
    /// </summary>
    public void CloseAllSubPanels()
    {
        HideAllSubPanels();

        if (lastMenuButton != null && lastMenuButton.activeInHierarchy)
        {
            EventSystem.current?.SetSelectedGameObject(lastMenuButton);
            lastSelected = lastMenuButton;
            MoveArrowTo(lastMenuButton);
        }
    }

    /// <summary>
    /// 포커스 이동 없이 서브패널들만 단순 비활성화합니다.
    /// </summary>
    private void HideAllSubPanels()
    {
        activeEntryIndex = -1;
        for (int i = 0; i < menuList.Count; i++)
        {
            if (menuList[i].panel != null)
            {
                menuList[i].panel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 패널 활성화 직후 레이아웃을 강제 갱신하고 화살표 위치를 즉시 동기화합니다.
    /// </summary>
    private IEnumerator CoInitArrowPosition(GameObject target)
    {
        RebuildLayouts();
        if (arrow != null && target != null)
        {
            arrow.localPosition = GetArrowPosition(target);
        }

        yield return null;

        RebuildLayouts();
        if (arrow != null && target != null)
        {
            arrow.localPosition = GetArrowPosition(target);
        }
    }

    /// <summary>
    /// 패널 하위의 모든 LayoutGroup을 강제로 즉시 재계산합니다.
    /// </summary>
    private void RebuildLayouts()
    {
        if (settingPanel == null) return;

        var layoutGroups = settingPanel.GetComponentsInChildren<LayoutGroup>(true);
        for (int i = 0; i < layoutGroups.Length; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroups[i].GetComponent<RectTransform>());
        }

        Canvas.ForceUpdateCanvases();
    }

    private void OnSettingButtonClicked()
    {
        if (IsOpen) Close();
        else Open();
    }

    /// <summary>
    /// 메뉴 버튼 이벤트(마우스 호버, 선택, 클릭)를 일괄 등록합니다.
    /// </summary>
    private void BindButtonEvents(Button button, int index)
    {
        var trigger = button.gameObject.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => EventSystem.current?.SetSelectedGameObject(button.gameObject));
        trigger.triggers.Add(enterEntry);

        var selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback.AddListener(_ => SelectMenu(index, true));
        trigger.triggers.Add(selectEntry);

        button.onClick.AddListener(() => OnEntryClicked(index));
    }

    /// <summary>
    /// 지정한 메뉴 항목을 선택 처리하고 화살표를 이동합니다.
    /// </summary>
    private void SelectMenu(int index, bool playSound)
    {
        if (index < 0 || index >= menuList.Count) return;
        var buttonGo = menuList[index].button.gameObject;

        if (lastSelected != buttonGo)
        {
            if (playSound) onNavigateSound?.Invoke();
            lastSelected = buttonGo;
            lastMenuButton = buttonGo;
            MoveArrowTo(buttonGo);
        }
    }

    /// <summary>
    /// 설정 메뉴 버튼 클릭 이벤트를 처리합니다.
    /// </summary>
    private void OnEntryClicked(int index)
    {
        if (index < 0 || index >= menuList.Count) return;

        var entry = menuList[index];
        if (entry.panel != null)
        {
            if (activeEntryIndex == index)
            {
                onCancelSound?.Invoke();
                CloseAllSubPanels();
                return;
            }

            onSubmitSound?.Invoke();
            lastMenuButton = entry.button.gameObject;
            MoveArrowTo(lastMenuButton);
            OpenSubPanel(index);
        }
        else
        {
            onSubmitSound?.Invoke();
            Close();
        }
    }

    private void MoveArrowTo(GameObject target)
    {
        if (arrow == null || target == null) return;

        if (arrowCoroutine != null) StopCoroutine(arrowCoroutine);
        arrowCoroutine = StartCoroutine(MoveArrow(GetArrowPosition(target)));
    }

    private IEnumerator MoveArrow(Vector3 targetLocalPos)
    {
        while (arrow != null && Vector3.Distance(arrow.localPosition, targetLocalPos) > 0.1f)
        {
            arrow.localPosition = Vector3.Lerp(arrow.localPosition, targetLocalPos, Time.unscaledDeltaTime * moveSpeed);
            yield return null;
        }

        if (arrow != null) arrow.localPosition = targetLocalPos;
        arrowCoroutine = null;
    }

    /// <summary>
    /// 대상 UI 오브젝트 기준 화살표의 로컬 좌표를 계산합니다.
    /// </summary>
    private Vector3 GetArrowPosition(GameObject target)
    {
        if (target == null || arrow == null) return Vector3.zero;

        var targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null) return arrow.localPosition;

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
}
