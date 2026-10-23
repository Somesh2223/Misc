using UnityEngine;

// Debug HUD for the delivery loop: money, quota, order list, and where the held package goes.
public class DeliveryHUD : MonoBehaviour
{
    private void OnGUI()
    {
        DeliveryManager manager = DeliveryManager.Instance;
        if (manager == null)
        {
            return;
        }

        string text = "Money: " + manager.Money + " / Quota " + manager.Quota + "\nOrders:\n" + manager.OrderSummary;
        GUI.Label(new Rect(20, 110, 480, 200), text);

        NetworkPlayer local = NetworkPlayer.Local;
        PlayerGrabber grabber = local != null ? local.GetComponent<PlayerGrabber>() : null;
        if (grabber != null && grabber.IsHolding)
        {
            Package held = grabber.HeldPackage;
            string address = held != null ? held.DestinationAddress : "";
            string hint = address == "" ? "Holding a package" : "Holding package for: " + address;
            if (held != null && held.Data != null && held.Data.holdersRequired > 1)
            {
                hint += "  (needs 2 players to lift)";
            }
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height - 120, 400, 30), hint);
        }

        string message = manager.Message;
        if (message != "")
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(0, 140, Screen.width, 40), message, style);
        }

        if (manager.ShiftOver)
        {
            GUIStyle big = new GUIStyle(GUI.skin.label);
            big.fontSize = 36;
            big.alignment = TextAnchor.MiddleCenter;
            string result = manager.QuotaMet ? "SHIFT COMPLETE - QUOTA MET" : "SHIFT OVER - QUOTA MISSED";
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
                result + "\nEarned " + manager.Money + " / " + manager.Quota + "\nHost: press R to restart", big);
        }
    }
}
