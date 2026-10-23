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
    [SerializeField] private GameObject floodPrefab;
    [SerializeField] private GameObject deliveryPrefab;
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private GameObject jalPishachPrefab;
    [SerializeField] private GameObject chudailPrefab;
    [SerializeField] private GameObject vetalaPrefab;

    [Header("Shift")]
    [SerializeField, Tooltip("Start the shift as soon as the host starts (test scenes). Off = the host presses START SHIFT.")]
    private bool autoStartShift = false;

    private readonly List<NetworkObject> shiftObjects = new List<NetworkObject>();
    private SteamLobbyService steam;

    public bool ShiftStarted => CampaignManager.Instance != null && CampaignManager.Instance.Phase != ShiftPhase.Depot;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        steam = FindFirstObjectByType<SteamLobbyService>();
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    private void OnServerStarted()
    {
        // The campaign lives for the whole hosted session (bank, quota, nights).
        GameObject campaign = Instantiate(campaignPrefab);
        campaign.GetComponent<NetworkObject>().Spawn(true);

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

        // The shift ends at dawn, or once every order is delivered or lost.
        if (FloodManager.Instance != null && FloodManager.Instance.IsDawn)
        {
            EndShift(true);
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
            Package package = ServerSpawnPackage(point.data, point.transform.position, point.destination);
            deliveryManager.ServerRegisterPackage(package);
        }

        foreach (WorldItemSpawnPoint point in FindObjectsByType<WorldItemSpawnPoint>(FindObjectsSortMode.None))
        {
            SpawnWorldItem(point);
        }

        foreach (MonsterSpawnPoint point in FindObjectsByType<MonsterSpawnPoint>(FindObjectsSortMode.None))
        {
            SpawnMonster(point);
        }
    }

    private void EndShift(bool atDawn)
    {
        int earnings = DeliveryManager.Instance != null ? DeliveryManager.Instance.Money : 0;
        string reason = "Every order is done.";

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

    public Package ServerSpawnPackage(PackageData data, Vector3 position, string destination)
    {
        PackageCatalog catalog = GameNetworkConfig.Instance.PackageCatalog;
        GameObject instance = Instantiate(packagePrefab, position, Quaternion.identity);
        Package package = instance.GetComponent<Package>();
        package.ServerInit(catalog.IndexOf(data), destination);
        ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
        return package;
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
