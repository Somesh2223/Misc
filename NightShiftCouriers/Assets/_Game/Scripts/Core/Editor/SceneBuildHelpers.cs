using System.IO;
using UnityEditor;
using UnityEngine;

// Shared helpers used by the "Night Shift" menu scene builders.
public static class SceneBuildHelpers
{
    public const string PackageDataFolder = "Assets/_Game/ScriptableObjects/Packages";
    public const string MonsterDataFolder = "Assets/_Game/ScriptableObjects/Monsters";

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

    public static Package MakePackage(PackageData data, Vector3 position, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Package - " + data.displayName;
        box.transform.position = position;
        box.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        box.AddComponent<Rigidbody>();
        ColorObject(box, color);
        Package package = box.AddComponent<Package>();
        package.SetData(data);
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
}
