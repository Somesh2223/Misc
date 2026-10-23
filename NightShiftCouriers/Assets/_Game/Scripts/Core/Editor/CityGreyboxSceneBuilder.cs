using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create City_Greybox Scene
// The playable level: a depot with a shop and extraction zone, a flooding canal with a Jal-Pishach,
// streets with a Chudail, wires and a balcony with Vetalas, a raft workbench with parts to find,
// climbable drainpipes, and deliveries (one needs two people).
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

        SceneBuildHelpers.MakeBlock("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(140f, 1f, 140f));

        BuildDepot();
        BuildNorthSide();
        BuildBridge();
        BuildRaftArea();
        BuildVetalaPerches();
        BuildStreetlights();

        // Water: the city floods, and the canal strip (z 0..10) is where the Jal-Pishach lives
        SceneBuildHelpers.MakeWater("Flood Water (city)", new Vector3(0f, -0.5f, 0f), 140f, 140f, true);
        WaterVolume canalWater = SceneBuildHelpers.MakeWater("Flood Water (canal)", new Vector3(0f, -0.5f, 5f), 140f, 10f, true);
        SceneBuildHelpers.MakeMonsterSpawn(canalWater, new Vector3(20f, -0.5f, 5f));

        // The Chudail walks the northern streets and rooftops.
        SceneBuildHelpers.MakeNavMeshSurface();
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Chudail, new Vector3(-15f, 0.1f, 50f));

        // Deliveries
        SceneBuildHelpers.MakeDeliveryPoint("12 Market Street", new Vector3(-20f, 0f, 25f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeDeliveryPoint("7 Temple Lane", new Vector3(10f, 0f, 35f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeDeliveryPoint("Mrs. Iyer, Rooftop Flat", new Vector3(25f, 3f, 31f), new Vector3(3f, 2.5f, 3f));
        SceneBuildHelpers.MakeDeliveryPoint("Lane House Roof", new Vector3(-8f, 8f, 40f), new Vector3(3f, 2.5f, 3f));

        // Up to four players start side by side in the depot yard
        for (int i = 0; i < 4; i++)
        {
            SceneBuildHelpers.MakePlayerSpawn(new Vector3(-3f + i * 2f, 0.1f, -26f));
        }

        // Menu to host/join; the host presses "Start Shift" once the crew has joined.
        SceneBuildHelpers.MakeNetworkSetup(prefabs, SceneBuildHelpers.GetFloodSettings(false), false, false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SceneBuildHelpers.AddSceneToBuild(ScenePath);
        Debug.Log("Created " + ScenePath);
    }

    private static void BuildDepot()
    {
        SceneBuildHelpers.MakeBlock("Depot Back Wall", new Vector3(0f, 2f, -34f), new Vector3(16f, 4f, 0.5f));
        SceneBuildHelpers.MakeBlock("Depot Wall W", new Vector3(-8f, 2f, -28f), new Vector3(0.5f, 4f, 12f));
        SceneBuildHelpers.MakeBlock("Depot Wall E", new Vector3(8f, 2f, -28f), new Vector3(0.5f, 4f, 12f));
        SceneBuildHelpers.MakeBlock("Depot Table", new Vector3(0f, 0.5f, -31f), new Vector3(8f, 1f, 1.5f));
        SceneBuildHelpers.MakeShopTerminal(new Vector3(6f, 0f, -26f), -90f);
        SceneBuildHelpers.MakeExtractionZone(new Vector3(0f, 2f, -28f), new Vector3(16f, 4f, 12f));
        SceneBuildHelpers.MakeReviveShrine(new Vector3(-6f, 0f, -24f), 90f);

        // Packages appear on the table when the shift starts
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(-3f, 1.3f, -31f), "12 Market Street");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetFragilePackageData(), new Vector3(-1f, 1.3f, -31f), "7 Temple Lane");
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetWetPackageData(), new Vector3(1f, 1.3f, -31f), "Mrs. Iyer, Rooftop Flat");
        // This one is always cursed (plan 4.9): deliver it unopened, or open it and gamble.
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetStandardPackageData(), new Vector3(2.5f, 1.3f, -31f), "Lane House Roof", true);
        // Needs two players to lift: a bonus for crews of 2+ (the quota can be met without it)
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetHeavyPackageData(), new Vector3(-3f, 0.4f, -28f), "12 Market Street");

        // A few free glow sticks in the yard
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.GlowItem, new Vector3(-5f, 0.3f, -24f));
    }

    private static void BuildNorthSide()
    {
        SceneBuildHelpers.MakeBlock("Building Market", new Vector3(-26f, 3f, 25f), new Vector3(8f, 6f, 10f));
        SceneBuildHelpers.MakeBlock("Building Temple", new Vector3(16f, 3f, 38f), new Vector3(8f, 6f, 6f));
        SceneBuildHelpers.MakeBlock("Building Lane 1", new Vector3(-8f, 4f, 40f), new Vector3(8f, 8f, 8f));
        SceneBuildHelpers.MakeBlock("Building Lane 2", new Vector3(-30f, 4f, 45f), new Vector3(10f, 8f, 8f));

        // The only way onto Lane House roof: climb its drainpipe (or use a rope).
        SceneBuildHelpers.MakeClimbable("Drainpipe (Lane 1)", new Vector3(-8f, 4f, 35.85f), new Vector3(0.3f, 8f, 0.3f));

        // Rooftop flat for Mrs. Iyer: stairs up (10 steps = 3 m) then a roof
        SceneBuildHelpers.MakeStairs("Rooftop Stairs", new Vector3(25f, 0f, 17f), 3f, 10);
        SceneBuildHelpers.MakeBlock("Rooftop Flat", new Vector3(25f, 2.85f, 33f), new Vector3(10f, 0.3f, 12f));
        SceneBuildHelpers.MakeBlock("Rooftop Flat Pillar", new Vector3(25f, 1.4f, 38.5f), new Vector3(1f, 2.8f, 1f));

        // Temple courtyard: a rare lashing rope lies here (raft part).
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftLashingData(), new Vector3(20f, 0.3f, 45f));
        SceneBuildHelpers.MakeItemSpawn(SceneBuildHelpers.SaltItem, new Vector3(14f, 0.3f, 44f));
        // A second revive shrine in the temple courtyard.
        SceneBuildHelpers.MakeReviveShrine(new Vector3(22f, 0f, 48f), 180f);
    }

    private static void BuildBridge()
    {
        // Safe way over the canal (z 0..10): stairs up, deck, stairs down. Stays above the deepest flood (2.2 m).
        float x = -4f;
        SceneBuildHelpers.MakeStairs("Bridge Stairs Up", new Vector3(x, 0f, -10f), 3f, 10);
        SceneBuildHelpers.MakeBlock("Bridge Deck", new Vector3(x, 2.85f, 5f), new Vector3(3f, 0.3f, 10f));
        SceneBuildHelpers.MakeStairs("Bridge Stairs Down", new Vector3(x, 0f, 10f), 3f, 10, true);
        SceneBuildHelpers.MakeBlock("Bridge Rail W", new Vector3(x - 1.4f, 3.5f, 5f), new Vector3(0.1f, 1f, 10f));
        SceneBuildHelpers.MakeBlock("Bridge Rail E", new Vector3(x + 1.4f, 3.5f, 5f), new Vector3(0.1f, 1f, 10f));
    }

    // Raft workbench by the canal, floaters out by the monster's water, the platform in an old godown.
    private static void BuildRaftArea()
    {
        SceneBuildHelpers.MakeRaftSpot(new Vector3(-25f, 0f, -8f), 0f, new Vector3(-25f, 0f, 5f));

        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftFloatersData(), new Vector3(32f, 0.6f, -3f));

        // Godown: three walls and a roof, with the heavy platform inside.
        SceneBuildHelpers.MakeBlock("Godown Back", new Vector3(-42f, 2f, -20f), new Vector3(8f, 4f, 0.4f));
        SceneBuildHelpers.MakeBlock("Godown West", new Vector3(-46f, 2f, -16f), new Vector3(0.4f, 4f, 8f));
        SceneBuildHelpers.MakeBlock("Godown East", new Vector3(-38f, 2f, -16f), new Vector3(0.4f, 4f, 8f));
        SceneBuildHelpers.MakeBlock("Godown Roof", new Vector3(-42f, 4.1f, -16f), new Vector3(8.4f, 0.2f, 8.4f));
        SceneBuildHelpers.MakePackageSpawn(SceneBuildHelpers.GetRaftPlatformData(), new Vector3(-42f, 0.3f, -17f));
    }

    // Streetlights along the main roads, for Signal Ghosts to flicker (plan 4.10).
    private static void BuildStreetlights()
    {
        Vector3[] spots =
        {
            new Vector3(-2f, 0f, -16f), new Vector3(2f, 0f, 14f), new Vector3(-12f, 0f, 18f), new Vector3(8f, 0f, 22f),
            new Vector3(-18f, 0f, 34f), new Vector3(4f, 0f, 30f), new Vector3(20f, 0f, 28f), new Vector3(-20f, 0f, -4f),
        };
        foreach (Vector3 spot in spots)
        {
            SceneBuildHelpers.MakeStreetlight(spot);
        }
    }

    // Vetalas hang from a wire across the market street and from the temple balcony.
    private static void BuildVetalaPerches()
    {
        SceneBuildHelpers.MakeBlock("Pole West", new Vector3(-16f, 2.75f, 22f), new Vector3(0.3f, 5.5f, 0.3f));
        SceneBuildHelpers.MakeBlock("Pole East", new Vector3(4f, 2.75f, 22f), new Vector3(0.3f, 5.5f, 0.3f));
        SceneBuildHelpers.MakeBlock("Wire", new Vector3(-6f, 5.4f, 22f), new Vector3(20f, 0.05f, 0.05f));
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Vetala, new Vector3(-9f, 5.3f, 22f));

        SceneBuildHelpers.MakeBlock("Temple Balcony", new Vector3(16f, 4.5f, 34.4f), new Vector3(4f, 0.3f, 1.2f));
        SceneBuildHelpers.MakeMonsterSpawn(MonsterKind.Vetala, new Vector3(16f, 4.3f, 34.4f));
    }
}
