using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

// Shared helpers used by the "Night Shift" menu scene builders.
// Builds the network prefabs (player, package, monster, managers) as assets, and the
// NetworkManager + ShiftSpawner objects every scene needs.
public static class SceneBuildHelpers
{
    public const string PrefabFolder = "Assets/_Game/Prefabs";
    public const string MaterialFolder = "Assets/_Game/Prefabs/Materials";
    public const string PackageDataFolder = "Assets/_Game/ScriptableObjects/Packages";
    public const string MonsterDataFolder = "Assets/_Game/ScriptableObjects/Monsters";
    public const string FloodFolder = "Assets/_Game/ScriptableObjects/Flood";

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

    // A material saved as an asset (prefabs cannot keep in-memory materials).
    public static Material GetMaterialAsset(string assetName, Color color)
    {
        string path = MaterialFolder + "/" + assetName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        EnsureFolder(MaterialFolder);
        material = MakeMaterial(color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    public static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
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

    // ---------- Water ----------

    // A flat box of water. The surface is the object's Y position; width/length come from the scale.
    // followsFlood = the FloodManager raises and lowers it.
    public static WaterVolume MakeWater(string name, Vector3 centreAtSurface, float width, float length, bool followsFlood = false)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = name;
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

    // ---------- Data assets ----------

    public static PackageData GetOrCreatePackageData(string assetName, string displayName, int value, float mass,
        bool fragile, bool wetSensitive, int holdersRequired, Vector3 size, Color color)
    {
        string path = PackageDataFolder + "/" + assetName + ".asset";
        PackageData data = AssetDatabase.LoadAssetAtPath<PackageData>(path);
        if (data != null)
        {
            return data;
        }

        EnsureFolder(PackageDataFolder);
        data = ScriptableObject.CreateInstance<PackageData>();
        data.displayName = displayName;
        data.baseValue = value;
        data.mass = mass;
        data.isFragile = fragile;
        data.wetSensitive = wetSensitive;
        data.holdersRequired = holdersRequired;
        data.size = size;
        data.color = color;
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    public static PackageData GetStandardPackageData()
    {
        return GetOrCreatePackageData("Package_Standard", "Standard Parcel", 100, 3f, false, false, 1,
            new Vector3(0.5f, 0.4f, 0.5f), new Color(0.8f, 0.6f, 0.3f));
    }

    public static PackageData GetFragilePackageData()
    {
        return GetOrCreatePackageData("Package_Fragile", "Fragile Parcel", 200, 2f, true, false, 1,
            new Vector3(0.5f, 0.4f, 0.5f), new Color(0.95f, 0.9f, 0.5f));
    }

    public static PackageData GetHeavyPackageData()
    {
        return GetOrCreatePackageData("Package_Heavy", "Heavy Crate (2 players)", 250, 12f, false, false, 2,
            new Vector3(1.0f, 0.6f, 0.7f), new Color(0.4f, 0.4f, 0.45f));
    }

    public static PackageData GetWetPackageData()
    {
        return GetOrCreatePackageData("Package_WetSensitive", "Documents (keep dry)", 150, 1.5f, false, true, 1,
            new Vector3(0.5f, 0.3f, 0.4f), new Color(0.9f, 0.9f, 0.95f));
    }

    public static PackageCatalog GetPackageCatalog()
    {
        string path = PackageDataFolder + "/PackageCatalog.asset";
        PackageCatalog catalog = AssetDatabase.LoadAssetAtPath<PackageCatalog>(path);
        if (catalog != null)
        {
            return catalog;
        }

        EnsureFolder(PackageDataFolder);
        catalog = ScriptableObject.CreateInstance<PackageCatalog>();
        catalog.packages = new PackageData[]
        {
            GetStandardPackageData(), GetFragilePackageData(), GetHeavyPackageData(), GetWetPackageData()
        };
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    // Flood settings asset. fast = true gives short stages for testing.
    public static FloodSettings GetFloodSettings(bool fast)
    {
        string path = FloodFolder + (fast ? "/FloodSettings_Test.asset" : "/FloodSettings_Shift.asset");
        FloodSettings settings = AssetDatabase.LoadAssetAtPath<FloodSettings>(path);
        if (settings != null)
        {
            return settings;
        }

        EnsureFolder(FloodFolder);
        settings = ScriptableObject.CreateInstance<FloodSettings>();
        float duration = fast ? 25f : 180f;
        settings.stages = new FloodSettings.Stage[]
        {
            new FloodSettings.Stage { name = "Dry streets", waterHeight = -0.5f, duration = duration },
            new FloodSettings.Stage { name = "Wading", waterHeight = 0.9f, duration = duration },
            new FloodSettings.Stage { name = "Deep", waterHeight = 2.2f, duration = duration },
        };
        settings.warningSeconds = fast ? 6f : 10f;
        settings.riseSpeed = fast ? 0.15f : 0.05f;
        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        return settings;
    }

    public static MonsterData GetJalPishachData()
    {
        string path = MonsterDataFolder + "/Monster_JalPishach.asset";
        MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (data != null)
        {
            return data;
        }

        EnsureFolder(MonsterDataFolder);
        data = ScriptableObject.CreateInstance<MonsterData>();
        data.displayName = "Jal-Pishach";
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    // ---------- Network prefabs ----------

    public class NetworkPrefabs
    {
        public GameObject player;
        public GameObject package;
        public GameObject jalPishach;
        public GameObject flood;
        public GameObject delivery;
    }

    // Builds (or rebuilds) every network prefab and returns them. Safe to call before every scene build.
    public static NetworkPrefabs BuildNetworkPrefabs()
    {
        EnsureFolder(PrefabFolder);
        GetPackageCatalog(); // make sure the data assets exist before prefabs refer to them

        NetworkPrefabs prefabs = new NetworkPrefabs();
        prefabs.player = BuildPlayerPrefab();
        prefabs.package = BuildPackagePrefab();
        prefabs.jalPishach = BuildJalPishachPrefab();
        prefabs.flood = BuildManagerPrefab("FloodManager", typeof(FloodManager), typeof(FloodHUD));
        prefabs.delivery = BuildManagerPrefab("DeliveryManager", typeof(DeliveryManager), typeof(DeliveryHUD));
        AssetDatabase.SaveAssets();
        return prefabs;
    }

    private static T Ensure<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    private static GameObject SavePrefab(GameObject instance, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void SetObjectField(Object owner, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(owner);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildPlayerPrefab()
    {
        GameObject player = new GameObject("Player");
        Ensure<NetworkObject>(player);
        Ensure<CharacterController>(player);

        NetworkPlayer networkPlayer = Ensure<NetworkPlayer>(player);
        PlayerController controller = Ensure<PlayerController>(player);
        PlayerInputReader input = Ensure<PlayerInputReader>(player);
        Ensure<PlayerStamina>(player);
        PlayerGrabber grabber = Ensure<PlayerGrabber>(player);
        PlayerNoise noise = Ensure<PlayerNoise>(player);
        MicNoiseReporter mic = Ensure<MicNoiseReporter>(player);
        PlayerVoice voice = Ensure<PlayerVoice>(player);
        Ensure<PlayerHealth>(player);

        // Camera (first person view, used only by the owner)
        GameObject cameraObject = new GameObject("Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();

        // Body (what other players see)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("Player_Body", new Color(0.9f, 0.6f, 0.15f));

        SetObjectField(controller, "cameraTransform", cameraObject.transform);
        SetObjectField(grabber, "cameraTransform", cameraObject.transform);
        SetObjectField(networkPlayer, "cameraTransform", cameraObject.transform);
        SetObjectField(networkPlayer, "bodyVisual", body);

        // Only the owner switches these on (see NetworkPlayer.SetUpLocalPlayer).
        controller.enabled = false;
        input.enabled = false;
        noise.enabled = false;
        mic.enabled = false;
        voice.enabled = false;

        return SavePrefab(player, "Player");
    }

    private static GameObject BuildPackagePrefab()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Package";
        box.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("Package", Color.white);
        box.AddComponent<Rigidbody>();
        Ensure<NetworkObject>(box);
        Ensure<NetworkTransform>(box);
        Ensure<Package>(box);
        return SavePrefab(box, "Package");
    }

    private static GameObject BuildJalPishachPrefab()
    {
        GameObject monster = new GameObject("Jal-Pishach");
        Ensure<NetworkObject>(monster);
        Ensure<NetworkTransform>(monster);
        JalPishach script = Ensure<JalPishach>(monster);

        SetObjectField(script, "data", GetJalPishachData());
        SetObjectField(script, "visualMaterial", GetMaterialAsset("Monster", Color.white));
        return SavePrefab(monster, "JalPishach");
    }

    private static GameObject BuildManagerPrefab(string name, System.Type managerType, System.Type hudType)
    {
        GameObject root = new GameObject(name);
        Ensure<NetworkObject>(root);
        root.AddComponent(managerType);
        root.AddComponent(hudType);
        return SavePrefab(root, name);
    }

    // ---------- Scene setup ----------

    // Adds the NetworkManager object and the ShiftSpawner to the current scene.
    // autoHost: skip the menu and host immediately (single-player test scenes).
    // autoStartShift: spawn the flood, orders, packages and monsters as soon as hosting starts.
    public static void MakeNetworkSetup(NetworkPrefabs prefabs, FloodSettings flood, bool autoHost, bool autoStartShift, int quota)
    {
        GameObject manager = new GameObject("NetworkManager");
        manager.AddComponent<NetworkManager>();
        manager.AddComponent<UnityTransport>();
        GameNetworkConfig config = manager.AddComponent<GameNetworkConfig>();
        NetworkBootstrap bootstrap = manager.AddComponent<NetworkBootstrap>();
        manager.AddComponent<SteamLobbyService>();
        manager.AddComponent<VoiceChatService>();
        manager.AddComponent<StaminaHUD>();
        manager.AddComponent<MonsterDebugHUD>();

        SerializedObject configSo = new SerializedObject(config);
        configSo.FindProperty("playerPrefab").objectReferenceValue = prefabs.player;
        configSo.FindProperty("packageCatalog").objectReferenceValue = GetPackageCatalog();
        configSo.FindProperty("floodSettings").objectReferenceValue = flood;
        SerializedProperty list = configSo.FindProperty("networkPrefabs");
        GameObject[] spawnable = { prefabs.package, prefabs.jalPishach, prefabs.flood, prefabs.delivery };
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
        spawnerSo.FindProperty("floodPrefab").objectReferenceValue = prefabs.flood;
        spawnerSo.FindProperty("deliveryPrefab").objectReferenceValue = prefabs.delivery;
        spawnerSo.FindProperty("packagePrefab").objectReferenceValue = prefabs.package;
        spawnerSo.FindProperty("jalPishachPrefab").objectReferenceValue = prefabs.jalPishach;
        spawnerSo.FindProperty("quota").intValue = quota;
        spawnerSo.FindProperty("autoStartShift").boolValue = autoStartShift;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();
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

    public static void MakePackageSpawn(PackageData data, Vector3 position, string destination = "")
    {
        GameObject marker = new GameObject("Package Spawn - " + data.displayName);
        marker.transform.position = position;
        PackageSpawnPoint point = marker.AddComponent<PackageSpawnPoint>();
        point.data = data;
        point.destination = destination;
    }

    public static void MakeMonsterSpawn(WaterVolume home, Vector3 position)
    {
        GameObject marker = new GameObject("Jal-Pishach Spawn");
        marker.transform.position = position;
        marker.AddComponent<MonsterSpawnPoint>().homeWater = home;
    }

    // A glowing marker zone where packages are delivered.
    public static DeliveryPoint MakeDeliveryPoint(string address, Vector3 groundPosition, Vector3 size)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "Delivery: " + address;
        zone.transform.position = groundPosition + new Vector3(0f, size.y / 2f, 0f);
        zone.transform.localScale = size;
        Object.DestroyImmediate(zone.GetComponent<BoxCollider>());
        zone.AddComponent<BoxCollider>().isTrigger = true;
        ColorObject(zone, new Color(0.2f, 1f, 0.4f, 1f));

        DeliveryPoint point = zone.AddComponent<DeliveryPoint>();
        SerializedObject so = new SerializedObject(point);
        so.FindProperty("address").stringValue = address;
        so.ApplyModifiedPropertiesWithoutUndo();
        return point;
    }
}
