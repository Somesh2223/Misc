using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Items Scene
// A depot with the shop terminal and 2000 money, plus things to try items on.
public static class TestItemsSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Items/Test_Items.unity";

    [MenuItem("Night Shift/Create Test_Items Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Items");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 10f), new Vector3(60f, 1f, 60f));
        SceneBuildHelpers.MakeShopTerminal(new Vector3(0f, 0f, 2f), 180f);

        // Rope: aim at the top edge of this block. Plank: bridge to the second block.
        SceneBuildHelpers.MakeBlock("Ledge (4 m)", new Vector3(-10f, 2f, 12f), new Vector3(6f, 4f, 6f));
        SceneBuildHelpers.MakeBlock("Ledge across a gap", new Vector3(-10f, 2f, 21f), new Vector3(6f, 4f, 6f));
        SceneBuildHelpers.MakeStairs("Stairs up the ledge", new Vector3(-10f, 0f, 1f), 2f, 8);

        // A fragile package to break and mend with the repair kit.
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(4f, 0.3f, 4f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -4f));
        }
        // Stays in the depot (shop open), with plenty of money to buy one of everything.
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, false, 2000);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
