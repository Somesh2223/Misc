using UnityEngine;
using UnityEngine.InputSystem;

// The courier's journal (plan 4.8): tonight's orders with each customer's note, and the lore found so far.
// J opens and closes it.
public class JournalWindow : MonoBehaviour
{
    private bool isOpen;
    private bool showLore;
    private Vector2 scroll;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }
        bool toggle = Keyboard.current.jKey.wasPressedThisFrame;
        bool close = isOpen && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (toggle && !isOpen && UIState.MenuOpen)
        {
            return; // another menu is open
        }
        if (toggle || close)
        {
            isOpen = !isOpen && !close;
            UIState.MenuOpen = isOpen;
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GameNetworkConfig config = GameNetworkConfig.Instance;
        if (config == null)
        {
            return;
        }

        Rect area = new Rect(Screen.width / 2f - 300f, 60f, 600f, Screen.height - 120f);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Tonight's orders")) showLore = false;
        if (GUILayout.Button("Journal")) showLore = true;
        if (GUILayout.Button("Close (J)"))
        {
            isOpen = false;
            UIState.MenuOpen = false;
        }
        GUILayout.EndHorizontal();

        scroll = GUILayout.BeginScrollView(scroll);
        if (showLore)
        {
            DrawLore(config.LoreBook);
        }
        else
        {
            DrawOrders(config.CustomerDirectory);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawOrders(CustomerDirectory directory)
    {
        DeliveryManager deliveries = DeliveryManager.Instance;
        if (deliveries == null)
        {
            GUILayout.Label("No orders yet. They are handed out when the shift starts.");
            return;
        }

        foreach (string address in deliveries.OrderAddresses)
        {
            CustomerData customer = directory != null ? directory.Find(address) : null;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(address);
            if (customer != null)
            {
                GUILayout.Label("For " + customer.customerName + ". \"" + customer.note + "\"");
            }
            GUILayout.EndVertical();
        }
    }

    private void DrawLore(LoreBook book)
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (book == null || book.entries == null || campaign == null)
        {
            return;
        }

        int found = 0;
        for (int i = 0; i < book.entries.Length; i++)
        {
            if (!campaign.IsLoreUnlocked(i))
            {
                continue;
            }
            found++;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(book.entries[i].title);
            GUILayout.Label(book.entries[i].text);
            GUILayout.EndVertical();
        }
        GUILayout.Label(found + " / " + book.entries.Length + " entries found. Deliver to strange customers and open cursed packages to find more.");
    }
}
