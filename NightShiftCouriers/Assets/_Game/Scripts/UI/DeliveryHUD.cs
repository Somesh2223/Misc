using UnityEngine;

// Debug HUD for deliveries: money this shift, the order list, and where the held package goes.
public class DeliveryHUD : MonoBehaviour
{
    private void OnGUI()
    {
        DeliveryManager manager = DeliveryManager.Instance;
        if (manager == null)
        {
            return;
        }

        string text = "Earned this shift: " + manager.Money + "\nOrders:\n" + manager.OrderSummary;
        GUI.Label(new Rect(20, 110, 480, 200), text);

        NetworkPlayer local = NetworkPlayer.Local;
        PlayerGrabber grabber = local != null ? local.GetComponent<PlayerGrabber>() : null;
        if (grabber != null && grabber.IsHolding)
        {
            Package held = grabber.HeldPackage;
            string hint = "Holding a package";
            if (held != null && held.Data != null)
            {
                if (held.Data.raftPart != RaftPartType.None)
                {
                    hint = "Raft part: " + held.Data.displayName + " (take it to a raft workbench)";
                }
                else if (held.DestinationAddress != "")
                {
                    hint = "Holding package for: " + held.DestinationAddress;
                    CustomerData customer = GameNetworkConfig.Instance.CustomerDirectory != null
                        ? GameNetworkConfig.Instance.CustomerDirectory.Find(held.DestinationAddress) : null;
                    if (customer != null)
                    {
                        hint += "\n\"" + customer.note + "\"";
                    }
                }
                if (held.IsCursed && !held.IsScreaming)
                {
                    hint += "\nIt is warm... and whispering. [F] open it?";
                }
                if (held.Data.holdersRequired > 1)
                {
                    hint += "  (needs 2 players to lift)";
                }
                if (held.IsPossessed)
                {
                    hint += "  - it feels unnaturally heavy... pass it to a teammate!";
                }
            }
            GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height - 150, 600, 80), hint);
        }
    }
}
