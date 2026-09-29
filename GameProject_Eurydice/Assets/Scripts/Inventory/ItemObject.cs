using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        if (itemData == null || amount <= 0) return;

        if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player"))
        {
            InventoryManager.Instance.AddItem(itemData, amount);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}
