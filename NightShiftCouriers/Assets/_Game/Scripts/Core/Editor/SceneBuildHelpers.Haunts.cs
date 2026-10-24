using UnityEditor;
using UnityEngine;

// Scene builder helpers for the M5 monsters' homes: haunted houses with doors (Brahmarakshas),
// drain grates (Mud Crawler) and the AI Director's spawn points.
public static partial class SceneBuildHelpers
{
    public const float DoorwayWidth = 1.6f;   // matches HouseDoor's NavMeshObstacle
    public const float DoorwayHeight = 2.4f;
    private const float WallThickness = 0.3f;

    private static readonly Color HouseWallColor = new Color(0.42f, 0.37f, 0.3f);
    private static readonly Color DoorColor = new Color(0.3f, 0.18f, 0.1f);

    // A haunted house with three rooms: a front hall (south), and two back rooms joined by a door.
    //   - front door in the middle of the south wall (starts open)
    //   - a wall across the middle with a door into each back room (closed)
    //   - a wall between the two back rooms with a door (closed)
    // The study (north-east room) is centred at groundCentre + (width/4, 0, depth/4).
    public static HauntedHouse MakeHauntedHouse(string houseName, Vector3 groundCentre, float width, float depth, float height = 4f)
    {
        GameObject root = new GameObject("Haunted House: " + houseName);
        root.transform.position = groundCentre;
        Transform parent = root.transform;
        float halfW = width / 2f;
        float halfD = depth / 2f;

        // Outer walls (the east and west walls run the full depth; north and south fit between them).
        MakeWall(parent, "Wall South", groundCentre + new Vector3(-halfW, 0f, -halfD), groundCentre + new Vector3(halfW, 0f, -halfD), height, halfW);
        MakeWall(parent, "Wall North", groundCentre + new Vector3(-halfW, 0f, halfD), groundCentre + new Vector3(halfW, 0f, halfD), height);
        MakeWall(parent, "Wall West", groundCentre + new Vector3(-halfW, 0f, -halfD), groundCentre + new Vector3(-halfW, 0f, halfD), height);
        MakeWall(parent, "Wall East", groundCentre + new Vector3(halfW, 0f, -halfD), groundCentre + new Vector3(halfW, 0f, halfD), height);

        // Inside walls.
        MakeWall(parent, "Wall Middle", groundCentre + new Vector3(-halfW, 0f, 0f), groundCentre + new Vector3(halfW, 0f, 0f), height,
            width / 4f, width * 3f / 4f);
        MakeWall(parent, "Wall Back Rooms", groundCentre + new Vector3(0f, 0f, 0f), groundCentre + new Vector3(0f, 0f, halfD), height, halfD / 2f);

        GameObject roof = MakeBlock("Roof", groundCentre + new Vector3(0f, height + 0.15f, 0f),
            new Vector3(width + 0.6f, 0.3f, depth + 0.6f), parent);
        ColorObject(roof, new Color(0.25f, 0.2f, 0.18f));

        MakeHouseDoor(groundCentre + new Vector3(0f, 0f, -halfD), 0f, false, parent);
        MakeHouseDoor(groundCentre + new Vector3(-halfW / 2f, 0f, 0f), 0f, true, parent);
        MakeHouseDoor(groundCentre + new Vector3(halfW / 2f, 0f, 0f), 0f, true, parent);
        MakeHouseDoor(groundCentre + new Vector3(0f, 0f, halfD / 2f), 90f, true, parent);

        // Candles: lit while the Brahmarakshas is home, flickering while it hunts.
        MakeCandle(parent, groundCentre + new Vector3(-halfW + 1f, 0f, -halfD + 1f));
        MakeCandle(parent, groundCentre + new Vector3(halfW - 1f, 0f, -halfD + 1f));
        MakeCandle(parent, groundCentre + new Vector3(-halfW / 2f, 0f, halfD - 1f));
        MakeCandle(parent, groundCentre + new Vector3(halfW / 2f, 0f, halfD - 1f));

        HauntedHouse house = root.AddComponent<HauntedHouse>();
        SerializedObject so = new SerializedObject(house);
        so.FindProperty("houseName").stringValue = houseName;
        so.FindProperty("center").vector3Value = new Vector3(0f, height / 2f, 0f);
        so.FindProperty("size").vector3Value = new Vector3(width, height, depth);
        so.ApplyModifiedPropertiesWithoutUndo();
        return house;
    }

    // A straight wall between two ground points (along X or Z), with door-sized gaps.
    // doorways: distance from a to the middle of each gap, in order.
    public static void MakeWall(Transform parent, string name, Vector3 a, Vector3 b, float height, params float[] doorways)
    {
        float length = Vector3.Distance(a, b);
        Vector3 direction = (b - a) / length;
        float start = 0f;
        foreach (float doorway in doorways)
        {
            MakeWallPiece(parent, name, a, direction, start, doorway - DoorwayWidth / 2f, 0f, height);
            MakeWallPiece(parent, name + " (above door)", a, direction, doorway - DoorwayWidth / 2f, doorway + DoorwayWidth / 2f, DoorwayHeight, height);
            start = doorway + DoorwayWidth / 2f;
        }
        MakeWallPiece(parent, name, a, direction, start, length, 0f, height);
    }

    private static void MakeWallPiece(Transform parent, string name, Vector3 a, Vector3 direction, float from, float to, float bottom, float top)
    {
        if (to - from < 0.01f || top - bottom < 0.01f)
        {
            return;
        }
        bool alongX = Mathf.Abs(direction.x) > 0.5f;
        float length = to - from;
        Vector3 scale = alongX
            ? new Vector3(length, top - bottom, WallThickness)
            : new Vector3(WallThickness, top - bottom, length);
        Vector3 centre = a + direction * (from + length / 2f) + Vector3.up * (bottom + (top - bottom) / 2f);
        GameObject piece = MakeBlock(name, centre, scale, parent);
        ColorObject(piece, HouseWallColor);
    }

    // A door that swings open and shut (E). groundCentre is the middle of the doorway at floor level.
    // yaw 0 = a door in a wall that runs along X; yaw 90 = a wall along Z.
    public static HouseDoor MakeHouseDoor(Vector3 groundCentre, float yaw, bool startsClosed, Transform parent = null)
    {
        int id = Object.FindObjectsByType<HouseDoor>(FindObjectsSortMode.None).Length;
        GameObject door = new GameObject("House Door " + id + " (E)");
        door.transform.position = groundCentre;
        door.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (parent != null)
        {
            door.transform.SetParent(parent, true);
        }

        GameObject hinge = new GameObject("Hinge");
        hinge.transform.SetParent(door.transform, false);
        hinge.transform.localPosition = new Vector3(-DoorwayWidth / 2f, 0f, 0f);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(hinge.transform, false);
        panel.transform.localPosition = new Vector3(DoorwayWidth / 2f, (DoorwayHeight - 0.05f) / 2f, 0f);
        panel.transform.localScale = new Vector3(DoorwayWidth - 0.05f, DoorwayHeight - 0.05f, 0.08f);
        panel.layer = IgnoreRaycastLayer; // left out of the NavMesh bake; HouseDoor carves the NavMesh itself
        ColorObject(panel, DoorColor);

        HouseDoor script = door.AddComponent<HouseDoor>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("doorId").intValue = id;
        so.FindProperty("startsClosed").boolValue = startsClosed;
        so.FindProperty("hinge").objectReferenceValue = hinge.transform;
        so.FindProperty("panel").objectReferenceValue = panel.GetComponent<Collider>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return script;
    }

    private static void MakeCandle(Transform parent, Vector3 groundPosition)
    {
        GameObject stand = MakeBlock("Candle Stand", groundPosition + Vector3.up * 0.45f, new Vector3(0.3f, 0.9f, 0.3f), parent);
        ColorObject(stand, new Color(0.3f, 0.25f, 0.2f));
        GameObject candle = AddVisual(stand.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f),
            new Vector3(0.4f, 0.2f, 0.4f), "Candle", new Color(0.95f, 0.9f, 0.75f));

        GameObject flame = new GameObject("Candle Light");
        flame.transform.position = groundPosition + Vector3.up * 1.2f;
        flame.transform.SetParent(candle.transform, true);
        Light light = flame.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 6f;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.enabled = false; // HauntedHouse lights them when the Brahmarakshas is home
    }

    // A storm drain grate in the street for the Mud Crawler (flush with the ground; no collider).
    public static DrainGrate MakeDrainGrate(Vector3 groundPosition)
    {
        int id = Object.FindObjectsByType<DrainGrate>(FindObjectsSortMode.None).Length;
        GameObject grate = new GameObject("Drain Grate " + id);
        grate.transform.position = groundPosition;

        GameObject plate = new GameObject("Plate");
        plate.transform.SetParent(grate.transform, false);
        plate.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        AddVisual(plate.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 0.03f, 0.9f), "DrainGrate", new Color(0.12f, 0.12f, 0.12f));
        for (int i = 0; i < 4; i++)
        {
            AddVisual(plate.transform, PrimitiveType.Cube, new Vector3(-0.3f + i * 0.2f, 0.02f, 0f), new Vector3(0.05f, 0.02f, 0.8f),
                "DrainBars", new Color(0.3f, 0.3f, 0.3f));
        }

        DrainGrate script = grate.AddComponent<DrainGrate>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("grateId").intValue = id;
        so.FindProperty("plate").objectReferenceValue = plate.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        return script;
    }

    // A spawn point only the AI Director uses, when the crew is not looking (plan M5).
    // homeWater is needed for a Jal-Pishach.
    public static void MakeDirectorSpawn(MonsterKind kind, Vector3 position, WaterVolume homeWater = null)
    {
        GameObject marker = new GameObject("Director Spawn - " + kind);
        marker.transform.position = position;
        MonsterSpawnPoint point = marker.AddComponent<MonsterSpawnPoint>();
        point.kind = kind;
        point.homeWater = homeWater;
        point.directorOnly = true;
    }
}
