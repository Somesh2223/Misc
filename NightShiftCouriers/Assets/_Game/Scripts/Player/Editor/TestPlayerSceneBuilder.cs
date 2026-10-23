using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Player Scene
// Greybox scene with steps, a low ceiling, a puddle and a pool for testing movement.
public static class TestPlayerSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Player/Test_Player.unity";

    [MenuItem("Night Shift/Create Test_Player Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(70f, 1f, 70f));

        BuildPool();

        SceneBuildHelpers.MakeBlock("Step 0.3m", new Vector3(-6f, 0.15f, 4f), new Vector3(3f, 0.3f, 3f));
        SceneBuildHelpers.MakeBlock("Step 0.6m", new Vector3(-6f, 0.3f, 7f), new Vector3(3f, 0.6f, 3f));
        SceneBuildHelpers.MakeBlock("Box 1m (jump)", new Vector3(-10f, 0.5f, 4f), new Vector3(2f, 1f, 2f));
        SceneBuildHelpers.MakeBlock("Low ceiling (crouch under)", new Vector3(6f, 1.4f, 5f), new Vector3(4f, 0.2f, 4f));
        SceneBuildHelpers.MakeBlock("Long strip (sprint)", new Vector3(0f, 0.05f, 20f), new Vector3(3f, 0.1f, 30f));

        // Shallow puddle for wading (0.5 m deep)
        SceneBuildHelpers.MakeWater("Shallow Water", new Vector3(-18f, 0.5f, 0f), 10f, 10f);

        SceneBuildHelpers.MakePlayerSpawn(new Vector3(0f, 0.1f, -8f));
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(true), true, false, 400);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Created " + ScenePath + ". Press Play, then WASD / Shift / Ctrl / Space.");
    }

    private static void BuildPool()
    {
        // Walled pool 12 x 12 m, 2.5 m tall walls, centred at x = 18
        Vector3 centre = new Vector3(18f, 0f, 0f);
        float wallHeight = 2.5f;
        float half = 6f;

        SceneBuildHelpers.MakeBlock("Pool Bottom", centre + new Vector3(0f, 0.05f, 0f), new Vector3(12f, 0.1f, 12f));
        SceneBuildHelpers.MakeBlock("Pool Wall N", centre + new Vector3(0f, wallHeight / 2f, half + 0.25f), new Vector3(12.5f, wallHeight, 0.5f));
        SceneBuildHelpers.MakeBlock("Pool Wall S", centre + new Vector3(0f, wallHeight / 2f, -half - 0.25f), new Vector3(12.5f, wallHeight, 0.5f));
        SceneBuildHelpers.MakeBlock("Pool Wall E", centre + new Vector3(half + 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 12f));
        SceneBuildHelpers.MakeBlock("Pool Wall W", centre + new Vector3(-half - 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 12f));
        // Steps on the west side so you can climb out
        SceneBuildHelpers.MakeBlock("Pool Step 1", centre + new Vector3(-half + 0.75f, 0.4f, 0f), new Vector3(1.5f, 0.8f, 4f));
        SceneBuildHelpers.MakeBlock("Pool Step 2", centre + new Vector3(-half + 2.25f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 4f));

        // Surface at y = 2.0 (about 1.9 m deep over the pool bottom)
        SceneBuildHelpers.MakeWater("Water", new Vector3(centre.x, 2.0f, centre.z), 12f, 12f);
    }
}
