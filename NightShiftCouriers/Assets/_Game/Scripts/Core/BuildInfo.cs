using UnityEngine;

// What kind of build this is (plan M5: Steam Next Fest demo).
// The demo build is made with Night Shift > Build > Windows Demo, which adds the DEMO_BUILD define.
// In the editor you can preview the demo rules with Night Shift > Build > Preview Demo Rules In Editor.
public static class BuildInfo
{
    // How many nights the demo lets a crew play before the "thanks for playing" screen.
    public const int DemoNights = 3;

    // Replace with the real store page once Steam gives the game an App ID.
    public const string StorePageUrl = "https://store.steampowered.com/";

    public const string PreviewDemoPrefKey = "NightShift.PreviewDemo";

    public static bool IsDemo
    {
        get
        {
#if DEMO_BUILD
            return true;
#elif UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(PreviewDemoPrefKey, false);
#else
            return false;
#endif
        }
    }

    public static string Version => Application.version;

    // True once the crew has played every demo night (checked at the depot).
    public static bool IsDemoOver(int night)
    {
        return IsDemo && night > DemoNights;
    }
}
