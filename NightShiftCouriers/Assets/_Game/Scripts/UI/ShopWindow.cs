using UnityEngine;

// The shop menu. Opened with E at the shop terminal; Esc or "Close" shuts it.
public class ShopWindow : MonoBehaviour
{
    private static bool isOpen;
    private Vector2 scroll;

    public static void Open()
    {
        isOpen = true;
        UIState.MenuOpen = true;
    }

    public static void Close()
    {
        isOpen = false;
        UIState.MenuOpen = false;
    }

    private void Update()
    {
        if (isOpen && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        CampaignManager campaign = CampaignManager.Instance;
        NetworkPlayer local = NetworkPlayer.Local;
        ItemCatalog catalog = GameNetworkConfig.Instance != null ? GameNetworkConfig.Instance.ItemCatalog : null;
        if (campaign == null || local == null || catalog == null)
        {
            Close();
            return;
        }

        Rect area = new Rect(Screen.width / 2f - 260f, 70f, 520f, Screen.height - 140f);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("DEPOT SHOP        Bank: " + campaign.Bank);

        if (!campaign.ShopOpen)
        {
            GUILayout.Label("The shop is closed during a shift.");
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < catalog.items.Length; i++)
            {
                ItemData item = catalog.items[i];
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(item.displayName + "  -  " + item.price + "\n" + item.description, GUILayout.Width(360f));
                GUI.enabled = campaign.Bank >= item.price;
                if (GUILayout.Button("Buy", GUILayout.Width(100f), GUILayout.Height(40f)))
                {
                    local.Inventory.RequestBuy(i);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        if (GUILayout.Button("Close (Esc)"))
        {
            Close();
        }
        GUILayout.EndArea();
    }
}
