using UnityEngine;

// The list of all items. Items are sent over the network as a number (index into this list).
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Night Shift/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public ItemData[] items;

    public int IndexOf(ItemData data)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == data)
            {
                return i;
            }
        }
        return -1;
    }

    // Returns null for -1 or an out-of-range index.
    public ItemData Get(int index)
    {
        if (items == null || index < 0 || index >= items.Length)
        {
            return null;
        }
        return items[index];
    }
}
