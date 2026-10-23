using System.IO;
using UnityEditor;
using UnityEngine;

// Shared helpers used by the "Night Shift" menu scene builders.
public static class SceneBuildHelpers
{
    public const string PackageDataFolder = "Assets/_Game/ScriptableObjects/Packages";
    public const string MonsterDataFolder = "Assets/_Game/ScriptableObjects/Monsters";
    public const string FloodFolder = "Assets/_Game/ScriptableObjects/Flood";

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

    public static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
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

    // A flat box of water. The surface is the object's Y position; width/depth come from the scale.
    public static WaterVolume MakeWater(string name, Vector3 centreAtSurface, float width, float length)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = name;
        water.transform.position = centreAtSurface;
        water.transform.localScale = new Vector3(width, 0.02f, length);
        Object.DestroyImmediate(water.GetComponent<BoxCollider>());
        water.AddComponent<BoxCollider>().isTrigger = true;
        ColorObject(water, new Color(0.1f, 0.4f, 0.9f, 1f));
        return water.AddComponent<WaterVolume>();
    }

    // Creates the player with camera, grabber, HUD hooks. Returns the player object.
    public static GameObject MakePlayer(Vector3 position)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = position;
        player.AddComponent<CharacterController>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerStamina stamina = player.AddComponent<PlayerStamina>();
        PlayerGrabber grabber = player.AddComponent<PlayerGrabber>();
        player.AddComponent<PlayerNoise>();
        player.AddComponent<PlayerHealth>();

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            cam = camObject.AddComponent<Camera>();
            camObject.AddComponent<AudioListener>();
        }
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.transform.localRotation = Quaternion.identity;

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject grabberSo = new SerializedObject(grabber);
        grabberSo.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        grabberSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject hud = new GameObject("StaminaHUD");
        StaminaHUD hudScript = hud.AddComponent<StaminaHUD>();
        SerializedObject hudSo = new SerializedObject(hudScript);
        hudSo.FindProperty("stamina").objectReferenceValue = stamina;
        hudSo.FindProperty("player").objectReferenceValue = controller;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    public static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    public static PackageData GetOrCreatePackageData(string assetName, string displayName, int value, float mass,
        bool fragile, bool wetSensitive)
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
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    public static PackageData GetStandardPackageData()
    {
        return GetOrCreatePackageData("Package_Standard", "Standard Parcel", 100, 3f, false, false);
    }

    public static PackageData GetFragilePackageData()
    {
        return GetOrCreatePackageData("Package_Fragile", "Fragile Parcel", 200, 2f, true, false);
    }

    public static PackageData GetHeavyPackageData()
    {
        return GetOrCreatePackageData("Package_Heavy", "Heavy Crate", 250, 12f, false, false);
    }

    public static PackageData GetWetPackageData()
    {
        return GetOrCreatePackageData("Package_WetSensitive", "Documents (keep dry)", 150, 1.5f, false, true);
    }

    public static Package MakePackage(PackageData data, Vector3 position, Color color, string destination = "")
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Package - " + data.displayName;
        box.transform.position = position;
        box.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        box.AddComponent<Rigidbody>();
        ColorObject(box, color);
        Package package = box.AddComponent<Package>();
        package.SetData(data);
        package.SetDestination(destination);
        return package;
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

    // Adds a FloodManager + FloodHUD wired to the given water volumes.
    public static FloodManager MakeFlood(FloodSettings settings, params WaterVolume[] waters)
    {
        GameObject floodObject = new GameObject("Flood");
        FloodManager manager = floodObject.AddComponent<FloodManager>();
        FloodHUD hud = floodObject.AddComponent<FloodHUD>();

        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("settings").objectReferenceValue = settings;
        SerializedProperty list = managerSo.FindProperty("floodWaters");
        list.arraySize = waters.Length;
        for (int i = 0; i < waters.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = waters[i];
        }
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("flood").objectReferenceValue = manager;
        hudSo.ApplyModifiedPropertiesWithoutUndo();
        return manager;
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

    // Creates a Jal-Pishach that lives in the given water.
    public static JalPishach MakeJalPishach(WaterVolume home, Vector3 startPosition)
    {
        GameObject monsterObject = new GameObject("Jal-Pishach");
        monsterObject.transform.position = startPosition;
        JalPishach monster = monsterObject.AddComponent<JalPishach>();

        SerializedObject so = new SerializedObject(monster);
        so.FindProperty("data").objectReferenceValue = GetJalPishachData();
        so.FindProperty("homeWater").objectReferenceValue = home;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindFirstObjectByType<MonsterDebugHUD>() == null)
        {
            new GameObject("MonsterDebugHUD").AddComponent<MonsterDebugHUD>();
        }
        return monster;
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

    // DeliveryManager + wallet + HUD. Call after MakePlayer so the HUD can find the grabber.
    public static DeliveryManager MakeDeliveryManager(int quota)
    {
        GameObject root = new GameObject("Deliveries");
        DeliveryManager manager = root.AddComponent<DeliveryManager>();
        DeliveryHUD hud = root.AddComponent<DeliveryHUD>();

        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("quota").intValue = quota;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("manager").objectReferenceValue = manager;
        hudSo.FindProperty("grabber").objectReferenceValue = Object.FindFirstObjectByType<PlayerGrabber>();
        hudSo.ApplyModifiedPropertiesWithoutUndo();
        return manager;
    }
}
