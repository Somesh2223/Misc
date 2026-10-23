using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Monster_JalPishach Scene
// A 24 x 24 m flooded pool with a Jal-Pishach, a dry pier across it, and a shallow ledge for crouch-wading.
public static class TestJalPishachSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Monster_JalPishach/Test_Monster_JalPishach.unity";

    [MenuItem("Night Shift/Create Test_Monster_JalPishach Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Monster_JalPishach");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 10f), new Vector3(70f, 1f, 70f));

        // Pool centred at (0, 0, 15), 24 x 24 m. Walls on three sides; the south side is open ground.
        Vector3 centre = new Vector3(0f, 0f, 15f);
        float half = 12f;
        float wallHeight = 2.5f;
        SceneBuildHelpers.MakeBlock("Pool Wall N", centre + new Vector3(0f, wallHeight / 2f, half + 0.25f), new Vector3(24.5f, wallHeight, 0.5f));
        SceneBuildHelpers.MakeBlock("Pool Wall E", centre + new Vector3(half + 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 24f));
        SceneBuildHelpers.MakeBlock("Pool Wall W", centre + new Vector3(-half - 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 24f));

        float southEdge = centre.z - half; // z = 3, where the water starts

        // Shallow ledge along the west side: steps up from dry ground, then 0.55 m of water (wade depth)
        SceneBuildHelpers.MakeBlock("Ledge Step 1", new Vector3(-8f, 0.125f, southEdge - 2.5f), new Vector3(2f, 0.25f, 1f));
        SceneBuildHelpers.MakeBlock("Ledge Step 2", new Vector3(-8f, 0.25f, southEdge - 1.5f), new Vector3(2f, 0.5f, 1f));
        SceneBuildHelpers.MakeBlock("Ledge Step 3", new Vector3(-8f, 0.375f, southEdge - 0.5f), new Vector3(2f, 0.75f, 1f));
        SceneBuildHelpers.MakeBlock("Shallow Ledge", new Vector3(-8f, 0.375f, centre.z), new Vector3(2f, 0.75f, 24f));

        // Dry pier across the pool (top at 1.5 m, above the 1.3 m water), reached by stairs
        SceneBuildHelpers.MakeStairs("Pier Stairs", new Vector3(8f, 0f, southEdge - 6f), 1.5f, 5);
        SceneBuildHelpers.MakeBlock("Pier", new Vector3(8f, 1.45f, 14.5f), new Vector3(1.5f, 0.1f, 25f));
        SceneBuildHelpers.MakeBlock("Pier Post 1", new Vector3(8f, 0.7f, centre.z - 6f), new Vector3(0.4f, 1.4f, 0.4f));
        SceneBuildHelpers.MakeBlock("Pier Post 2", new Vector3(8f, 0.7f, centre.z + 6f), new Vector3(0.4f, 1.4f, 0.4f));

        // Water, surface at y = 1.3
        WaterVolume water = SceneBuildHelpers.MakeWater("Pool Water", new Vector3(centre.x, 1.3f, centre.z), 24f, 24f);

        // A package out on the pier end
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(8f, 1.8f, centre.z + 9f));

        SceneBuildHelpers.MakeMonsterSpawn(water, new Vector3(0f, 1f, centre.z));

        SceneBuildHelpers.MakePlayerSpawn(new Vector3(0f, 0.1f, southEdge - 8f));
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
