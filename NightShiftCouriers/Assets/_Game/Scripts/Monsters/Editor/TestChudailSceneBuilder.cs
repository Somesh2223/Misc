using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Monster_Chudail Scene
// Four buildings with streets between them, a rooftop, and salt / decoys / a flashlight to test counters.
public static class TestChudailSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Monster_Chudail/Test_Monster_Chudail.unity";

    [MenuItem("Night Shift/Create Test_Monster_Chudail Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Monster_Chudail");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f));

        // A 2 x 2 block of buildings with 8 m streets.
        SceneBuildHelpers.MakeBlock("Building NW", new Vector3(-9f, 3f, 9f), new Vector3(10f, 6f, 10f));
        SceneBuildHelpers.MakeBlock("Building NE", new Vector3(9f, 3f, 9f), new Vector3(10f, 6f, 10f));
        SceneBuildHelpers.MakeBlock("Building SW", new Vector3(-9f, 3f, -9f), new Vector3(10f, 6f, 10f));
        SceneBuildHelpers.MakeBlock("Low Building SE", new Vector3(9f, 1.5f, -9f), new Vector3(10f, 3f, 10f));
        // Stairs onto the low rooftop: she walks rooftops too.
        SceneBuildHelpers.MakeStairs("Stairs to roof", new Vector3(16f, 0f, -24f), 2f, 10);

        SceneBuildHelpers.MakeNavMeshSurface();
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Chudail, new Vector3(0f, 0.1f, 30f));

        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.SaltItem, new Vector3(-2f, 0.3f, -26f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.SaltItem, new Vector3(-1f, 0.3f, -26f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.DecoyItem, new Vector3(1f, 0.3f, -26f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.FlashlightItem, new Vector3(2f, 0.3f, -26f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -30f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
