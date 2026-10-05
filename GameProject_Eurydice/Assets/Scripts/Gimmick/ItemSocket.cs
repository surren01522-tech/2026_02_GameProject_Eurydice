using System;
using UnityEngine;
using UnityEngine.Events;

public class ItemSocket : MonoBehaviour, IInteractable
{
    [Header("ID 설정")]
    [SerializeField] private string uniqueId;

    [Header("요구 아이템 설정")]
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;

    [Header("효과 설정")]
    [SerializeField] private GameObject placedVisual;
    [SerializeField] private ParticleSystem placeFX;
    [SerializeField] private AudioClip placeSound;

    [Header("상호작용 설정")]
    [SerializeField] private string promptFormat = "Place {name}";
    [SerializeField] private InteractionDisplayMode displayMode = InteractionDisplayMode.Floating;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.4f, 0);

    [Header("실패 피드백 설정")]
    [SerializeField] private bool useFeedbackUI = true;
    [SerializeField] private FeedbackDisplayMode feedbackDisplayMode = FeedbackDisplayMode.Floating;
    [SerializeField] private Vector3 feedbackOffset = new Vector3(0, 0.8f, 0);
    [SerializeField] private string missingItemMessage = "Missing {name}";
    [SerializeField] private string wrongItemMessage = "This item does not fit here";
    [SerializeField] private AudioClip failSound;

    [Header("퍼즐 진입 모드 설정")]
    [SerializeField] private bool requirePuzzleMode = false;
    [SerializeField] private PuzzleBase parentPuzzle;

    [Header("상태")]
    [SerializeField] private bool isPlaced = false;

    public bool IsPlaced => isPlaced;
    public ItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;

    public event Action<ItemSocket> OnPlaced;
    public UnityEvent onPlacedEvent;

    public string InteractionPrompt
    {
        get
        {
            if (string.IsNullOrEmpty(promptFormat)) return string.Empty;
            string itemName = requiredItem != null ? requiredItem.itemName : string.Empty;
            return promptFormat.Replace("{name}", itemName).Replace("{amount}", requiredAmount.ToString());
        }
    }

    public InteractionDisplayMode DisplayMode => displayMode;
    public Vector3 WorldOffset => worldOffset;
    public Transform TargetTransform => this != null ? transform : null;
    public bool IsLocked { get; set; }

    public bool CanInteract
    {
        get
        {
            if (isPlaced || IsLocked || requiredItem == null) return false;
            if (requirePuzzleMode && !GameStateManager.IsPuzzleActive) return false;
            return true;
        }
    }

    private void Awake()
    {
        if (SaveManager.GetSocketPlaced(uniqueId))
        {
            isPlaced = true;
        }

        if (placedVisual != null)
            placedVisual.SetActive(isPlaced);

        if (parentPuzzle == null)
            parentPuzzle = GetComponentInParent<PuzzleBase>();

        if (parentPuzzle != null)
            requirePuzzleMode = true;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract) return;

        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.OpenForSelection(this);
        }
    }

    public bool TryInsertItem(ItemData item)
    {
        if (!CanInteract) return false;

        if (item == requiredItem)
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.RemoveItem(requiredItem, requiredAmount);
            Place();
            return true;
        }

        TriggerWrongFeedback();
        return false;
    }

    public void TriggerMissingFeedback()
    {
        PlayFailSound();
        if (useFeedbackUI && InteractionFeedbackUI.Instance != null)
        {
            string itemName = requiredItem != null ? requiredItem.itemName : string.Empty;
            string msg = missingItemMessage.Replace("{name}", itemName);
            InteractionFeedbackUI.Instance.Show(msg, feedbackDisplayMode, transform, feedbackOffset);
        }
    }

    public void TriggerWrongFeedback()
    {
        PlayFailSound();
        if (useFeedbackUI && InteractionFeedbackUI.Instance != null)
        {
            InteractionFeedbackUI.Instance.Show(wrongItemMessage, feedbackDisplayMode, transform, feedbackOffset);
        }
    }

    private void PlayFailSound()
    {
        if (failSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(failSound, transform.position);
    }

    public void Place()
    {
        if (isPlaced) return;

        isPlaced = true;
        SaveManager.SaveSocketPlaced(uniqueId, true);

        if (placedVisual != null)
            placedVisual.SetActive(true);

        if (placeFX != null)
            placeFX.Play();

        if (placeSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(placeSound, transform.position);

        OnPlaced?.Invoke(this);
        onPlacedEvent?.Invoke();
    }
}
