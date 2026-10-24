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
        if (currentTarget == null || currentTarget.DisplayMode != InteractionDisplayMode.Floating || floatingPanel == null) return;

        if (currentTarget is UnityEngine.Object uObj && uObj == null)
        {
            currentTarget = null;
            floatingPanel.SetActive(false);
            return;
        }

        if (!currentTarget.CanInteract)
        {
            if (floatingPanel.activeSelf) floatingPanel.SetActive(false);
            return;
        }

        Transform targetTransform = currentTarget.TargetTransform;
        if (targetTransform == null)
        {
            if (floatingPanel.activeSelf) floatingPanel.SetActive(false);
            return;
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 worldPos = targetTransform.position + currentTarget.WorldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        if (screenPos.z > 0)
        {
            floatingPanel.transform.position = screenPos;
            if (!floatingPanel.activeSelf)
                ShowPanelWithTween(floatingPanel);
        }
        else if (floatingPanel.activeSelf)
        {
            floatingPanel.SetActive(false);
        }
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
                ShowPanelWithTween(fixedPanel);
        }
        else if (target.DisplayMode == InteractionDisplayMode.Floating)
        {
            if (fixedPanel != null) fixedPanel.SetActive(false);

            if (floatingKeyText != null) floatingKeyText.text = formattedKey;
            if (floatingDescriptionText != null) floatingDescriptionText.text = description;

            if (floatingPanel != null && (isNewTarget || !floatingPanel.activeSelf))
                ShowPanelWithTween(floatingPanel);
        }
    }

    private void ShowPanelWithTween(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        panel.transform.DOKill();
        panel.transform.localScale = Vector3.one * 0.88f;
        panel.transform.DOScale(1f, 0.18f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(panel);
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
