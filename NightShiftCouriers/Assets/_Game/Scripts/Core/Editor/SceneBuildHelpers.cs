using System.IO;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Shared helpers used by the "Night Shift" menu scene builders: shapes, water, markers and the
// NetworkManager setup every scene needs. Data assets and prefabs are in SceneBuildHelpers.Assets.cs.
public static partial class SceneBuildHelpers
{
    public const int IgnoreRaycastLayer = 2; // players and trigger zones
    public const int WaterLayer = 4;

    // ---------- Basic shapes ----------

    public static GameObject MakeBlock(string name, Vector3 position, Vector3 scale, Transform parent = null)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.localScale = scale;
        if (parent != null)
        {
            block.transform.SetParent(parent, true);
        }
        return block;
    }

    // Uses the URP shader only when a URP pipeline asset is active; otherwise URP shaders render pink,
    // so fall back to the built-in Standard shader.
    public static Material MakeMaterial(Color color)
    {
        bool usingUrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        Shader shader = usingUrp ? Shader.Find("Universal Render Pipeline/Lit") : null;
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    public static void ColorObject(GameObject target, Color color)
    {
        target.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
    }

    public static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    public static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Stairs of 0.3 m steps (matches the CharacterController step offset) along +Z, rising (or falling if descending).
    public static void MakeStairs(string name, Vector3 startBottom, float width, int steps, bool descending = false)
    {
        GameObject parent = new GameObject(name);
        for (int i = 0; i < steps; i++)
        {
            float height = descending ? 0.3f * (steps - i) : 0.3f * (i + 1);
            GameObject step = MakeBlock("Step " + (i + 1),
                startBottom + new Vector3(0f, height / 2f, 1f * i + 0.5f),
                new Vector3(width, height, 1f), parent.transform);
            ColorObject(step, new Color(0.6f, 0.6f, 0.6f));
        }
    }

    // A wall (or drainpipe) players can climb: face it and press Space (plan 4.1). Coloured so it reads as climbable.
    public static GameObject MakeClimbable(string name, Vector3 position, Vector3 scale)
    {
        GameObject block = MakeBlock(name, position, scale);
        block.AddComponent<Climbable>();
        ColorObject(block, new Color(0.45f, 0.6f, 0.35f));
        return block;
    }

    // ---------- Water ----------

    // A flat box of water. The surface is the object's Y position; width/length come from the scale.
    // followsFlood = the FloodManager raises and lowers it.
    public static WaterVolume MakeWater(string name, Vector3 centreAtSurface, float width, float length, bool followsFlood = false)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = name;
        water.layer = WaterLayer;
        water.transform.position = centreAtSurface;
        water.transform.localScale = new Vector3(width, 0.02f, length);
        Object.DestroyImmediate(water.GetComponent<BoxCollider>());
        water.AddComponent<BoxCollider>().isTrigger = true;
        ColorObject(water, new Color(0.1f, 0.4f, 0.9f, 1f));

        int id = Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None).Length + 1;
        WaterVolume volume = water.AddComponent<WaterVolume>();
        SerializedObject so = new SerializedObject(volume);
        so.FindProperty("waterId").intValue = id;
        so.FindProperty("followsFlood").boolValue = followsFlood;
        so.ApplyModifiedPropertiesWithoutUndo();
        return volume;
    }

    // ---------- Scene setup ----------

    // Adds the NetworkManager object (with all the HUDs) and the ShiftSpawner to the current scene.
    // autoHost: skip the menu and host immediately (single-player test scenes).
    // autoStartShift: start the shift as soon as hosting starts.
    // startingMoney: -1 = normal; test scenes can give the crew more to try the shop.
    public static void MakeNetworkSetup(NetworkPrefabs prefabs, FloodSettings flood, bool autoHost, bool autoStartShift, int startingMoney = -1)
    {
        GameObject manager = new GameObject("NetworkManager");
        NetworkManager networkManager = manager.AddComponent<NetworkManager>();
        // Everything is spawned at run time, so Netcode's scene synchronisation is not used
        // (GameNetworkConfig also switches it off when the game starts).
        if (networkManager.NetworkConfig != null)
        {
            networkManager.NetworkConfig.EnableSceneManagement = false;
        }
        manager.AddComponent<UnityTransport>();
        GameNetworkConfig config = manager.AddComponent<GameNetworkConfig>();
        NetworkBootstrap bootstrap = manager.AddComponent<NetworkBootstrap>();
        manager.AddComponent<SteamLobbyService>();
        manager.AddComponent<VoiceChatService>();
        manager.AddComponent<StaminaHUD>();
        manager.AddComponent<MonsterDebugHUD>();
        manager.AddComponent<InventoryHUD>();
        manager.AddComponent<CampaignHUD>();
        manager.AddComponent<MessageFeedHUD>();
        manager.AddComponent<ShopWindow>();
        manager.AddComponent<JournalWindow>();
        manager.AddComponent<HighlightReelWindow>();
        manager.AddComponent<WorldMarkersHUD>();

        SerializedObject configSo = new SerializedObject(config);
        configSo.FindProperty("playerPrefab").objectReferenceValue = prefabs.player;
        configSo.FindProperty("packageCatalog").objectReferenceValue = GetPackageCatalog();
        configSo.FindProperty("itemCatalog").objectReferenceValue = GetItemCatalog();
        configSo.FindProperty("floodSettings").objectReferenceValue = flood;
        configSo.FindProperty("curseTable").objectReferenceValue = GetCurseTable();
        configSo.FindProperty("customerDirectory").objectReferenceValue = GetCustomerDirectory();
        configSo.FindProperty("loreBook").objectReferenceValue = GetLoreBook();
        configSo.FindProperty("roleCatalog").objectReferenceValue = GetRoleCatalog();
        configSo.FindProperty("courierBadge").objectReferenceValue = BadgeItem;
        configSo.FindProperty("raftPartFloaters").objectReferenceValue = GetRaftFloatersData();
        configSo.FindProperty("raftPartPlatform").objectReferenceValue = GetRaftPlatformData();
        configSo.FindProperty("raftPartLashing").objectReferenceValue = GetRaftLashingData();
        configSo.FindProperty("itemPrefabs.rope").objectReferenceValue = prefabs.rope;
        configSo.FindProperty("itemPrefabs.plank").objectReferenceValue = prefabs.plank;
        configSo.FindProperty("itemPrefabs.decoy").objectReferenceValue = prefabs.decoy;
        configSo.FindProperty("itemPrefabs.saltWard").objectReferenceValue = prefabs.saltWard;
        configSo.FindProperty("itemPrefabs.glowStick").objectReferenceValue = prefabs.glowStick;
        configSo.FindProperty("itemPrefabs.worldItem").objectReferenceValue = prefabs.worldItem;
        configSo.FindProperty("itemPrefabs.raft").objectReferenceValue = prefabs.raft;

        GameObject[] spawnable = prefabs.AllSpawnable();
        SerializedProperty list = configSo.FindProperty("networkPrefabs");
        list.arraySize = spawnable.Length;
        for (int i = 0; i < spawnable.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = spawnable[i];
        }
        configSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject bootstrapSo = new SerializedObject(bootstrap);
        bootstrapSo.FindProperty("autoStartHost").boolValue = autoHost;
        bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject spawnerObject = new GameObject("ShiftSpawner");
        ShiftSpawner spawner = spawnerObject.AddComponent<ShiftSpawner>();
        SerializedObject spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("campaignPrefab").objectReferenceValue = prefabs.campaign;
        spawnerSo.FindProperty("ghostWorldPrefab").objectReferenceValue = prefabs.ghostWorld;
        spawnerSo.FindProperty("floodPrefab").objectReferenceValue = prefabs.flood;
        spawnerSo.FindProperty("deliveryPrefab").objectReferenceValue = prefabs.delivery;
        spawnerSo.FindProperty("packagePrefab").objectReferenceValue = prefabs.package;
        spawnerSo.FindProperty("jalPishachPrefab").objectReferenceValue = prefabs.jalPishach;
        spawnerSo.FindProperty("chudailPrefab").objectReferenceValue = prefabs.chudail;
        spawnerSo.FindProperty("vetalaPrefab").objectReferenceValue = prefabs.vetala;
        spawnerSo.FindProperty("autoStartShift").boolValue = autoStartShift;
        spawnerSo.FindProperty("startingMoneyOverride").intValue = startingMoney;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // The walking map for the Chudail. It is rebuilt on the server at the start of every shift.
    public static void MakeNavMeshSurface()
    {
        GameObject navigation = new GameObject("NavMesh (rebuilt each shift)");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~((1 << IgnoreRaycastLayer) | (1 << WaterLayer)); // no players, triggers or water
    }

    // Adds a saved scene to File > Build Profiles so builds (and Netcode) can find it.
    public static void AddSceneToBuild(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene existing in current)
        {
            if (existing.path == scenePath)
            {
                return;
            }
        }

        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[current.Length + 1];
        current.CopyTo(updated, 0);
        updated[current.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    // Switch to the new Input System if the project is still on the old one.
    public static void EnableNewInputSystem()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
        {
            return;
        }
        SerializedObject settings = new SerializedObject(assets[0]);
        SerializedProperty handler = settings.FindProperty("activeInputHandler");
        if (handler != null && handler.intValue == 0)
        {
            handler.intValue = 1; // 1 = Input System Package (new)
            settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.LogWarning("Switched active input handler to the new Input System. Restart the editor if Unity asks.");
        }
    }

    // ---------- Scene markers ----------

    public static void MakePlayerSpawn(Vector3 position, float yaw = 0f)
    {
        GameObject marker = new GameObject("Player Spawn");
        marker.transform.position = position;
        marker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        marker.AddComponent<PlayerSpawnPoint>();
    }

    public static void MakePackageSpawn(PackageData data, Vector3 position, string destination = "", bool alwaysCursed = false)
    {
        GameObject marker = new GameObject("Package Spawn - " + data.displayName + (alwaysCursed ? " (cursed)" : ""));
        marker.transform.position = position;
        PackageSpawnPoint point = marker.AddComponent<PackageSpawnPoint>();
        point.data = data;
        point.destination = destination;
        point.alwaysCursed = alwaysCursed;
    }

    // Jal-Pishach: lives in the given water.
    public static void MakeMonsterSpawn(WaterVolume home, Vector3 position)
    {
        GameObject marker = new GameObject("Jal-Pishach Spawn");
        marker.transform.position = position;
        MonsterSpawnPoint point = marker.AddComponent<MonsterSpawnPoint>();
        point.kind = MonsterKind.JalPishach;
        point.homeWater = home;
    }

    // Chudail (walks from here) or Vetala (hangs here).
    public static void MakeMonsterSpawn(MonsterKind kind, Vector3 position)
    {
        GameObject marker = new GameObject(kind + " Spawn");
        marker.transform.position = position;
        marker.AddComponent<MonsterSpawnPoint>().kind = kind;
    }

    public static void MakeItemSpawn(ItemData item, Vector3 position)
    {
        GameObject marker = new GameObject("Item Spawn - " + item.displayName);
        marker.transform.position = position;
        marker.AddComponent<WorldItemSpawnPoint>().item = item;
    }

    // A glowing marker zone where packages are delivered.
    public static DeliveryPoint MakeDeliveryPoint(string address, Vector3 groundPosition, Vector3 size)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "Delivery: " + address;
        zone.layer = IgnoreRaycastLayer;
        zone.transform.position = groundPosition + new Vector3(0f, size.y / 2f, 0f);
        zone.transform.localScale = size;
        Object.DestroyImmediate(zone.GetComponent<BoxCollider>());
        zone.AddComponent<BoxCollider>().isTrigger = true;
        ColorObject(zone, new Color(0.2f, 1f, 0.4f, 1f));

        DeliveryPoint point = zone.AddComponent<DeliveryPoint>();
        SerializedObject so = new SerializedObject(point);
        so.FindProperty("address").stringValue = address;
        so.ApplyModifiedPropertiesWithoutUndo();

        // The customer's door, on the far side of the zone: press E to knock (plan 4.8).
        GameObject door = MakeBlock("Door: " + address,
            groundPosition + new Vector3(0f, 1.1f, size.z / 2f + 0.15f), new Vector3(1.2f, 2.2f, 0.2f));
        ColorObject(door, new Color(0.35f, 0.2f, 0.1f));
        DeliveryDoor doorScript = door.AddComponent<DeliveryDoor>();
        SerializedObject doorSo = new SerializedObject(doorScript);
        doorSo.FindProperty("address").stringValue = address;
        doorSo.ApplyModifiedPropertiesWithoutUndo();
        return point;
    }

    // A streetlight a Signal Ghost can flicker (plan 4.10).
    public static void MakeStreetlight(Vector3 groundPosition)
    {
        GameObject pole = MakeBlock("Streetlight", groundPosition + Vector3.up * 2.5f, new Vector3(0.15f, 5f, 0.15f));
        GameObject lampObject = new GameObject("Lamp");
        lampObject.transform.position = groundPosition + Vector3.up * 4.9f;
        lampObject.transform.SetParent(pole.transform, true);
        Light lamp = lampObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 12f;
        lamp.intensity = 1.6f;
        lamp.color = new Color(1f, 0.8f, 0.55f);

        int id = Object.FindObjectsByType<Streetlight>(FindObjectsSortMode.None).Length + 1;
        Streetlight light = pole.AddComponent<Streetlight>();
        SerializedObject so = new SerializedObject(light);
        so.FindProperty("lightId").intValue = id;
        so.FindProperty("lamp").objectReferenceValue = lamp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A revive shrine (plan 4.10): bring a fallen courier's badge here and press E.
    public static void MakeReviveShrine(Vector3 groundPosition, float yaw)
    {
        GameObject shrine = MakeBlock("Revive Shrine (E with a badge)", groundPosition + Vector3.up * 0.6f, new Vector3(1f, 1.2f, 1f));
        shrine.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        ColorObject(shrine, new Color(0.9f, 0.45f, 0.1f));
        shrine.AddComponent<ReviveShrine>();
    }

    // A dangerous spot: a live wire in a puddle (hurts players standing in it).
    public static void MakeHazard(string name, Vector3 centre, Vector3 size)
    {
        GameObject hazard = MakeBlock(name, centre, size);
        hazard.layer = IgnoreRaycastLayer;
        hazard.GetComponent<BoxCollider>().isTrigger = true;
        ColorObject(hazard, new Color(0.9f, 0.9f, 0.2f));
        hazard.AddComponent<HazardZone>();
    }

    // The depot shop: press E on it to buy items.
    public static void MakeShopTerminal(Vector3 groundPosition, float yaw)
    {
        GameObject terminal = MakeBlock("Shop Terminal (E)", groundPosition + Vector3.up * 0.75f, new Vector3(1.2f, 1.5f, 0.6f));
        terminal.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        ColorObject(terminal, new Color(0.95f, 0.75f, 0.1f));

        GameObject drop = new GameObject("Drop Point");
        drop.transform.SetParent(terminal.transform, false);
        drop.transform.localPosition = new Vector3(0f, 0.2f, 1.5f);

        ShopTerminal shop = terminal.AddComponent<ShopTerminal>();
        SerializedObject so = new SerializedObject(shop);
        so.FindProperty("dropPoint").objectReferenceValue = drop.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Where players must be at dawn (plan 4.3).
    public static void MakeExtractionZone(Vector3 centre, Vector3 size)
    {
        GameObject zone = new GameObject("Extraction Zone (be here at dawn)");
        zone.layer = IgnoreRaycastLayer;
        zone.transform.position = centre;
        BoxCollider box = zone.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        zone.AddComponent<ExtractionZone>();
    }

    // A raft workbench (plan 4.4): parts go in the zone in front of it, the raft appears at the launch point.
    public static void MakeRaftSpot(Vector3 benchPosition, float yaw, Vector3 launchPosition)
    {
        GameObject bench = MakeBlock("Raft Workbench (E)", benchPosition + Vector3.up * 0.45f, new Vector3(2f, 0.9f, 0.8f));
        bench.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        ColorObject(bench, new Color(0.55f, 0.35f, 0.2f));

        GameObject zone = new GameObject("Raft Parts Zone");
        zone.layer = IgnoreRaycastLayer;
        zone.transform.SetParent(bench.transform, false);
        zone.transform.localPosition = new Vector3(0f, 0.5f, 2.2f);
        BoxCollider box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        // The bench is scaled; undo that so the zone is about 4 x 2 x 3 m.
        box.size = new Vector3(4f / 2f, 2f / 0.9f, 3f / 0.8f);

        GameObject launch = new GameObject("Raft Launch Point");
        launch.transform.position = launchPosition;

        RaftSpot spot = bench.AddComponent<RaftSpot>();
        SerializedObject so = new SerializedObject(spot);
        so.FindProperty("partsZone").objectReferenceValue = box;
        so.FindProperty("launchPoint").objectReferenceValue = launch.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
