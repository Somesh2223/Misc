using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Sits next to the NetworkManager and sets it up in code: transport, player prefab, spawnable prefabs.
// Doing it here (instead of in the Inspector) means the host and every client always register
// exactly the same prefabs.
[RequireComponent(typeof(NetworkManager))]
public class GameNetworkConfig : MonoBehaviour
{
    // Prefabs that items and raft spots spawn while playing.
    [Serializable]
    public class ItemPrefabSet
    {
        public GameObject rope;
        public GameObject plank;
        public GameObject decoy;
        public GameObject saltWard;
        public GameObject glowStick;
        public GameObject worldItem;
        public GameObject raft;
    }

    public static GameNetworkConfig Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    [SerializeField, Tooltip("Every other prefab the server spawns (packages, monsters, managers).")]
    private GameObject[] networkPrefabs;
    [SerializeField] private PackageCatalog packageCatalog;
    [SerializeField, Tooltip("How this scene's flood behaves (stage heights and timings).")]
    private FloodSettings floodSettings;
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private ItemPrefabSet itemPrefabs = new ItemPrefabSet();

    [Header("Raft parts (plan 4.4)")]
    [SerializeField] private PackageData raftPartFloaters;
    [SerializeField] private PackageData raftPartPlatform;
    [SerializeField] private PackageData raftPartLashing;

    [SerializeField, Tooltip("Crew size limit (plan: 2-4 players).")]
    private int maxPlayers = 4;

    public PackageCatalog PackageCatalog => packageCatalog;
    public FloodSettings FloodSettings => floodSettings;
    public int MaxPlayers => maxPlayers;
    public ItemCatalog ItemCatalog => itemCatalog;
    public ItemPrefabSet ItemPrefabs => itemPrefabs;
    public PackageData RaftPartFloaters => raftPartFloaters;
    public PackageData RaftPartPlatform => raftPartPlatform;
    public PackageData RaftPartLashing => raftPartLashing;

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true; // so a second test window keeps running when unfocused

        NetworkManager manager = GetComponent<NetworkManager>();
        if (manager.NetworkConfig == null)
        {
            manager.NetworkConfig = new NetworkConfig();
        }
        if (manager.NetworkConfig.NetworkTransport == null)
        {
            manager.NetworkConfig.NetworkTransport = GetComponent<UnityTransport>();
        }

        // All objects are spawned by the server at run time, so no scene synchronisation is needed.
        manager.NetworkConfig.EnableSceneManagement = false;
        manager.NetworkConfig.ConnectionApproval = true;
        manager.NetworkConfig.PlayerPrefab = playerPrefab;

        foreach (GameObject prefab in networkPrefabs)
        {
            try
            {
                if (!manager.NetworkConfig.Prefabs.Contains(prefab))
                {
                    manager.AddNetworkPrefab(prefab);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not register " + prefab.name + " as a network prefab: " + e.Message);
            }
        }
    }
}
