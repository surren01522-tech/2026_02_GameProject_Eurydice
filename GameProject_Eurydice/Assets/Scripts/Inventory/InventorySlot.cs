using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int count;

    public InventorySlot(ItemData itemData, int count)
    {
        this.itemData = itemData;
        this.count = count;
    }
}
