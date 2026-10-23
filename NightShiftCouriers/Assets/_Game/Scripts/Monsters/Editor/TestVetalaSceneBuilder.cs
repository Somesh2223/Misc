using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Monster_Vetala Scene
// A street with a wire overhead and a balcony, a Vetala hanging from each, packages to carry under them,
// and a flashlight and a lemon-chilli charm to test the tells and counters.
public static class TestVetalaSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Monster_Vetala/Test_Monster_Vetala.unity";

    [MenuItem("Night Shift/Create Test_Monster_Vetala Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Monster_Vetala");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 15f), new Vector3(60f, 1f, 70f));

        // A wire strung across the street between two poles, with a Vetala hanging from it.
        SceneBuildHelpers.MakeBlock("Pole West", new Vector3(-6f, 3f, 10f), new Vector3(0.3f, 6f, 0.3f));
        SceneBuildHelpers.MakeBlock("Pole East", new Vector3(6f, 3f, 10f), new Vector3(0.3f, 6f, 0.3f));
        SceneBuildHelpers.MakeBlock("Wire", new Vector3(0f, 5.8f, 10f), new Vector3(12f, 0.05f, 0.05f));
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Vetala, new Vector3(0f, 5.7f, 10f));

        // A building with a balcony, and a second Vetala under it.
        SceneBuildHelpers.MakeBlock("Building", new Vector3(10f, 4f, 28f), new Vector3(8f, 8f, 8f));
        SceneBuildHelpers.MakeBlock("Balcony", new Vector3(10f, 5f, 23.4f), new Vector3(4f, 0.3f, 1.2f));
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Vetala, new Vector3(10f, 4.8f, 23.4f));

        // Packages to carry under them (to test possession), and the counters.
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-2f, 0.3f, 0f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(2f, 0.3f, 0f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.FlashlightItem, new Vector3(-1f, 0.3f, -3f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.CharmItem, new Vector3(1f, 0.3f, -3f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -8f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
