using UnityEngine;

// Debug HUD for the delivery loop: money, quota, order list, and where the held package goes.
public class DeliveryHUD : MonoBehaviour
{
    [SerializeField] private DeliveryManager manager;
    [SerializeField] private PlayerGrabber grabber;

    private void OnGUI()
    {
        if (manager == null)
        {
            return;
        }

        string text = "Money: " + manager.Money + " / Quota " + manager.Quota + "\nOrders:\n";
        foreach (DeliveryOrder order in manager.Orders)
        {
            string status = order.status.ToString();
            if (order.status == OrderStatus.Delivered)
            {
                status += " +" + order.payout;
            }
            text += "  " + order.packageName + " -> " + order.address + "  [" + status + "]\n";
        }
        GUI.Label(new Rect(20, 110, 450, 200), text);

        if (grabber != null && grabber.IsHolding)
        {
            string address = manager.FindAddressFor(grabber.HeldPackage);
            string hint = address == "" ? "Holding a package" : "Holding package for: " + address;
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
                result + "\nEarned " + manager.Money + " / " + manager.Quota + "\nPress R to restart", big);
        }
    }
}
