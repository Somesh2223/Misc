using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Monster_Brahmarakshas Scene
// One haunted house with a Brahmarakshas inside, a package to deliver to the study at the back,
// and firecrackers to distract it. Try: closing doors behind you, two couriers holding a door shut,
// and throwing a decoy to pull it away from the study.
public static class TestBrahmarakshasSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Monster_Brahmarakshas/Test_Monster_Brahmarakshas.unity";

    [MenuItem("Night Shift/Create Test_Monster_Brahmarakshas Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Monster_Brahmarakshas");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 5f), new Vector3(50f, 1f, 60f));

        // The house: 16 x 16 m, front door facing the players. The study is the back-right room.
        Vector3 houseCentre = new Vector3(0f, 0f, 12f);
        SceneBuildHelpers.MakeHauntedHouse("The Old Haveli", houseCentre, 16f, 16f);
        Vector3 study = houseCentre + new Vector3(4f, 0f, 4f);
        SceneBuildHelpers.MakeDeliveryPoint("The Old Haveli", study + new Vector3(0f, 0f, -0.5f), new Vector3(2f, 1.5f, 2f));

        SceneBuildHelpers.MakeNavMeshSurface();
        // It starts in the back-left room, next door to the study.
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Brahmarakshas, houseCentre + new Vector3(-4f, 0.1f, 4f));

        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-1f, 0.3f, -10f), "The Old Haveli");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(1f, 0.3f, -10f), "The Old Haveli");
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.DecoyItem, new Vector3(-2f, 0.3f, -12f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.DecoyItem, new Vector3(-1f, 0.3f, -12f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.FlashlightItem, new Vector3(1f, 0.3f, -12f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.FlashlightItem, new Vector3(2f, 0.3f, -12f));

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -15f));
        }
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, true);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
