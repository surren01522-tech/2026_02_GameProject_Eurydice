using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingKeyRebindSlot : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TextMeshProUGUI actionNameText;
    [SerializeField] private TextMeshProUGUI keyNameText;
    [SerializeField] private Button rebindButton;

    [Header("설정 정보")]
    [SerializeField] private string actionName = "";
    [SerializeField] private string compositePart = "";
    [SerializeField] private int bindingIndex = -1;

    private InputAction targetAction;
    private bool isRebinding;

    public string ActionName => actionName;
    public int BindingIndex => bindingIndex;

    private void Awake()
    {
        AutoFind();
        rebindButton?.onClick.AddListener(StartRebinding);
    }

    private void Reset() => AutoFind();

    public void Init(InputAction action, string targetCompositePart, string displayName)
    {
        targetAction = action;
        compositePart = targetCompositePart ?? "";
        actionName = displayName;
        bindingIndex = FindBindingIndex(action, compositePart);

        if (actionNameText != null) actionNameText.text = displayName;
        RefreshDisplay();
    }

    public void Init(InputAction action, int bindIndex, string displayName)
    {
        targetAction = action;
        bindingIndex = bindIndex;
        actionName = displayName;

        if (actionNameText != null) actionNameText.text = displayName;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (keyNameText == null) return;
        if (targetAction == null)
        {
            keyNameText.text = "-";
            return;
        }

        if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
            bindingIndex = FindBindingIndex(targetAction, compositePart);

        if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
        {
            keyNameText.text = "-";
            return;
        }

        string display = targetAction.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (string.IsNullOrEmpty(display))
            display = targetAction.bindings[bindingIndex].ToDisplayString();

        keyNameText.text = string.IsNullOrEmpty(display) ? "-" : display;
    }

    public void StartRebinding()
    {
        if (isRebinding || targetAction == null || SettingManager.Instance == null) return;

        if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count)
            bindingIndex = FindBindingIndex(targetAction, compositePart);

        if (bindingIndex < 0 || bindingIndex >= targetAction.bindings.Count) return;

        isRebinding = true;
        if (keyNameText != null) keyNameText.text = "[ Input Waitng... ]";
        if (rebindButton != null) rebindButton.interactable = false;

        SettingManager.Instance.Rebind(
            targetAction,
            bindingIndex,
            onComplete: () =>
            {
                isRebinding = false;
                if (rebindButton != null) rebindButton.interactable = true;
                RefreshDisplay();
            },
            onCancel: () =>
            {
                isRebinding = false;
                if (rebindButton != null) rebindButton.interactable = true;
                RefreshDisplay();
            }
        );
    }

    private int FindBindingIndex(InputAction action, string part)
    {
        if (action == null) return -1;

        var bindings = action.bindings;
        bool hasPart = !string.IsNullOrEmpty(part);

        if (hasPart)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite && b.name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        else
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (!b.isComposite && !b.isPartOfComposite)
                {
                    if (b.groups != null && b.groups.Contains("Keyboard"))
                        return i;
                    if (b.effectivePath != null && b.effectivePath.Contains("Keyboard"))
                        return i;
                }
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (!b.isComposite && !b.isPartOfComposite)
                    return i;
            }
        }

        return -1;
    }

    private void AutoFind()
    {
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            string tName = t.gameObject.name.ToLower();
            if (actionNameText == null && (tName.Contains("action") || tName.Contains("title") || tName.Contains("label") || tName.Contains("name")))
                actionNameText = t;
            else if (keyNameText == null && (tName.Contains("key") || tName.Contains("bind") || tName.Contains("value") || tName.Contains("button")))
                keyNameText = t;
        }

        if (rebindButton == null)
            rebindButton = GetComponentInChildren<Button>(true);
    }
}
