using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Packages Scene
// A yard with one of each package type, a wall to smash fragile ones against, and a pool.
public static class TestPackagesSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Packages/Test_Packages.unity";

    [MenuItem("Night Shift/Create Test_Packages Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Packages");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f));
        SceneBuildHelpers.MakeBlock("Table", new Vector3(0f, 0.5f, 3f), new Vector3(8f, 1f, 1.5f));
        SceneBuildHelpers.MakeBlock("Smash Wall", new Vector3(0f, 2f, 12f), new Vector3(12f, 4f, 0.5f));
        SceneBuildHelpers.MakeBlock("Shelf (drop from here)", new Vector3(-8f, 1.5f, 3f), new Vector3(2f, 3f, 2f));

        // Pool for floating / sinking / wet tests (water surface y = 1.5)
        Vector3 pool = new Vector3(14f, 0f, 3f);
        SceneBuildHelpers.MakeBlock("Pool Bottom", pool + new Vector3(0f, 0.05f, 0f), new Vector3(8f, 0.1f, 8f));
        SceneBuildHelpers.MakeBlock("Pool Wall N", pool + new Vector3(0f, 1f, 4.25f), new Vector3(8.5f, 2f, 0.5f));
        SceneBuildHelpers.MakeBlock("Pool Wall S", pool + new Vector3(0f, 1f, -4.25f), new Vector3(8.5f, 2f, 0.5f));
        SceneBuildHelpers.MakeBlock("Pool Wall E", pool + new Vector3(4.25f, 1f, 0f), new Vector3(0.5f, 2f, 8f));
        SceneBuildHelpers.MakeBlock("Pool Wall W", pool + new Vector3(-4.25f, 1f, 0f), new Vector3(0.5f, 2f, 8f));
        SceneBuildHelpers.MakeWater("Water", new Vector3(pool.x, 1.5f, pool.z), 8f, 8f);

        // One of each package on the table. The server spawns them when the shift starts.
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-3f, 1.3f, 3f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(-1f, 1.3f, 3f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetHeavyPackageData(), new Vector3(1.5f, 1.4f, 3f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetWetPackageData(), new Vector3(3.5f, 1.3f, 3f));

        SceneBuildHelpers.MakePlayerSpawn(new Vector3(0f, 0.1f, -4f));
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(true), true, true, 400);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
