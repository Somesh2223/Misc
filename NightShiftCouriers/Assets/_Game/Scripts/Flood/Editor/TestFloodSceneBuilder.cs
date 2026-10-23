using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Flood Scene
// Streets, stairs up to a rooftop, and a flood that rises every 25 seconds.
public static class TestFloodSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Flood/Test_Flood.unity";

    [MenuItem("Night Shift/Create Test_Flood Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Street", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f));

        // Buildings along the street (just solid blocks)
        SceneBuildHelpers.MakeBlock("Building West", new Vector3(-14f, 4f, 0f), new Vector3(8f, 8f, 20f));
        SceneBuildHelpers.MakeBlock("Building East", new Vector3(14f, 4f, 0f), new Vector3(8f, 8f, 20f));

        // Stairs up to a rooftop (3.0 m) at the north end
        SceneBuildHelpers.MakeStairs("Stairs", new Vector3(0f, 0f, 2f), 3f, 10);
        SceneBuildHelpers.MakeBlock("Rooftop", new Vector3(0f, 2.85f, 18f), new Vector3(8f, 0.3f, 12f));
        SceneBuildHelpers.MakeBlock("Rooftop Crate", new Vector3(2f, 3.3f, 20f), new Vector3(1f, 0.8f, 1f));

        // One package on the street
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(3f, 0.3f, -5f));

        // The flood water rises with the flood (starts hidden below the street)
        SceneBuildHelpers.MakeWater("Flood Water", new Vector3(0f, -0.5f, 0f), 80f, 80f, true);

        SceneBuildHelpers.MakePlayerSpawn(new Vector3(0f, 0.1f, -10f));
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(true), true, true, 400);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
