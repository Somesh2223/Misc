using UnityEngine;

// Minimal debug HUD for the local player: stamina bar plus current state.
// Uses OnGUI so no Canvas setup is needed; real UI comes later.
public class StaminaHUD : MonoBehaviour
{
    private void OnGUI()
    {
        if (UIState.HideHud)
        {
            return; // F1: screenshot mode
        }
        NetworkPlayer local = NetworkPlayer.Local;
        if (local == null)
        {
            return;
        }

        PlayerStamina stamina = local.GetComponent<PlayerStamina>();
        PlayerController player = local.GetComponent<PlayerController>();
        if (stamina == null || player == null)
        {
            return;
        }

        float barWidth = 220f;
        float barHeight = 18f;
        Rect back = new Rect(20, Screen.height - 40, barWidth, barHeight);
        Rect fill = new Rect(back.x, back.y, barWidth * stamina.Percent, barHeight);

        GUI.color = Color.black;
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = stamina.IsExhausted ? Color.red : Color.green;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = Color.white;

        string state = "Walking";
        if (player.IsSwimming) state = "Swimming";
        else if (player.IsCrouching) state = "Crouching";
        else if (player.IsSprinting) state = "Sprinting";
        else if (!player.IsGrounded) state = "In air";

        GUI.Label(new Rect(20, Screen.height - 110, 400, 40),
            "State: " + state + "\nWater depth: " + player.WaterDepth.ToString("0.00") + " m  Speed: " + player.CurrentSpeed.ToString("0.0"));
    }
}
