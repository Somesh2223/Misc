using System.Collections.Generic;
using UnityEngine;

// After a shift (plan 4.11): plays the best body cam clips under the results, and saves them as PNG images.
public class HighlightReelWindow : MonoBehaviour
{
    private const int ClipsShown = 3;
    private const float FramesPerSecond = 4f;

    private ShiftPhase lastPhase = ShiftPhase.Depot;
    private string savedTo = "";

    private void Update()
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null)
        {
            return;
        }

        // Back at the depot: last night's clips are done with.
        if (campaign.Phase == ShiftPhase.Depot && lastPhase != ShiftPhase.Depot)
        {
            BodyCam.ClearClips();
            savedTo = "";
        }
        lastPhase = campaign.Phase;
    }

    private void OnGUI()
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null || campaign.Phase != ShiftPhase.Results)
        {
            return;
        }

        List<BodyCam.Clip> best = BodyCam.TopClips(ClipsShown);
        float boxWidth = 256f;
        float boxHeight = 144f;
        float startX = Screen.width / 2f - (boxWidth + 10f) * best.Count / 2f;
        float y = Screen.height / 2f + 165f;

        if (best.Count == 0)
        {
            GUI.Label(new Rect(Screen.width / 2f - 150f, y, 300f, 24f), "Body cam: nothing exciting happened.");
            return;
        }

        for (int i = 0; i < best.Count; i++)
        {
            BodyCam.Clip clip = best[i];
            if (clip.frames.Count == 0)
            {
                continue;
            }
            int frame = (int)(Time.time * FramesPerSecond) % clip.frames.Count;
            Rect box = new Rect(startX + i * (boxWidth + 10f), y, boxWidth, boxHeight);
            GUI.DrawTexture(box, clip.frames[frame]);
            GUI.Label(new Rect(box.x, box.yMax, boxWidth, 22f), "REC  " + clip.label);
        }

        if (GUI.Button(new Rect(Screen.width / 2f - 120f, y + boxHeight + 26f, 240f, 28f), "Save highlight clips (PNG)"))
        {
            savedTo = BodyCam.SaveAll();
        }
        if (savedTo != "")
        {
            GUI.Label(new Rect(Screen.width / 2f - 300f, y + boxHeight + 58f, 600f, 40f), "Saved to: " + savedTo);
        }
    }
}
