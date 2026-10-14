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
    [SerializeField] private AudioClip retrieveSound;

    [Header("상호작용 설정")]
    [SerializeField] private string promptFormat = "Place {name}";
    [SerializeField] private string retrievePromptFormat = "Take {name}";
    [SerializeField] private InteractionDisplayMode displayMode = InteractionDisplayMode.Floating;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.4f, 0);

    [Header("퍼즐 진입 모드 설정")]
    [SerializeField] private bool requirePuzzleMode = false;
    [SerializeField] private PuzzleBase parentPuzzle;

    [Header("상태")]
    [SerializeField] private bool isPlaced = false;
    [SerializeField] private ItemData placedItem;
    [SerializeField] private int placedAmount = 0;

    public ItemData PlacedItem => placedItem;
    public int PlacedAmount => placedAmount;
    public bool IsPlaced => isPlaced && placedItem != null;
    public bool IsCorrect => IsPlaced && placedItem == requiredItem && placedAmount >= requiredAmount;
    public ItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;

    public event Action<ItemSocket> OnPlaced;
    public UnityEvent onPlacedEvent;

    public string InteractionPrompt
    {
        get
        {
            if (isPlaced && placedItem != null)
            {
                if (string.IsNullOrEmpty(retrievePromptFormat)) return string.Empty;
                return retrievePromptFormat.Replace("{name}", placedItem.itemName).Replace("{amount}", placedAmount.ToString());
            }

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
            if (IsCorrect || IsLocked || requiredItem == null) return false;
            if (requirePuzzleMode && !GameStateManager.IsPuzzleActive) return false;
            return true;
        }
    }

    private void Awake()
    {
        var data = SaveManager.GetSocketData(uniqueId, gameObject.scene.name);
        if (data != null && data.isPlaced && !string.IsNullOrEmpty(data.placedItemId))
        {
            placedItem = InventoryManager.Instance != null
                ? InventoryManager.Instance.GetItemData(data.placedItemId)
                : Resources.Load<ItemData>($"Items/{data.placedItemId}");
            placedAmount = data.placedAmount > 0 ? data.placedAmount : requiredAmount;
            isPlaced = placedItem != null;
        }
        else
        {
            isPlaced = false;
            placedItem = null;
            placedAmount = 0;
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

        if (isPlaced)
        {
            RetrieveItem();
        }
        else if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.OpenForSelection(this);
        }
    }

    public bool InsertItem(ItemData item)
    {
        if (!CanInteract || isPlaced || item == null) return false;
        if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(item, requiredAmount)) return false;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RemoveItem(item, requiredAmount);

        Place(item, requiredAmount);
        return true;
    }

    public void Place(ItemData item, int amount = -1)
    {
        if (item == null) return;
        int count = amount > 0 ? amount : requiredAmount;

        isPlaced = true;
        placedItem = item;
        placedAmount = count;
        SaveManager.SaveSocketState(uniqueId, true, item.id, count, gameObject.scene.name);

        if (placedVisual != null)
            placedVisual.SetActive(true);

        if (placeFX != null)
            placeFX.Play();

        if (placeSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(placeSound, transform.position);

        OnPlaced?.Invoke(this);
        onPlacedEvent?.Invoke();
    }

    public void RetrieveItem()
    {
        if (!isPlaced || IsCorrect || placedItem == null) return;

        ItemData removedItem = placedItem;
        int count = placedAmount > 0 ? placedAmount : requiredAmount;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(removedItem, count);

        isPlaced = false;
        placedItem = null;
        placedAmount = 0;
        SaveManager.SaveSocketState(uniqueId, false, null, 0, gameObject.scene.name);

        if (placedVisual != null)
            placedVisual.SetActive(false);

        if (retrieveSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(retrieveSound, transform.position);

        OnPlaced?.Invoke(this);
        onPlacedEvent?.Invoke();
    }
}
