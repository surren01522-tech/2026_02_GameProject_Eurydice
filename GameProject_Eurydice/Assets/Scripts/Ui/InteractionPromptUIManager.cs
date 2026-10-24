using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class InteractionPromptUIManager : MonoBehaviour
{
    [Header("Fixed HUD UI")]
    [SerializeField] private GameObject fixedPanel;
    [SerializeField] private TextMeshProUGUI fixedKeyText;
    [SerializeField] private TextMeshProUGUI fixedDescriptionText;

    [Header("Floating Target UI")]
    [SerializeField] private GameObject floatingPanel;
    [SerializeField] private TextMeshProUGUI floatingKeyText;
    [SerializeField] private TextMeshProUGUI floatingDescriptionText;

    [Header("키 표시 형식")]
    [SerializeField] private string keyFormat = "[{key}]";

    private PlayerInteraction playerInteraction;
    private Camera mainCam;
    private IInteractable currentTarget;

    private void Awake()
    {
        mainCam = Camera.main;
        HideAll();
    }

    private void Start()
    {
        if (playerInteraction == null)
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (playerInteraction != null)
            playerInteraction.OnTargetChanged += UpdatePrompt;
    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
            playerInteraction.OnTargetChanged -= UpdatePrompt;
    }

    private void LateUpdate()
    {
        if (currentTarget == null || currentTarget.DisplayMode != InteractionDisplayMode.Floating) return;

        if (currentTarget is Object uObj && uObj == null)
        {
            currentTarget = null;
            if (floatingPanel != null) floatingPanel.SetActive(false);
            return;
        }

        if (!currentTarget.CanInteract)
        {
            if (floatingPanel != null && floatingPanel.activeSelf) floatingPanel.SetActive(false);
            return;
        }

        UITweenExtensions.TryUpdateWorldToScreen(mainCam, currentTarget.TargetTransform, currentTarget.WorldOffset, floatingPanel);
    }

    private void UpdatePrompt(IInteractable target)
    {
        bool isNewTarget = currentTarget != target;
        currentTarget = target;

        if (target == null || !target.CanInteract)
        {
            HideAll();
            return;
        }

        string rawKey = GetKeyString();
        string formattedKey = string.IsNullOrEmpty(keyFormat) ? rawKey : keyFormat.Replace("{key}", rawKey);
        string description = target.InteractionPrompt;

        if (target.DisplayMode == InteractionDisplayMode.Fixed)
        {
            if (floatingPanel != null) floatingPanel.SetActive(false);

            if (fixedKeyText != null) fixedKeyText.text = formattedKey;
            if (fixedDescriptionText != null) fixedDescriptionText.text = description;

            if (fixedPanel != null && (isNewTarget || !fixedPanel.activeSelf))
            {
                fixedPanel.SetActive(true);
                fixedPanel.transform.PlayPopIn(0.18f, 0.88f);
            }
        }
        else if (target.DisplayMode == InteractionDisplayMode.Floating)
        {
            if (fixedPanel != null) fixedPanel.SetActive(false);

            if (floatingKeyText != null) floatingKeyText.text = formattedKey;
            if (floatingDescriptionText != null) floatingDescriptionText.text = description;

            if (floatingPanel != null)
            {
                if (UITweenExtensions.TryUpdateWorldToScreen(mainCam, target.TargetTransform, target.WorldOffset, floatingPanel))
                {
                    if (isNewTarget || !floatingPanel.activeSelf)
                    {
                        floatingPanel.SetActive(true);
                        floatingPanel.transform.PlayPopIn(0.18f, 0.88f);
                    }
                }
            }
        }
    }

    private void HideAll()
    {
        if (fixedPanel != null)
        {
            fixedPanel.transform.DOKill();
            fixedPanel.SetActive(false);
        }

        if (floatingPanel != null)
        {
            floatingPanel.transform.DOKill();
            floatingPanel.SetActive(false);
        }
    }

    private string GetKeyString()
    {
        if (playerInteraction != null)
        {
            var player = playerInteraction.GetComponent<PlayerController>();
            if (player?.InputActions != null)
            {
                var action = player.InputActions.Player.Interact;
                int idx = action.GetBindingIndex(InputBinding.MaskByGroup("Keyboard&Mouse"));
                if (idx >= 0)
                    return action.GetBindingDisplayString(idx, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
        }
        return "E";
    }
}
