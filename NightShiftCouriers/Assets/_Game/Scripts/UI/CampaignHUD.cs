using UnityEngine;

// Night, quota and bank at the top of the screen, and the results screen after a shift.
public class CampaignHUD : MonoBehaviour
{
    private void OnGUI()
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null)
        {
            return;
        }

        string line = "Night " + campaign.Night + "   Quota " + campaign.QuotaProgress + " / " + campaign.Quota
                      + " (" + campaign.NightsLeft + " nights left)   Bank " + campaign.Bank;
        GUIStyle top = new GUIStyle(GUI.skin.label);
        top.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(0, 4, Screen.width, 22), line, top);

        if (campaign.Phase == ShiftPhase.Depot)
        {
            GUI.Label(new Rect(0, 24, Screen.width, 22), "DEPOT - buy items at the shop terminal (E), then the host starts the shift.", top);
        }
        else if (campaign.Phase == ShiftPhase.Results)
        {
            GUIStyle big = new GUIStyle(GUI.skin.box);
            big.fontSize = 22;
            big.alignment = TextAnchor.MiddleCenter;
            big.wordWrap = true;
            Rect panel = new Rect(Screen.width / 2f - 320f, Screen.height / 2f - 150f, 640f, 300f);
            GUI.Box(panel, "SHIFT OVER\n\n" + campaign.ResultText + "\n\n(The host returns everyone to the depot)", big);
        }
    }
}
