using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    [Header("ID 설정")]
    [SerializeField] private string uniqueId;

    [Header("아이템 설정")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("상호작용 UI 설정")]
    [SerializeField] private InteractionDisplayMode displayMode = InteractionDisplayMode.Floating;
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.4f, 0);
    [SerializeField] private string promptFormat = "{name} x{amount}";

    public InteractionDisplayMode DisplayMode => displayMode;
    public Vector3 WorldOffset => worldOffset;
    public Transform TargetTransform => this != null ? transform : null;
    public bool CanInteract => itemData != null && amount > 0;

    public string InteractionPrompt
    {
        get
        {
            if (string.IsNullOrEmpty(promptFormat)) return string.Empty;
            string name = itemData != null ? itemData.itemName : string.Empty;
            return promptFormat
                .Replace("{name}", name)
                .Replace("{amount}", amount.ToString());
        }
    }

    private void Awake()
    {
        if (SaveManager.IsItemPicked(uniqueId, gameObject.scene.name))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
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

        InventoryManager.Instance.AddItem(itemData, amount);
        SaveManager.MarkItemPicked(uniqueId, gameObject.scene.name);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
