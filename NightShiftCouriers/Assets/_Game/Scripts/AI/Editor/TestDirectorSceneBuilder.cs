using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Director Scene
// A small block of streets with no monsters at the start: the AI Director (fast settings) sends them
// from hidden spawn points behind the buildings. Press F3 to watch its stress, budget and phase.
// Try: making noise and taking hits (stress goes up, then it backs off), and standing still for a while
// (it plays a scare sound nearby).
public static class TestDirectorSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Director/Test_Director.unity";

    [MenuItem("Night Shift/Create Test_Director Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Director");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(90f, 1f, 90f));

        // A 3 x 3 grid of buildings with 8 m streets: plenty of corners to spawn behind.
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == -1)
                {
                    continue; // an open square where the players start
                }
                SceneBuildHelpers.MakeBlock("Building " + x + "," + z, new Vector3(x * 18f, 4f, z * 18f), new Vector3(10f, 8f, 10f));
            }
        }

        // Director-only spawns in the back streets.
        SceneBuildHelpers.MakeDirectorSpawn(MonsterKind.Chudail, new Vector3(-27f, 0.1f, 27f));
        SceneBuildHelpers.MakeDirectorSpawn(MonsterKind.Chudail, new Vector3(27f, 0.1f, 27f));
        SceneBuildHelpers.MakeDirectorSpawn(MonsterKind.Chudail, new Vector3(0f, 0.1f, 36f));

        // A hanging spot for a Vetala (a wire between two buildings).
        SceneBuildHelpers.MakeBlock("Wire", new Vector3(9f, 6f, 18f), new Vector3(8f, 0.05f, 0.05f));
        SceneBuildHelpers.MakeDirectorSpawn(MonsterKind.Vetala, new Vector3(9f, 5.9f, 18f));

        // Grates for a Mud Crawler along the middle street.
        for (int i = 0; i < 5; i++)
        {
            SceneBuildHelpers.MakeDrainGrate(new Vector3(-9f + (i % 2) * 18f, 0f, -18f + i * 9f));
        }
        SceneBuildHelpers.MakeDirectorSpawn(MonsterKind.MudCrawler, new Vector3(-9f, 0f, 18f));

        SceneBuildHelpers.MakeDeliveryPoint("North Corner", new Vector3(-27f, 0f, 36f), new Vector3(3f, 1.5f, 2f));
        SceneBuildHelpers.MakeDeliveryPoint("East Corner", new Vector3(36f, 0f, 0f), new Vector3(3f, 1.5f, 2f));
        SceneBuildHelpers.MakeNavMeshSurface();

        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-1.5f, 0.3f, -16f), "North Corner");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(1.5f, 0.3f, -16f), "East Corner");
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.SaltItem, new Vector3(-1f, 0.3f, -20f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.DecoyItem, new Vector3(1f, 0.3f, -20f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -24f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true, -1,
            SceneBuildHelpers.GetDirectorSettings(true));

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
