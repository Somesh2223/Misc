using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Raft Scene
// A workbench on the shore, the three raft parts nearby, and a wide stretch of water to paddle on.
public static class TestRaftSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Raft/Test_Raft.unity";

    [MenuItem("Night Shift/Create Test_Raft Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Raft");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 10f), new Vector3(80f, 1f, 80f));

        // Water from z = -8 to z = 32, 1.3 m deep.
        WaterVolume water = SceneBuildHelpers.MakeWater("Water", new Vector3(0f, 1.3f, 12f), 60f, 40f);
        // Things to bump into (rafts take damage hitting them fast).
        SceneBuildHelpers.MakeBlock("Post 1", new Vector3(-6f, 1f, 8f), new Vector3(1f, 2f, 1f));
        SceneBuildHelpers.MakeBlock("Post 2", new Vector3(6f, 1f, 16f), new Vector3(1f, 2f, 1f));

        // Workbench on the shore, facing the water. Parts go in front of it.
        SceneBuildHelpers.MakeRaftSpot(new Vector3(0f, 0f, -16f), 0f, new Vector3(0f, 1.3f, -4f));

        // The heavy platform starts inside the parts zone (one player cannot carry it alone).
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftPlatformData(), new Vector3(0f, 0.3f, -14f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftFloatersData(), new Vector3(6f, 0.6f, -16f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftLashingData(), new Vector3(-5f, 0.3f, -16f));

        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.PlankItem, new Vector3(-2f, 0.3f, -20f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.PlankItem, new Vector3(-1f, 0.3f, -20f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.RepairItem, new Vector3(1f, 0.3f, -20f));

        // A water monster at the far end, to test it ramming the raft.
        SceneBuildHelpers.MakeMonsterSpawn(water, new Vector3(0f, 1f, 28f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -24f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
