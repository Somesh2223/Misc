using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Night Shift > Create Test_Player Scene
// Builds a greybox scene with a player, floor, steps, a low ceiling and a pool,
// then saves it to Assets/_Game/Scenes/Test_Player/Test_Player.unity
public static class TestPlayerSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Player/Test_Player.unity";

    [MenuItem("Night Shift/Create Test_Player Scene")]
    public static void Build()
    {
        EnableNewInputSystem();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(60f, 1f, 60f);

        // Pool: four walls around a slab, filled with water.
        BuildPool();

        // Walk-through test props
        MakeBlock("Step 0.3m", new Vector3(-6f, 0.15f, 4f), new Vector3(3f, 0.3f, 3f));
        MakeBlock("Step 0.6m", new Vector3(-6f, 0.3f, 7f), new Vector3(3f, 0.6f, 3f));
        MakeBlock("Box 1m (jump)", new Vector3(-10f, 0.5f, 4f), new Vector3(2f, 1f, 2f));
        MakeBlock("Low ceiling (crouch under)", new Vector3(6f, 1.4f, 5f), new Vector3(4f, 0.2f, 4f));
        MakeBlock("Long strip (sprint)", new Vector3(0f, 0.05f, 20f), new Vector3(3f, 0.1f, 30f));

        // Player
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.1f, -8f);
        player.AddComponent<CharacterController>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerStamina stamina = player.AddComponent<PlayerStamina>();

        // Move the default camera under the player
        Camera cam = Camera.main;
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.transform.localRotation = Quaternion.identity;

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // HUD
        GameObject hud = new GameObject("StaminaHUD");
        StaminaHUD hudScript = hud.AddComponent<StaminaHUD>();
        SerializedObject hudSo = new SerializedObject(hudScript);
        hudSo.FindProperty("stamina").objectReferenceValue = stamina;
        hudSo.FindProperty("player").objectReferenceValue = controller;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Created " + ScenePath + ". Press Play, then WASD / Shift / Ctrl / Space.");
    }

    private static void BuildPool()
    {
        // Walled pool 12 x 12 m, 2.5 m tall walls, at x = 12..24, z = -6..6 (centre 18, 0)
        
        Vector3 centre = new Vector3(18f, 0f, 0f);
        float wallHeight = 2.5f;
        float half = 6f;

        MakeBlock("Pool Bottom", centre + new Vector3(0f, 0.05f, 0f), new Vector3(12f, 0.1f, 12f));
        MakeBlock("Pool Wall N", centre + new Vector3(0f, wallHeight / 2f, half + 0.25f), new Vector3(12.5f, wallHeight, 0.5f));
        MakeBlock("Pool Wall S", centre + new Vector3(0f, wallHeight / 2f, -half - 0.25f), new Vector3(12.5f, wallHeight, 0.5f));
        MakeBlock("Pool Wall E", centre + new Vector3(half + 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 12f));
        MakeBlock("Pool Wall W", centre + new Vector3(-half - 0.25f, wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 12f));
        // Ramp-like steps on the west side so you can climb out
        MakeBlock("Pool Step 1", centre + new Vector3(-half + 0.75f, 0.4f, 0f), new Vector3(1.5f, 0.8f, 4f));
        MakeBlock("Pool Step 2", centre + new Vector3(-half + 2.25f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 4f));

        // Water volume: surface at y = 2.0 (about 1.9 m deep over the bottom)
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Water";
        water.transform.position = new Vector3(centre.x, 2.0f, centre.z);
        water.transform.localScale = new Vector3(12f, 0.02f, 12f);
        Object.DestroyImmediate(water.GetComponent<BoxCollider>());
        water.AddComponent<BoxCollider>().isTrigger = true;
        water.AddComponent<WaterVolume>();

        Renderer r = water.GetComponent<Renderer>();
        Material blue = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        blue.color = new Color(0.1f, 0.4f, 0.9f, 1f);
        r.sharedMaterial = blue;

        // Shallow puddle for wading (0.5 m deep)
        GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        puddle.name = "Shallow Water";
        puddle.transform.position = new Vector3(-18f, 0.5f, 0f);
        puddle.transform.localScale = new Vector3(10f, 0.02f, 10f);
        Object.DestroyImmediate(puddle.GetComponent<BoxCollider>());
        puddle.AddComponent<BoxCollider>().isTrigger = true;
        puddle.AddComponent<WaterVolume>();
        puddle.GetComponent<Renderer>().sharedMaterial = blue;
    }

    private static void MakeBlock(string name, Vector3 position, Vector3 scale)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.localScale = scale;
    }

    // The player scripts use the new Input System, so make sure it is the active input handler.
    private static void EnableNewInputSystem()
    {
        SerializedObject settings = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty handler = settings.FindProperty("activeInputHandler");
        if (handler != null && handler.intValue == 0)
        {
            handler.intValue = 1; // 1 = Input System Package (new)
            settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.LogWarning("Switched active input handler to the new Input System. Restart the editor if Unity asks.");
        }
    }
}
