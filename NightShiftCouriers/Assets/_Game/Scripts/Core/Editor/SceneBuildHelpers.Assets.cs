using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Data assets (packages, items, monsters, flood) and network prefabs, created or refreshed by the scene builders.
public static partial class SceneBuildHelpers
{
    public const string PrefabFolder = "Assets/_Game/Prefabs";
    public const string MaterialFolder = "Assets/_Game/Prefabs/Materials";
    public const string PackageDataFolder = "Assets/_Game/ScriptableObjects/Packages";
    public const string ItemDataFolder = "Assets/_Game/ScriptableObjects/Items";
    public const string MonsterDataFolder = "Assets/_Game/ScriptableObjects/Monsters";
    public const string FloodFolder = "Assets/_Game/ScriptableObjects/Flood";
    public const string CustomerFolder = "Assets/_Game/ScriptableObjects/Customers";
    public const string LoreFolder = "Assets/_Game/ScriptableObjects/Lore";
    public const string RoleFolder = "Assets/_Game/ScriptableObjects/Roles";
    public const string RetroShaderPath = "Assets/_Game/Shaders/RetroVHS.shader";

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

    // ---------- Packages ----------

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

    public static PackageData GetRaftFloatersData()
    {
        return GetRaftPart("RaftPart_Floaters", "Raft Floaters (sealed drums)", 3f, 1,
            new Vector3(0.8f, 0.9f, 0.8f), new Color(0.15f, 0.35f, 0.85f), RaftPartType.Floaters);
    }

    public static PackageData GetRaftPlatformData()
    {
        return GetRaftPart("RaftPart_Platform", "Raft Platform (old door)", 14f, 2,
            new Vector3(1.6f, 0.12f, 1.1f), new Color(0.5f, 0.32f, 0.18f), RaftPartType.Platform);
    }

    public static PackageData GetRaftLashingData()
    {
        return GetRaftPart("RaftPart_Lashing", "Lashing Rope", 1f, 1,
            new Vector3(0.4f, 0.3f, 0.4f), new Color(0.8f, 0.7f, 0.45f), RaftPartType.LashingRope);
    }

    private static PackageData GetRaftPart(string assetName, string displayName, float mass, int holders,
        Vector3 size, Color color, RaftPartType part)
    {
        PackageData data = GetOrCreatePackageData(assetName, displayName, 0, mass, false, false, holders, size, color);
        if (data.raftPart != part)
        {
            data.raftPart = part;
            EditorUtility.SetDirty(data);
        }
        return data;
    }

    // Always refreshed, so new package types reach catalogs made by older builds.
    public static PackageCatalog GetPackageCatalog()
    {
        string path = PackageDataFolder + "/PackageCatalog.asset";
        PackageCatalog catalog = AssetDatabase.LoadAssetAtPath<PackageCatalog>(path);
        if (catalog == null)
        {
            EnsureFolder(PackageDataFolder);
            catalog = ScriptableObject.CreateInstance<PackageCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        catalog.packages = new PackageData[]
        {
            GetStandardPackageData(), GetFragilePackageData(), GetHeavyPackageData(), GetWetPackageData(),
            GetRaftFloatersData(), GetRaftPlatformData(), GetRaftLashingData()
        };
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    // ---------- Items (plan 4.5) ----------

    public static ItemData GetItem(string assetName, string displayName, ItemType type, int price, int uses,
        float duration, string description, Color color)
    {
        string path = ItemDataFolder + "/" + assetName + ".asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null)
        {
            return item;
        }

        EnsureFolder(ItemDataFolder);
        item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName = displayName;
        item.type = type;
        item.price = price;
        item.uses = uses;
        item.duration = duration;
        item.description = description;
        item.color = color;
        item.charmAgainst = MonsterKind.Vetala;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    public static ItemData FlashlightItem => GetItem("Item_Flashlight", "Flashlight", ItemType.Flashlight, 40, 180, 0f,
        "Right mouse: on/off. 180 s of battery. Draws the Chudail in, wakes the Vetala, makes the Jal-Pishach back off.", Color.yellow);
    public static ItemData RopeItem => GetItem("Item_Rope", "Rope", ItemType.Rope, 60, 1, 0f,
        "Right mouse at a ledge: hangs a climbable rope down to the ground.", new Color(0.7f, 0.55f, 0.3f));
    public static ItemData PlankItem => GetItem("Item_Plank", "Plank", ItemType.Plank, 50, 1, 0f,
        "Right mouse: lay a 4 m bridge in front of you. On a raft it is a paddle.", new Color(0.6f, 0.4f, 0.2f));
    public static ItemData RadioItem => GetItem("Item_Radio", "Radio", ItemType.Radio, 80, 1, 0f,
        "Carry it to talk to everyone else who has one, at any range.", new Color(0.3f, 0.3f, 0.3f));
    public static ItemData DecoyItem => GetItem("Item_Decoy", "Firecracker (decoy)", ItemType.Decoy, 30, 1, 0f,
        "Right mouse: throw. Bangs loudly a few times to pull monsters away.", Color.red);
    public static ItemData SaltItem => GetItem("Item_Salt", "Salt Pouch", ItemType.SaltPouch, 45, 1, 60f,
        "Right mouse: pour a ward circle at your feet for 60 s. The Chudail cannot cross it.", Color.white);
    public static ItemData CharmItem => GetItem("Item_Charm", "Lemon-Chilli Charm", ItemType.LemonChilliCharm, 55, 1, 90f,
        "Right mouse: the Vetala cannot see you for 90 s.", new Color(0.6f, 0.9f, 0.2f));
    public static ItemData RepairItem => GetItem("Item_RepairKit", "Repair Kit", ItemType.RepairKit, 60, 2, 0f,
        "Right mouse: patch the raft you stand on, or mend the fragile package you hold.", new Color(0.9f, 0.3f, 0.3f));
    public static ItemData GlowItem => GetItem("Item_GlowSticks", "Glow Sticks", ItemType.GlowSticks, 20, 3, 0f,
        "Right mouse: drop a glow stick to mark a path. Does not attract monsters.", Color.green);
    public static ItemData BadgeItem
    {
        get
        {
            ItemData badge = GetItem("Item_CourierBadge", "Courier ID Badge", ItemType.CourierBadge, 0, 1, 0f,
                "A fallen courier's badge. Take it to a shrine (E) to bring them back.", new Color(0.2f, 0.8f, 1f));
            if (badge.inShop)
            {
                badge.inShop = false;
                EditorUtility.SetDirty(badge);
            }
            return badge;
        }
    }

    public static ItemData LashingItem => GetItem("Item_LashingRope", "Lashing Rope (raft part)", ItemType.LashingRope, 200, 1, 0f,
        "One of the three raft parts. Appears next to the shop.", new Color(0.8f, 0.7f, 0.45f));

    // Always refreshed so new items reach catalogs made by older builds.
    public static ItemCatalog GetItemCatalog()
    {
        string path = ItemDataFolder + "/ItemCatalog.asset";
        ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
        if (catalog == null)
        {
            EnsureFolder(ItemDataFolder);
            catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        catalog.items = new ItemData[]
        {
            FlashlightItem, RopeItem, PlankItem, RadioItem, DecoyItem,
            SaltItem, CharmItem, RepairItem, GlowItem, LashingItem, BadgeItem
        };
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    // ---------- Milestone 4 data ----------

    private static T GetOrCreateAsset<T>(string folder, string assetName, out bool created) where T : ScriptableObject
    {
        string path = folder + "/" + assetName + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        created = asset == null;
        if (created)
        {
            EnsureFolder(folder);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    // Cursed package outcomes (plan 4.9). Created once; tweak the weights in the Inspector afterwards.
    public static CurseTable GetCurseTable()
    {
        CurseTable table = GetOrCreateAsset<CurseTable>(PackageDataFolder, "CurseTable", out bool created);
        if (created)
        {
            table.entries = new CurseTable.Entry[]
            {
                new CurseTable.Entry { outcome = CurseOutcome.RareItem, weight = 25f, message = "Something useful falls out!" },
                new CurseTable.Entry { outcome = CurseOutcome.BigMoney, weight = 20f, message = "It is stuffed with cash!" },
                new CurseTable.Entry { outcome = CurseOutcome.LoreReveal, weight = 15f, message = "Inside: an old letter. (New journal entry)" },
                new CurseTable.Entry { outcome = CurseOutcome.MonsterReleased, weight = 15f, message = "Something crawls out of it!" },
                new CurseTable.Entry { outcome = CurseOutcome.FloodJump, weight = 12f, message = "The water surges up early!" },
                new CurseTable.Entry { outcome = CurseOutcome.Screaming, weight = 13f, message = "It starts SCREAMING." },
            };
            EditorUtility.SetDirty(table);
        }
        return table;
    }

    private static CustomerData GetCustomer(string assetName, string customerName, string address, string note,
        CustomerRule rule, int loreEntry)
    {
        CustomerData customer = GetOrCreateAsset<CustomerData>(CustomerFolder, assetName, out bool created);
        if (created)
        {
            customer.customerName = customerName;
            customer.address = address;
            customer.note = note;
            customer.rule = rule;
            customer.loreEntry = loreEntry;
            EditorUtility.SetDirty(customer);
        }
        return customer;
    }

    // Customers and their notes (plan 4.8). Always refreshed so new customers reach older builds.
    public static CustomerDirectory GetCustomerDirectory()
    {
        CustomerDirectory directory = GetOrCreateAsset<CustomerDirectory>(CustomerFolder, "CustomerDirectory", out bool created);
        directory.customers = new CustomerData[]
        {
            GetCustomer("Customer_Pillai", "Mr. Pillai", "12 Market Street",
                "Leave it inside the shutter. If the lights flicker, do not wait for me.", CustomerRule.NobodyAnswers, 0),
            GetCustomer("Customer_Priest", "The temple priest", "7 Temple Lane",
                "Bring it before the second siren. After that, the temple belongs to the water.", CustomerRule.BeforeSecondRise, 1),
            GetCustomer("Customer_Iyer", "Mrs. Iyer, 3rd floor", "Mrs. Iyer, Rooftop Flat",
                "Knock once. Do NOT knock twice.", CustomerRule.KnockOnce, 2),
            GetCustomer("Customer_LaneHouse", "(no name given)", "Lane House Roof",
                "Come alone. Whatever you hear on the way up, it is not your friends.", CustomerRule.ComeAlone, 3),
            GetCustomer("Customer_Test", "Test customer", "Test Door",
                "Knock once, exactly once, then leave it at the door. Lights off, please.", CustomerRule.KnockOnce, 4),
        };
        EditorUtility.SetDirty(directory);
        return directory;
    }

    // The depot journal (plan 4.8).
    public static LoreBook GetLoreBook()
    {
        LoreBook book = GetOrCreateAsset<LoreBook>(LoreFolder, "LoreBook", out bool created);
        if (created)
        {
            book.entries = new LoreBook.Entry[]
            {
                new LoreBook.Entry { title = "Shift Handbook, page 1",
                    text = "Couriers will complete the night's orders before dawn. Couriers will not discuss what they see. "
                         + "The company is not responsible for anything that follows you home." },
                new LoreBook.Entry { title = "The temple bell",
                    text = "The priest says the bell was rung every monsoon night for three hundred years, to tell the water "
                         + "where the town ends. This year nobody rang it." },
                new LoreBook.Entry { title = "Mrs. Iyer",
                    text = "Mrs. Iyer has lived on the third floor since 1974. Her neighbours moved out in 1975. "
                         + "She orders the same parcel every week. Nobody knows who sends it." },
                new LoreBook.Entry { title = "Footprints",
                    text = "A note pinned to the depot board: 'If the footprints face you, she is walking away from you. "
                         + "If they face away... run.'" },
                new LoreBook.Entry { title = "Knock once",
                    text = "An old courier's diary: 'Once is a delivery. Twice is an invitation.'" },
                new LoreBook.Entry { title = "The canal",
                    text = "Before the drains, the canal was a river. Before the river, the old ones say, it was a mouth." },
                new LoreBook.Entry { title = "Hanging things",
                    text = "Do not shine your light up into the trees. They are not sleeping. They are waiting for the light." },
                new LoreBook.Entry { title = "Company memo",
                    text = "Quota targets will continue to rise. Couriers who do not meet quota will be let go. "
                         + "Couriers who are let go are not to be spoken of." },
            };
            EditorUtility.SetDirty(book);
        }
        return book;
    }

    private static RoleData GetRole(string assetName, string roleName, string description, RolePassive passive, ItemData item)
    {
        RoleData role = GetOrCreateAsset<RoleData>(RoleFolder, assetName, out bool created);
        if (created)
        {
            role.roleName = roleName;
            role.description = description;
            role.passive = passive;
            role.startingItem = item;
            EditorUtility.SetDirty(role);
        }
        return role;
    }

    // Roles (plan 4.7). Always refreshed.
    public static RoleCatalog GetRoleCatalog()
    {
        RoleCatalog catalog = GetOrCreateAsset<RoleCatalog>(RoleFolder, "RoleCatalog", out bool created);
        catalog.roles = new RoleData[]
        {
            GetRole("Role_Courier", "Courier", "No special skill.", RolePassive.None, null),
            GetRole("Role_Navigator", "Navigator", "Sees delivery addresses from far away. Starts with a radio.", RolePassive.Navigator, RadioItem),
            GetRole("Role_Builder", "Builder", "Builds rafts faster; rafts they ride often shrug off hits. Starts with a repair kit.", RolePassive.Builder, RepairItem),
            GetRole("Role_Runner", "Runner", "More stamina, faster climbing. Starts with a rope.", RolePassive.Runner, RopeItem),
            GetRole("Role_Lookout", "Lookout", "Sees nearby monsters marked on screen. Starts with glow sticks.", RolePassive.Lookout, GlowItem),
        };
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    // ---------- Flood ----------

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

    // ---------- Monsters ----------

    private static MonsterData GetMonsterData(string assetName, System.Action<MonsterData> setUp)
    {
        string path = MonsterDataFolder + "/" + assetName + ".asset";
        MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (data != null)
        {
            return data;
        }

        EnsureFolder(MonsterDataFolder);
        data = ScriptableObject.CreateInstance<MonsterData>();
        setUp(data);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    public static MonsterData GetJalPishachData()
    {
        return GetMonsterData("Monster_JalPishach", data =>
        {
            data.displayName = "Jal-Pishach";
            data.kind = MonsterKind.JalPishach;
        });
    }

    public static MonsterData GetChudailData()
    {
        return GetMonsterData("Monster_Chudail", data =>
        {
            data.displayName = "Chudail";
            data.kind = MonsterKind.Chudail;
            data.blockedBySalt = true;
            data.wanderSpeed = 1.4f;
            data.investigateSpeed = 2.6f;
            data.huntSpeed = 4.2f;
            data.hearingRadiusPerLoudness = 6f;
            data.huntNoiseFraction = 0.3f;
            data.sightRange = 18f;
            data.forgetDistance = 35f;
            data.loseTargetSeconds = 6f;
            data.attackRange = 1.6f;
            data.attackWindup = 0.8f;
            data.attackDamage = 40f;
            data.cooldownSeconds = 8f;
            data.searchSeconds = 6f;
        });
    }

    public static MonsterData GetVetalaData()
    {
        return GetMonsterData("Monster_Vetala", data =>
        {
            data.displayName = "Vetala";
            data.kind = MonsterKind.Vetala;
            data.wanderSpeed = 3f;
            data.investigateSpeed = 3f;
            data.huntSpeed = 9f;
            data.hearingRadiusPerLoudness = 0f;
            data.sightRange = 12f;
            data.forgetDistance = 12f;
            data.loseTargetSeconds = 1f;
            data.attackRange = 1.6f;
            data.attackWindup = 0.4f;
            data.attackDamage = 30f;
            data.cooldownSeconds = 10f;
        });
    }

    // ---------- Network prefabs ----------

    public class NetworkPrefabs
    {
        public GameObject player;
        public GameObject campaign;
        public GameObject flood;
        public GameObject delivery;
        public GameObject package;
        public GameObject jalPishach;
        public GameObject chudail;
        public GameObject vetala;
        public GameObject rope;
        public GameObject plank;
        public GameObject decoy;
        public GameObject saltWard;
        public GameObject glowStick;
        public GameObject worldItem;
        public GameObject raft;
        public GameObject ghostWorld;

        // Everything the server may spawn (the player prefab is registered separately).
        public GameObject[] AllSpawnable()
        {
            return new GameObject[]
            {
                campaign, flood, delivery, package, jalPishach, chudail, vetala,
                rope, plank, decoy, saltWard, glowStick, worldItem, raft, ghostWorld
            };
        }
    }

    // Builds (or rebuilds) every network prefab and returns them. Safe to call before every scene build.
    public static NetworkPrefabs BuildNetworkPrefabs()
    {
        EnsureFolder(PrefabFolder);
        GetPackageCatalog(); // make sure the data assets exist before prefabs refer to them
        GetItemCatalog();

        NetworkPrefabs prefabs = new NetworkPrefabs();
        prefabs.player = BuildPlayerPrefab();
        prefabs.campaign = BuildManagerPrefab("CampaignManager", typeof(CampaignManager), null);
        prefabs.flood = BuildManagerPrefab("FloodManager", typeof(FloodManager), typeof(FloodHUD));
        prefabs.delivery = BuildManagerPrefab("DeliveryManager", typeof(DeliveryManager), typeof(DeliveryHUD));
        prefabs.package = BuildPackagePrefab();
        prefabs.jalPishach = BuildJalPishachPrefab();
        prefabs.chudail = BuildChudailPrefab();
        prefabs.vetala = BuildVetalaPrefab();
        prefabs.rope = BuildRopePrefab();
        prefabs.plank = BuildPlankPrefab();
        prefabs.decoy = BuildDecoyPrefab();
        prefabs.saltWard = BuildSaltWardPrefab();
        prefabs.glowStick = BuildGlowStickPrefab();
        prefabs.worldItem = BuildWorldItemPrefab();
        prefabs.raft = BuildRaftPrefab();
        prefabs.ghostWorld = BuildManagerPrefab("GhostWorld", typeof(GhostWorld), null);
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

    // A child shape with no collider, for looks only.
    private static GameObject AddVisual(Transform parent, PrimitiveType shape, Vector3 localPosition, Vector3 scale, string material, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(shape);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset(material, color);
        return visual;
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
        LocalMicrophone microphone = Ensure<LocalMicrophone>(player);
        MicNoiseReporter mic = Ensure<MicNoiseReporter>(player);
        Ensure<VoiceClipRecorder>(player);
        PlayerVoice voice = Ensure<PlayerVoice>(player);
        Ensure<PlayerHealth>(player);
        PlayerInventory inventory = Ensure<PlayerInventory>(player);
        Ensure<SignalGhost>(player);
        BodyCam bodyCam = Ensure<BodyCam>(player);

        // Camera (first person view, used only by the owner)
        GameObject cameraObject = new GameObject("Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera playerCamera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        RetroCameraEffect retro = cameraObject.AddComponent<RetroCameraEffect>();
        SetObjectField(retro, "shader", AssetDatabase.LoadAssetAtPath<Shader>(RetroShaderPath));
        SetObjectField(bodyCam, "sourceCamera", playerCamera);

        // Body: what other players see, and what they bump into (owners hide their own).
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("Player_Body", new Color(0.9f, 0.6f, 0.15f));

        // Flashlight beam, switched on by the flashlight item. Everyone sees it.
        GameObject lightObject = new GameObject("Flashlight");
        lightObject.transform.SetParent(player.transform, false);
        lightObject.transform.localPosition = new Vector3(0.2f, 1.45f, 0.25f);
        Light beam = lightObject.AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.range = 20f;
        beam.spotAngle = 55f;
        beam.intensity = 2.5f;
        beam.enabled = false;

        SetObjectField(controller, "cameraTransform", cameraObject.transform);
        SetObjectField(grabber, "cameraTransform", cameraObject.transform);
        SetObjectField(networkPlayer, "cameraTransform", cameraObject.transform);
        SetObjectField(networkPlayer, "bodyVisual", body);
        SetObjectField(inventory, "flashlight", beam);

        // Only the owner switches these on (see NetworkPlayer.SetUpLocalPlayer).
        controller.enabled = false;
        input.enabled = false;
        noise.enabled = false;
        microphone.enabled = false;
        mic.enabled = false;
        voice.enabled = false;
        bodyCam.enabled = false;

        // Players are on "Ignore Raycast": the navigation map and rafts ignore them.
        SetLayerRecursively(player, IgnoreRaycastLayer);
        return SavePrefab(player, "Player");
    }

    private static GameObject BuildManagerPrefab(string name, System.Type managerType, System.Type hudType)
    {
        GameObject root = new GameObject(name);
        Ensure<NetworkObject>(root);
        root.AddComponent(managerType);
        if (hudType != null)
        {
            root.AddComponent(hudType);
        }
        return SavePrefab(root, name);
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

    private static GameObject BuildChudailPrefab()
    {
        GameObject monster = new GameObject("Chudail");
        Ensure<NetworkObject>(monster);
        Ensure<NetworkTransform>(monster);
        NavMeshAgent agent = Ensure<NavMeshAgent>(monster);
        agent.radius = 0.35f;
        agent.height = 2f;
        agent.enabled = false; // the server turns it on; clients just follow the synced position
        Chudail script = Ensure<Chudail>(monster);
        SetObjectField(script, "data", GetChudailData());
        SetObjectField(script, "visualMaterial", GetMaterialAsset("Monster", Color.white));
        return SavePrefab(monster, "Chudail");
    }

    private static GameObject BuildVetalaPrefab()
    {
        GameObject monster = new GameObject("Vetala");
        Ensure<NetworkObject>(monster);
        Ensure<NetworkTransform>(monster);
        Vetala script = Ensure<Vetala>(monster);
        SetObjectField(script, "data", GetVetalaData());
        SetObjectField(script, "visualMaterial", GetMaterialAsset("Monster", Color.white));
        return SavePrefab(monster, "Vetala");
    }

    private static GameObject BuildRopePrefab()
    {
        GameObject rope = new GameObject("Rope");
        Ensure<NetworkObject>(rope);
        Ensure<BoxCollider>(rope);
        Ensure<Climbable>(rope);
        PlacedRope script = Ensure<PlacedRope>(rope);
        GameObject visual = AddVisual(rope.transform, PrimitiveType.Cylinder, Vector3.down, new Vector3(0.06f, 1f, 0.06f),
            "Rope", new Color(0.7f, 0.55f, 0.3f));
        SetObjectField(script, "visual", visual.transform);
        return SavePrefab(rope, "Rope");
    }

    private static GameObject BuildPlankPrefab()
    {
        GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "Plank";
        plank.transform.localScale = new Vector3(0.7f, 0.1f, 4f);
        plank.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("Plank", new Color(0.6f, 0.4f, 0.2f));
        Ensure<NetworkObject>(plank);
        return SavePrefab(plank, "Plank");
    }

    private static GameObject BuildDecoyPrefab()
    {
        GameObject decoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        decoy.name = "Decoy";
        decoy.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
        decoy.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("Decoy", Color.red);
        decoy.AddComponent<Rigidbody>().mass = 0.3f;
        Ensure<NetworkObject>(decoy);
        Ensure<NetworkTransform>(decoy);
        Ensure<Decoy>(decoy);
        return SavePrefab(decoy, "Decoy");
    }

    private static GameObject BuildSaltWardPrefab()
    {
        GameObject ward = new GameObject("SaltWard");
        Ensure<NetworkObject>(ward);
        Ensure<NavMeshObstacle>(ward);
        Ensure<SaltWard>(ward);
        return SavePrefab(ward, "SaltWard");
    }

    private static GameObject BuildGlowStickPrefab()
    {
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        stick.name = "GlowStick";
        stick.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
        stick.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("GlowStick", Color.green);
        stick.AddComponent<Rigidbody>().mass = 0.1f;
        Ensure<NetworkObject>(stick);
        Ensure<NetworkTransform>(stick);
        Ensure<TimedDespawn>(stick);

        GameObject lightObject = new GameObject("Glow");
        lightObject.transform.SetParent(stick.transform, false);
        Light glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.3f, 1f, 0.3f);
        glow.range = 5f;
        glow.intensity = 1.5f;
        return SavePrefab(stick, "GlowStick");
    }

    private static GameObject BuildWorldItemPrefab()
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = "WorldItem";
        item.transform.localScale = Vector3.one * 0.35f;
        item.GetComponent<Renderer>().sharedMaterial = GetMaterialAsset("WorldItem", Color.white);
        Ensure<NetworkObject>(item);
        Ensure<WorldItem>(item);
        return SavePrefab(item, "WorldItem");
    }

    private static GameObject BuildRaftPrefab()
    {
        GameObject raft = new GameObject("Raft");
        BoxCollider deck = Ensure<BoxCollider>(raft);
        deck.center = new Vector3(0f, 0.15f, 0f);
        deck.size = new Vector3(2.4f, 0.3f, 1.8f);
        Ensure<NetworkObject>(raft);
        Ensure<NetworkTransform>(raft);
        Ensure<MovingPlatform>(raft);
        Ensure<Raft>(raft);

        AddVisual(raft.transform, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(2.4f, 0.15f, 1.8f),
            "RaftDeck", new Color(0.5f, 0.32f, 0.18f));
        Vector3[] drums =
        {
            new Vector3(-0.85f, 0f, -0.6f), new Vector3(0.85f, 0f, -0.6f),
            new Vector3(-0.85f, 0f, 0.6f), new Vector3(0.85f, 0f, 0.6f)
        };
        foreach (Vector3 drum in drums)
        {
            AddVisual(raft.transform, PrimitiveType.Cylinder, drum, new Vector3(0.5f, 0.3f, 0.5f),
                "RaftDrum", new Color(0.15f, 0.35f, 0.85f));
        }
        return SavePrefab(raft, "Raft");
    }
}
