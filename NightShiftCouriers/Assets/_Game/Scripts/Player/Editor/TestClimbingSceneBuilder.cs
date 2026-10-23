using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Climbing Scene
// A climbable building, a drainpipe, a rooftop gap for planks, a ledge for ropes.
public static class TestClimbingSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Climbing/Test_Climbing.unity";

    [MenuItem("Night Shift/Create Test_Climbing Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Climbing");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 10f), new Vector3(70f, 1f, 70f));

        // 1) A whole climbable building (green): face it, press Space, climb with W, pull up at the top.
        SceneBuildHelpers.MakeClimbable("Climbable Building (6 m)", new Vector3(0f, 3f, 10f), new Vector3(8f, 6f, 8f));
        // A low climbable wall to practise on.
        SceneBuildHelpers.MakeClimbable("Climbable Wall (2.5 m)", new Vector3(-8f, 1.25f, 4f), new Vector3(4f, 2.5f, 0.5f));

        // 2) A normal building with a climbable drainpipe on its front.
        SceneBuildHelpers.MakeBlock("Building with Drainpipe", new Vector3(14f, 3f, 10f), new Vector3(8f, 6f, 8f));
        SceneBuildHelpers.MakeClimbable("Drainpipe", new Vector3(14f, 3f, 5.85f), new Vector3(0.3f, 6f, 0.3f));

        // 3) Another building 3 m away: lay a plank across the rooftop gap (right mouse with the plank).
        SceneBuildHelpers.MakeBlock("Building across the gap", new Vector3(14f, 3f, 21f), new Vector3(8f, 6f, 8f));

        // 4) A plain 4 m block: aim at its top edge with a rope (right mouse) and climb the rope.
        SceneBuildHelpers.MakeBlock("Ledge for Rope (4 m)", new Vector3(-14f, 2f, 12f), new Vector3(6f, 4f, 6f));

        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.RopeItem, new Vector3(-3f, 0.3f, -2f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.RopeItem, new Vector3(-2f, 0.3f, -2f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.PlankItem, new Vector3(2f, 0.3f, -2f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.PlankItem, new Vector3(3f, 0.3f, -2f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -6f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
