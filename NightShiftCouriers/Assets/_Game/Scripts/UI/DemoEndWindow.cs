using UnityEngine;

// Demo builds only: a small "DEMO" mark in the corner, and after the last demo night
// a thank-you screen with a wishlist button instead of the next shift.
public class DemoEndWindow : MonoBehaviour
{
    private void OnGUI()
    {
        if (!BuildInfo.IsDemo)
        {
            return;
        }
        UITheme.Apply();

        if (!UIState.HideHud)
        {
            GUIStyle corner = new GUIStyle(GUI.skin.label);
            corner.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(Screen.width - 310, 4, 300, 22), "DEMO " + BuildInfo.Version + " - " + BuildInfo.DemoNights + " nights", corner);
        }

        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null || campaign.Phase != ShiftPhase.Depot || !BuildInfo.IsDemoOver(campaign.Night))
        {
            return;
        }

        Rect area = new Rect(Screen.width / 2f - 260f, Screen.height / 2f - 170f, 520f, 340f);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("THANKS FOR PLAYING", UITheme.Title);
        GUILayout.Space(10);
        GUILayout.Label("You survived the demo nights of Night Shift Couriers. The full game has more of the city, "
                        + "more monsters, more customers with strange rules, and a campaign that keeps getting wetter.");
        GUILayout.Space(6);
        GUILayout.Label("Bank: " + campaign.Bank + "    Quota: " + campaign.QuotaProgress + " / " + campaign.Quota);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Wishlist on Steam", GUILayout.Height(40)))
        {
            Application.OpenURL(BuildInfo.StorePageUrl);
        }
        if (GUILayout.Button("Quit", GUILayout.Height(30)))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        GUILayout.EndArea();
    }
}
