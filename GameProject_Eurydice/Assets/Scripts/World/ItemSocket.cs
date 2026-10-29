using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class ItemSocket : MonoBehaviour, IInteractable
{
    public static event Action<ItemSocket, ItemData> OnAnySocketPlaced;

    [Header("ID 설정")]
    [SerializeField] private string uniqueId;

    [FoldGroup("요구 아이템 설정")]
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;

    [FoldGroup("효과 설정")]
    [SerializeField] private GameObject placedVisual;
    [SerializeField] private ParticleSystem placeFX;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip retrieveSound;

    [FoldGroup("상호작용 설정")]
    [Tooltip("체크 시 인벤토리 창 없이 인벤토리에 요구 부품이 있으면 즉시 자동 장착합니다. (퍼즐 조각 등 특수 소켓용)")]
    [SerializeField] private bool autoPlaceFromInventory = false;
    [Tooltip("장착 후 아이템을 다시 회수할 수 있는지 여부")]
    [SerializeField] private bool allowRetrieve = true;
    [SerializeField] private string missingItemPromptFormat = "{name}이(가) 필요합니다.";
    [SerializeField] private string promptFormat = "Place {name}";
    [SerializeField] private string retrievePromptFormat = "Take {name}";
    [SerializeField] private InteractionDisplayMode displayMode = InteractionDisplayMode.Floating;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.4f, 0);

    [FoldGroup("퍼즐 진입 모드 설정")]
    [SerializeField] private bool requirePuzzleMode = false;
    [SerializeField] private PuzzleBase parentPuzzle;

    [FoldGroup("상태")]
    [SerializeField] private bool isPlaced = false;
    [SerializeField] private ItemData placedItem;
    [SerializeField] private int placedAmount = 0;

    public string UniqueId => uniqueId;
    public ItemData PlacedItem => placedItem;
    public int PlacedAmount => placedAmount;
    public bool IsPlaced => isPlaced && placedItem != null;
    public bool IsCorrect => IsPlaced && requiredItem != null && (placedItem == requiredItem || (placedItem != null && placedItem.id == requiredItem.id)) && placedAmount >= requiredAmount;
    public ItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;

    public event Action<ItemSocket> OnPlaced;
    public UnityEvent onPlacedEvent;

    private Vector3 defaultVisualScale = Vector3.one;

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

    public bool AutoPlaceFromInventory => autoPlaceFromInventory;
    public bool AllowRetrieve => allowRetrieve;

    public bool CanInteract
    {
        get
        {
            if (isPlaced && !allowRetrieve) return false;
            if (IsCorrect || IsLocked || requiredItem == null) return false;
            if (requirePuzzleMode && !GameStateManager.IsPuzzleActive) return false;
            return true;
        }
    }

    private void Awake()
    {
        if (placedVisual != null)
            defaultVisualScale = placedVisual.transform.localScale;

        RestoreFromSave();

        if (parentPuzzle == null)
            parentPuzzle = GetComponentInParent<PuzzleBase>();

        if (parentPuzzle != null)
            requirePuzzleMode = true;

        if (string.IsNullOrEmpty(uniqueId)) Debug.LogWarning($"{gameObject.name} | 소켓의 Unique Id가 설정되지 않았습니다.");
    }

    private void Start()
    {
        RestoreFromSave();
    }

    private void RestoreFromSave()
    {
        var data = SaveManager.GetSocketData(uniqueId, gameObject.scene.name);
        if (data != null && data.isPlaced && !string.IsNullOrEmpty(data.placedItemId))
        {
            if (InventoryManager.Instance != null)
                placedItem = InventoryManager.Instance.GetItemData(data.placedItemId);

            if (placedItem == null && requiredItem != null && requiredItem.id == data.placedItemId)
                placedItem = requiredItem;

            if (placedItem == null)
            {
                var allItems = Resources.LoadAll<ItemData>("Items");
                for (int i = 0; i < allItems.Length; i++)
                {
                    if (allItems[i] != null && allItems[i].id == data.placedItemId)
                    {
                        placedItem = allItems[i];
                        break;
                    }
                }
            }

            placedAmount = data.placedAmount > 0 ? data.placedAmount : requiredAmount;
            isPlaced = placedItem != null;
        }

        if (placedVisual != null)
            placedVisual.SetActive(isPlaced);

        if (isPlaced && !allowRetrieve)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract) return;

        if (isPlaced)
        {
            if (allowRetrieve)
                RetrieveItem();
        }
        else if (autoPlaceFromInventory)
        {
            TryAutoPlace();
        }
        else if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.OpenForSelection(this);
        }
    }

    public bool TryAutoPlace()
    {
        if (requiredItem == null) return false;

        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredItem, requiredAmount))
            return InsertItem(requiredItem);

        string itemName = !string.IsNullOrEmpty(requiredItem.itemName) ? requiredItem.itemName : "필요한 부품";
        string msg = !string.IsNullOrEmpty(missingItemPromptFormat)
            ? missingItemPromptFormat.Replace("{name}", itemName).Replace("{amount}", requiredAmount.ToString())
            : $"{itemName}이(가) 필요합니다.";
        InteractionFeedbackUIManager.Instance?.Show(msg);
        return false;
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
        {
            placedVisual.SetActive(true);
            placedVisual.transform.DOKill();
            placedVisual.transform.localScale = defaultVisualScale * 0.2f;
            placedVisual.transform.DOScale(defaultVisualScale, 0.25f)
                .SetEase(Ease.OutBack)
                .SetLink(placedVisual);
        }

        if (placeFX != null)
            placeFX.Play();

        if (placeSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(placeSound, transform.position);

        if (!allowRetrieve)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        OnPlaced?.Invoke(this);
        OnAnySocketPlaced?.Invoke(this, item);
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

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (placedVisual != null)
        {
            placedVisual.transform.DOKill();
            placedVisual.transform.localScale = defaultVisualScale;
            placedVisual.SetActive(false);
        }

        if (retrieveSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAt(retrieveSound, transform.position);

        OnPlaced?.Invoke(this);
        onPlacedEvent?.Invoke();
    }
}
