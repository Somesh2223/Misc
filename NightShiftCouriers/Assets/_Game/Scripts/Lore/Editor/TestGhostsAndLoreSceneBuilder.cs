using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Ghosts_And_Lore Scene
// Milestone 4 in one place: a live wire to die on (Signal Ghost), streetlights to flicker, a revive shrine,
// cursed packages, a customer with a rule and a door to knock on, roles, and the journal (J).
public static class TestGhostsAndLoreSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Ghosts_And_Lore/Test_Ghosts_And_Lore.unity";

    [MenuItem("Night Shift/Create Test_Ghosts_And_Lore Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        SceneBuildHelpers.EnsureFolder("Assets/_Game/Scenes/Test_Ghosts_And_Lore");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 10f), new Vector3(60f, 1f, 60f));

        // Depot corner: shop, shrine, extraction.
        SceneBuildHelpers.MakeShopTerminal(new Vector3(-6f, 0f, -6f), 90f);
        SceneBuildHelpers.MakeReviveShrine(new Vector3(6f, 0f, -6f), 0f);
        SceneBuildHelpers.MakeExtractionZone(new Vector3(0f, 2f, -6f), new Vector3(16f, 4f, 8f));

        // A live wire in a puddle: step in to die and try being a Signal Ghost.
        SceneBuildHelpers.MakeHazard("Live Wire Puddle (deadly)", new Vector3(12f, 0.05f, 6f), new Vector3(3f, 0.1f, 3f));

        // A street of lamps for ghosts to flicker.
        for (int i = 0; i < 5; i++)
        {
            SceneBuildHelpers.MakeStreetlight(new Vector3(-4f, 0f, 4f + i * 6f));
        }

        // A customer with a rule: knock once (E on the brown door), then drop the package in the green zone.
        SceneBuildHelpers.MakeDeliveryPoint("Test Door", new Vector3(8f, 0f, 24f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeBlock("Table", new Vector3(0f, 0.5f, 2f), new Vector3(6f, 1f, 1.2f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-2f, 1.3f, 2f), "Test Door");

        // Three cursed packages: hold one and press F to open it.
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(0f, 1.3f, 2f), "", true);
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(1.2f, 1.3f, 2f), "", true);
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetWetPackageData(), new Vector3(2.4f, 1.3f, 2f), "", true);

        // The Chudail can be released by a cursed package or a broken rule, so she needs a walking map.
        SceneBuildHelpers.MakeNavMeshSurface();

        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -8f));
        }
        // Starts in the depot so you can pick a role in the side panel, then press START SHIFT.
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), true, false, 1000);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
