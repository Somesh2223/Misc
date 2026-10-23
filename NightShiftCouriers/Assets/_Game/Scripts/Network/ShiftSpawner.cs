using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Runs the night's loop on the SERVER (plan section 3): Depot -> Shift -> Results -> Depot.
// Everything that matters (flood, orders, packages, monsters, items, rafts) is spawned here at run time,
// so the scene itself holds no networked objects. Markers in the scene say where things go.
public class ShiftSpawner : MonoBehaviour
{
    public static ShiftSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject campaignPrefab;
    [SerializeField] private GameObject ghostWorldPrefab;
    [SerializeField] private GameObject floodPrefab;
    [SerializeField] private GameObject deliveryPrefab;
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private GameObject jalPishachPrefab;
    [SerializeField] private GameObject chudailPrefab;
    [SerializeField] private GameObject vetalaPrefab;

    [Header("Shift")]
    [SerializeField, Tooltip("Start the shift as soon as the host starts (test scenes). Off = the host presses START SHIFT.")]
    private bool autoStartShift = false;
    [SerializeField, Tooltip("Test scenes: start the run with this much money (-1 = the normal amount).")]
    private int startingMoneyOverride = -1;
    [SerializeField, Range(0f, 1f), Tooltip("Chance that a package with an address is cursed (plan 4.9).")]
    private float cursedChance = 0.15f;

    private readonly List<NetworkObject> shiftObjects = new List<NetworkObject>();
    private SteamLobbyService steam;
    private bool campaignSpawned;

    public bool ShiftStarted => CampaignManager.Instance != null && CampaignManager.Instance.Phase != ShiftPhase.Depot;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        steam = FindFirstObjectByType<SteamLobbyService>();
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        // Test scenes may start hosting before this Start runs; catch up if so.
        if (NetworkManager.Singleton.IsServer)
        {
            OnServerStarted();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }
    }

    private void OnServerStopped(bool wasHost)
    {
        campaignSpawned = false;
        shiftObjects.Clear();
    }

    private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    private void OnServerStarted()
    {
        if (campaignSpawned)
        {
            return;
        }
        campaignSpawned = true;

        // The campaign lives for the whole hosted session (bank, quota, nights).
        GameObject campaign = Instantiate(campaignPrefab);
        campaign.GetComponent<NetworkObject>().Spawn(true);
        if (startingMoneyOverride >= 0)
        {
            campaign.GetComponent<CampaignManager>().ServerSetBank(startingMoneyOverride);
        }

        GameObject ghostWorld = Instantiate(ghostWorldPrefab);
        ghostWorld.GetComponent<NetworkObject>().Spawn(true);

        if (autoStartShift)
        {
            StartShift();
        }
    }

    // ---------- The loop ----------

    private void Update()
    {
        if (!IsServer || CampaignManager.Instance == null || CampaignManager.Instance.Phase != ShiftPhase.Shift)
        {
            return;
        }

        // The shift ends at dawn, when the whole crew is dead, or once every order is delivered or lost.
        if (FloodManager.Instance != null && FloodManager.Instance.IsDawn)
        {
            EndShift(true);
        }
        else if (WholeCrewDead())
        {
            EndShift(false, true);
        }
        else if (DeliveryManager.Instance != null && DeliveryManager.Instance.AllOrdersResolved)
        {
            EndShift(false);
        }
    }

    public void StartShift()
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (!IsServer || campaign == null || campaign.Phase != ShiftPhase.Depot)
        {
            return;
        }
        ShopWindow.Close();
        campaign.ServerSetPhase(ShiftPhase.Shift);

        // Late join only at the depot, never mid-shift (plan section 5).
        if (steam != null)
        {
            steam.SetJoinable(false);
        }

        NavMeshBaker.ServerBake(); // before anything movable is spawned

        GameObject deliveries = Instantiate(deliveryPrefab);
        ServerSpawnAndTrack(deliveries.GetComponent<NetworkObject>());
        DeliveryManager deliveryManager = deliveries.GetComponent<DeliveryManager>();

        GameObject flood = Instantiate(floodPrefab);
        ServerSpawnAndTrack(flood.GetComponent<NetworkObject>());

        foreach (PackageSpawnPoint point in FindObjectsByType<PackageSpawnPoint>(FindObjectsSortMode.None))
        {
            bool hasAddress = point.destination != "";
            bool cursed = point.alwaysCursed || (hasAddress && Random.value < cursedChance);
            Package package = ServerSpawnPackage(point.data, point.transform.position, point.destination, cursed);
            deliveryManager.ServerRegisterPackage(package);
        }

        GiveRoleItems();

        foreach (WorldItemSpawnPoint point in FindObjectsByType<WorldItemSpawnPoint>(FindObjectsSortMode.None))
        {
            SpawnWorldItem(point);
        }

        foreach (MonsterSpawnPoint point in FindObjectsByType<MonsterSpawnPoint>(FindObjectsSortMode.None))
        {
            SpawnMonster(point);
        }
    }

    private bool WholeCrewDead()
    {
        if (NetworkPlayer.All.Count == 0)
        {
            return false;
        }
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (!player.IsDead)
            {
                return false;
            }
        }
        return true;
    }

    // Each role starts the shift with its item (plan 4.7).
    private void GiveRoleItems()
    {
        ItemCatalog catalog = GameNetworkConfig.Instance.ItemCatalog;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            RoleData role = player.Role;
            if (role != null && role.startingItem != null)
            {
                player.Inventory.ServerTryAdd(catalog.IndexOf(role.startingItem));
            }
        }
    }

    private void EndShift(bool atDawn, bool crewWiped = false)
    {
        int earnings = DeliveryManager.Instance != null ? DeliveryManager.Instance.Money : 0;
        string reason = "Every order is done.";
        if (crewWiped)
        {
            earnings /= 2;
            reason = "The whole crew was lost. The company pays half.";
        }

        if (atDawn)
        {
            reason = "Dawn broke.";
            int lost = 0;
            foreach (NetworkPlayer player in NetworkPlayer.All)
            {
                if (!ExtractionZone.Contains(player.Position))
                {
                    // Not at the extraction: lost in the flood, along with everything they carried.
                    player.GetComponent<PlayerGrabber>().ServerDrop();
                    player.Inventory.ServerClearAll();
                    lost++;
                }
            }
            if (lost > 0)
            {
                reason += " " + lost + " courier(s) did not make it back and lost their gear.";
            }
        }

        CampaignManager.Instance.ServerEndNight(earnings, reason);
    }

    // Host presses RETURN TO DEPOT on the results screen.
    public void ReturnToDepot()
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (!IsServer || campaign == null || campaign.Phase != ShiftPhase.Results)
        {
            return;
        }

        foreach (NetworkObject spawned in shiftObjects)
        {
            if (spawned != null && spawned.IsSpawned)
            {
                spawned.Despawn(true);
            }
        }
        shiftObjects.Clear();

        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            player.GetComponent<PlayerGrabber>().ServerDrop();
            player.Inventory.ServerSetFlashlight(false);
            player.Inventory.ServerRemoveAll(ItemType.CourierBadge);
            player.Health.ServerRevive();
            player.ServerRespawn();
        }

        campaign.ServerSetPhase(ShiftPhase.Depot);
        if (steam != null)
        {
            steam.SetJoinable(true);
        }
    }

    // ---------- Spawning helpers (also used by items and rafts) ----------

    // Spawns a networked object and removes it again when the crew returns to the depot.
    public void ServerSpawnAndTrack(NetworkObject networkObject)
    {
        networkObject.Spawn(true);
        shiftObjects.Add(networkObject);
    }

    public Package ServerSpawnPackage(PackageData data, Vector3 position, string destination, bool cursed = false)
    {
        PackageCatalog catalog = GameNetworkConfig.Instance.PackageCatalog;
        GameObject instance = Instantiate(packagePrefab, position, Quaternion.identity);
        Package package = instance.GetComponent<Package>();
        package.ServerInit(catalog.IndexOf(data), destination, cursed);
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
        return package;
    }

    // A dead player's courier ID badge (plan 4.10).
    public void ServerDropBadge(Vector3 position, ulong ownerClientId)
    {
        ItemData badge = GameNetworkConfig.Instance.CourierBadge;
        int index = GameNetworkConfig.Instance.ItemCatalog.IndexOf(badge);
        if (index < 0)
        {
            return;
        }
        GameObject instance = Instantiate(GameNetworkConfig.Instance.ItemPrefabs.worldItem, position, Quaternion.identity);
        instance.GetComponent<WorldItem>().ServerInit(index, (int)ownerClientId);
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
    }

    // Used by cursed packages and broken customer rules to let something loose.
    public void ServerSpawnMonster(MonsterKind kind, Vector3 position)
    {
        GameObject prefab = kind == MonsterKind.Vetala ? vetalaPrefab : chudailPrefab;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
    }

    private void SpawnWorldItem(WorldItemSpawnPoint point)
    {
        int index = GameNetworkConfig.Instance.ItemCatalog.IndexOf(point.item);
        if (index < 0)
        {
            return;
        }
        GameObject instance = Instantiate(GameNetworkConfig.Instance.ItemPrefabs.worldItem, point.transform.position, Quaternion.identity);
        instance.GetComponent<WorldItem>().ServerInit(index);
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
    }

    private void SpawnMonster(MonsterSpawnPoint point)
    {
        GameObject prefab = jalPishachPrefab;
        if (point.kind == MonsterKind.Chudail)
        {
            prefab = chudailPrefab;
        }
        else if (point.kind == MonsterKind.Vetala)
        {
            prefab = vetalaPrefab;
        }

        GameObject instance = Instantiate(prefab, point.transform.position, point.transform.rotation);
        JalPishach waterMonster = instance.GetComponent<JalPishach>();
        if (waterMonster != null)
        {
            waterMonster.ServerSetHome(point.homeWater);
        }
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
    }
}
