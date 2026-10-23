using UnityEngine;

// The four item slots at the bottom of the screen. 1-4 selects, right mouse uses.
public class InventoryHUD : MonoBehaviour
{
    private void OnGUI()
    {
        NetworkPlayer local = NetworkPlayer.Local;
        if (local == null || local.Inventory == null || !local.Inventory.IsSpawned)
        {
            return;
        }

        PlayerInventory inventory = local.Inventory;
        float slotWidth = 130f;
        float slotHeight = 46f;
        float startX = Screen.width / 2f - slotWidth * PlayerInventory.SlotCount / 2f;
        float y = Screen.height - slotHeight - 14f;

        for (int i = 0; i < PlayerInventory.SlotCount; i++)
        {
            Rect box = new Rect(startX + i * slotWidth, y, slotWidth - 6f, slotHeight);
            GUI.color = i == inventory.SelectedSlot ? Color.yellow : Color.white;
            GUI.Box(box, "");
            GUI.color = Color.white;

            ItemData item = inventory.GetItem(i);
            string text = (i + 1) + ": ";
            if (item == null)
            {
                text += "-";
            }
            else
            {
                text += item.displayName;
                if (item.type == ItemType.Flashlight)
                {
                    text += "\n" + inventory.GetUses(i) + " s" + (inventory.FlashlightOn ? " (ON)" : "");
                }
                else if (item.uses > 1)
                {
                    text += "\nx" + inventory.GetUses(i);
                }
            }
            GUI.Label(new Rect(box.x + 6f, box.y + 3f, box.width - 8f, box.height), text);
        }

        if (inventory.IsCharmedAgainst(MonsterKind.Vetala) || inventory.IsCharmedAgainst(MonsterKind.Chudail)
            || inventory.IsCharmedAgainst(MonsterKind.JalPishach))
        {
            GUI.Label(new Rect(startX, y - 22f, 300f, 20f), "Charm active");
        }
    }
}
