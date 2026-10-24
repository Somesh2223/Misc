using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu: Night Shift > Build > ...
// Makes the Steam Next Fest demo (plan M5): City_Greybox only, Windows 64-bit, with the DEMO_BUILD define
// (BuildInfo.IsDemo), which stops the game after BuildInfo.DemoNights nights and shows a wishlist screen.
public static class DemoBuildMenu
{
    private const string CityScene = "Assets/_Game/Scenes/City_Greybox/City_Greybox.unity";
    private const string DemoFolder = "Builds/Demo";
    private const string FullFolder = "Builds/Full";
    private const string PreviewMenu = "Night Shift/Build/Preview Demo Rules In Editor";

    [MenuItem("Night Shift/Build/Windows Demo")]
    public static void BuildWindowsDemo()
    {
        Build(DemoFolder, new[] { "DEMO_BUILD" });
    }

    [MenuItem("Night Shift/Build/Windows Full Game (testing)")]
    public static void BuildWindowsFull()
    {
        Build(FullFolder, new string[0]);
    }

    [MenuItem("Night Shift/Build/Open Builds Folder")]
    public static void OpenBuildsFolder()
    {
        Directory.CreateDirectory("Builds");
        EditorUtility.RevealInFinder("Builds");
    }

    [MenuItem(PreviewMenu)]
    public static void TogglePreviewDemo()
    {
        bool on = !EditorPrefs.GetBool(BuildInfo.PreviewDemoPrefKey, false);
        EditorPrefs.SetBool(BuildInfo.PreviewDemoPrefKey, on);
        Debug.Log("Demo rules in the editor: " + (on ? "ON" : "OFF"));
    }

    [MenuItem(PreviewMenu, true)]
    private static bool TogglePreviewDemoCheck()
    {
        Menu.SetChecked(PreviewMenu, EditorPrefs.GetBool(BuildInfo.PreviewDemoPrefKey, false));
        return true;
    }

    private static void Build(string folder, string[] defines)
    {
        if (!File.Exists(CityScene))
        {
            EditorUtility.DisplayDialog("Night Shift build", "Create the city first: Night Shift > Create City_Greybox Scene.", "OK");
            return;
        }

        string exe = Path.Combine(folder, PlayerSettings.productName.Replace(" ", "") + ".exe");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { CityScene },
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            extraScriptingDefines = defines,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Built " + exe + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
            EditorUtility.RevealInFinder(exe);
        }
        else
        {
            Debug.LogError("Build failed: " + report.summary.result + ". See the Console for the errors.");
        }
    }
}
