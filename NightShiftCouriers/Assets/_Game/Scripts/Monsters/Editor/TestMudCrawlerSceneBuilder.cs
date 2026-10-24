using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Monster_MudCrawler Scene
// A long street with drain grates down both sides and a Mud Crawler under them. Carry the packages
// to the far end. Try: standing still next to a grate (it grabs you), a teammate pulling you free (E),
// and listening for the gurgle and rattle before it strikes.
public static class TestMudCrawlerSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Monster_MudCrawler/Test_Monster_MudCrawler.unity";

    [MenuItem("Night Shift/Create Test_Monster_MudCrawler Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Monster_MudCrawler");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 20f), new Vector3(30f, 1f, 70f));
        SceneBuildHelpers.MakeBlock("Buildings West", new Vector3(-10f, 4f, 20f), new Vector3(6f, 8f, 50f));
        SceneBuildHelpers.MakeBlock("Buildings East", new Vector3(10f, 4f, 20f), new Vector3(6f, 8f, 50f));

        // Grates every 8 m down both kerbs, staggered.
        for (int i = 0; i < 6; i++)
        {
            SceneBuildHelpers.MakeDrainGrate(new Vector3(-5.5f, 0f, 2f + i * 8f));
            SceneBuildHelpers.MakeDrainGrate(new Vector3(5.5f, 0f, 6f + i * 8f));
        }
        // One in the middle of the road, right where the delivery is.
        SceneBuildHelpers.MakeDrainGrate(new Vector3(0f, 0f, 44f));

        SceneBuildHelpers.MakeDeliveryPoint("End of the Street", new Vector3(0f, 0f, 46f), new Vector3(3f, 1.5f, 2f));
        SceneBuildHelpers.MakeNavMeshSurface();
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.MudCrawler, new Vector3(0f, 0f, 44f));

        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-1f, 0.3f, -6f), "End of the Street");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetHeavyPackageData(), new Vector3(1.5f, 0.4f, -6f), "End of the Street");
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.FlashlightItem, new Vector3(0f, 0.3f, -8f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -10f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
