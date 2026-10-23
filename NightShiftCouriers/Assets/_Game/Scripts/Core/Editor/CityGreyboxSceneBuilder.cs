using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create City_Greybox Scene
// The playable level for Milestones 1 and 2: depot, a flooding canal with a Jal-Pishach, a bridge,
// three deliveries (plus a two-person heavy crate), and a menu to host or join a game.
public static class CityGreyboxSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/City_Greybox/City_Greybox.unity";

    [MenuItem("Night Shift/Create City_Greybox Scene")]
    public static void Build()
    {
        SceneBuildHelpers.EnableNewInputSystem();
        SceneBuildHelpers.NetworkPrefabs prefabs = SceneBuildHelpers.BuildNetworkPrefabs();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Menu camera: looks at the depot until your player spawns
        Camera menuCamera = Camera.main;
        menuCamera.transform.position = new Vector3(0f, 6f, -48f);
        menuCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        // Ground
        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(140f, 1f, 140f));

        BuildDepot();
        BuildNorthSide();
        BuildBridge();

        // Water: the city floods, and the canal strip (z 0..10) is where the Jal-Pishach lives
        SceneBuildHelpers.MakeWater("Flood Water (city)", new Vector3(0f, -0.5f, 0f), 140f, 140f, true);
        WaterVolume canalWater = SceneBuildHelpers.MakeWater("Flood Water (canal)", new Vector3(0f, -0.5f, 5f), 140f, 10f, true);
        SceneBuildHelpers.MakeMonsterSpawn(canalWater, new Vector3(20f, -0.5f, 5f));

        // Deliveries
        SceneBuildHelpers.MakeDeliveryPoint("12 Market Street", new Vector3(-20f, 0f, 25f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeDeliveryPoint("7 Temple Lane", new Vector3(10f, 0f, 35f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeDeliveryPoint("Mrs. Iyer, Rooftop Flat", new Vector3(25f, 3f, 31f), new Vector3(3f, 2.5f, 3f));

        // Up to four players start side by side in the depot yard
        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -26f));
        }

        // Menu to host/join; the host presses "Start Shift" once the crew has joined.
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), false, false, 400);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }

    private static void BuildDepot()
    {
        // Back wall and side walls of the depot yard
        SceneBuildHelpers.MakeBlock("Depot Back Wall", new Vector3(0f, 2f, -34f), new Vector3(16f, 4f, 0.5f));
        SceneBuildHelpers.MakeBlock("Depot Wall W", new Vector3(-8f, 2f, -28f), new Vector3(0.5f, 4f, 12f));
        SceneBuildHelpers.MakeBlock("Depot Wall E", new Vector3(8f, 2f, -28f), new Vector3(0.5f, 4f, 12f));
        SceneBuildHelpers.MakeBlock("Depot Table", new Vector3(0f, 0.5f, -31f), new Vector3(8f, 1f, 1.5f));

        // Packages appear on the table when the shift starts
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-3f, 1.3f, -31f), "12 Market Street");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(-1f, 1.3f, -31f), "7 Temple Lane");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetWetPackageData(), new Vector3(1f, 1.3f, -31f), "Mrs. Iyer, Rooftop Flat");
        // Needs two players to lift: a bonus for crews of 2+ (the quota can be met without it)
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetHeavyPackageData(), new Vector3(3f, 1.4f, -31f), "12 Market Street");
    }

    private static void BuildNorthSide()
    {
        // Some buildings to make streets
        SceneBuildHelpers.MakeBlock("Building Market", new Vector3(-26f, 3f, 25f), new Vector3(8f, 6f, 10f));
        SceneBuildHelpers.MakeBlock("Building Temple", new Vector3(16f, 3f, 38f), new Vector3(8f, 6f, 6f));
        SceneBuildHelpers.MakeBlock("Building Lane 1", new Vector3(-8f, 4f, 40f), new Vector3(8f, 8f, 8f));
        SceneBuildHelpers.MakeBlock("Building Lane 2", new Vector3(-30f, 4f, 45f), new Vector3(10f, 8f, 8f));

        // Rooftop flat for Mrs. Iyer: stairs up (10 steps = 3 m) then a roof
        SceneBuildHelpers.MakeStairs("Rooftop Stairs", new Vector3(25f, 0f, 17f), 3f, 10);
        SceneBuildHelpers.MakeBlock("Rooftop Flat", new Vector3(25f, 2.85f, 33f), new Vector3(10f, 0.3f, 12f));
        SceneBuildHelpers.MakeBlock("Rooftop Flat Pillar", new Vector3(25f, 1.4f, 38.5f), new Vector3(1f, 2.8f, 1f));
    }

    private static void BuildBridge()
    {
        // Safe way over the canal (z 0..10): stairs up, deck, stairs down. Stays above the deepest flood (2.2 m).
        float x = -4f;
        SceneBuildHelpers.MakeStairs("Bridge Stairs Up", new Vector3(x, 0f, -10f), 3f, 10);
        SceneBuildHelpers.MakeBlock("Bridge Deck", new Vector3(x, 2.85f, 5f), new Vector3(3f, 0.3f, 10f));
        SceneBuildHelpers.MakeStairs("Bridge Stairs Down", new Vector3(x, 0f, 10f), 3f, 10, true);
        // Rails, so you can see the bridge from far away
        SceneBuildHelpers.MakeBlock("Bridge Rail W", new Vector3(x - 1.4f, 3.5f, 5f), new Vector3(0.1f, 1f, 10f));
        SceneBuildHelpers.MakeBlock("Bridge Rail E", new Vector3(x + 1.4f, 3.5f, 5f), new Vector3(0.1f, 1f, 10f));
    }
}
