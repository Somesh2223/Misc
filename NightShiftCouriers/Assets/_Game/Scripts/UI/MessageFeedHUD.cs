using UnityEngine;

// Draws the MessageFeed in the upper middle of the screen.
public class MessageFeedHUD : MonoBehaviour
{
    private void OnGUI()
    {
        UITheme.Apply();
        if (UIState.HideHud)
        {
            return; // F1: screenshot mode
        }
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.alignment = TextAnchor.UpperCenter;

        float y = 140f;
        foreach (string text in MessageFeed.Current())
        {
            GUI.Label(new Rect(0, y, Screen.width, 34), text, style);
            y += 30f;
        }
    }
}
