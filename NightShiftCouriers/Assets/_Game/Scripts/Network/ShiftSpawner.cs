using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Starts and restarts the shift on the SERVER. Everything that matters (flood, orders, packages, monsters)
// is spawned here at run time, so the scene itself holds no networked objects.
// Markers in the scene (PackageSpawnPoint, MonsterSpawnPoint, PlayerSpawnPoint) say where things go.
public class ShiftSpawner : MonoBehaviour
{
    public static ShiftSpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject floodPrefab;
    [SerializeField] private GameObject deliveryPrefab;
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private GameObject jalPishachPrefab;

    [Header("Shift")]
    [SerializeField, Tooltip("Money needed to meet the quota.")]
    private int quota = 400;
    [SerializeField, Tooltip("Start the shift as soon as the host starts. Off = the host presses 'Start Shift' once everyone has joined.")]
    private bool autoStartShift = false;

    private readonly List<NetworkObject> shiftObjects = new List<NetworkObject>();
    private SteamLobbyService steam;

    public bool ShiftStarted { get; private set; }

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

    private void OnServerStarted()
    {
        ShiftStarted = false;
        if (autoStartShift)
        {
            StartShift();
        }
    }

    // ---------- Start / restart ----------

    public void StartShift()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsServer || ShiftStarted)
        {
            return;
        }
        ShiftStarted = true;

        // Late join only at the depot, never mid-shift (plan section 5).
        if (steam != null)
        {
            steam.SetJoinable(false);
        }

        DeliveryManager deliveries = SpawnDeliveryManager();
        Spawn(floodPrefab, Vector3.zero);
        SpawnPackages(deliveries);
        SpawnMonsters();
    }

    public void RestartShift()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsServer)
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
            player.Health.ServerRevive();
            player.ServerRespawn();
        }

        ShiftStarted = false;
        StartShift();
    }

    // ---------- Spawning ----------

    private DeliveryManager SpawnDeliveryManager()
    {
        GameObject instance = Instantiate(deliveryPrefab);
        DeliveryManager manager = instance.GetComponent<DeliveryManager>();
        manager.ServerInit(quota);
        Register(instance.GetComponent<NetworkObject>());
        return manager;
    }

    private void SpawnPackages(DeliveryManager deliveries)
    {
        PackageCatalog catalog = GameNetworkConfig.Instance.PackageCatalog;

        foreach (PackageSpawnPoint point in FindObjectsByType<PackageSpawnPoint>(FindObjectsSortMode.None))
        {
            GameObject instance = Instantiate(packagePrefab, point.transform.position, point.transform.rotation);
            Package package = instance.GetComponent<Package>();
            package.ServerInit(catalog.IndexOf(point.data), point.destination);
            Register(instance.GetComponent<NetworkObject>());
            deliveries.ServerRegisterPackage(package);
        }
    }

    private void SpawnMonsters()
    {
        foreach (MonsterSpawnPoint point in FindObjectsByType<MonsterSpawnPoint>(FindObjectsSortMode.None))
        {
            GameObject instance = Instantiate(jalPishachPrefab, point.transform.position, point.transform.rotation);
            instance.GetComponent<JalPishach>().ServerSetHome(point.homeWater);
            Register(instance.GetComponent<NetworkObject>());
        }
    }

    private void Spawn(GameObject prefab, Vector3 position)
    {
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        Register(instance.GetComponent<NetworkObject>());
    }

    private void Register(NetworkObject networkObject)
    {
        networkObject.Spawn(true);
        shiftObjects.Add(networkObject);
    }
}
