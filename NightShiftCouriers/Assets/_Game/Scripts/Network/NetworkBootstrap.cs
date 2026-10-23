using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Starts, joins and leaves games, with a simple on-screen menu (real UI comes later).
//   Direct connection: Host / Join by IP (works on a LAN, or over the internet with a forwarded port).
//   Steam: Host a lobby / browse lobbies (needs Steam running; see SteamLobbyService).
// Test scenes tick "Auto Start Host" to play alone with no menu.
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField, Tooltip("Start hosting immediately and skip the menu (single-player test scenes).")]
    private bool autoStartHost = false;
    [SerializeField] private string defaultAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private NetworkManager network;
    private UnityTransport directTransport;
    private SteamLobbyService steam;
    private GameNetworkConfig config;

    private string addressInput;
    private string status = "";
    private List<LobbyInfo> lobbies = new List<LobbyInfo>();
    private bool busy;

    private void Start()
    {
        network = NetworkManager.Singleton;
        directTransport = GetComponent<UnityTransport>();
        steam = GetComponent<SteamLobbyService>();
        config = GetComponent<GameNetworkConfig>();
        addressInput = defaultAddress;

        network.ConnectionApprovalCallback = ApproveConnection;
        network.OnClientDisconnectCallback += OnClientDisconnected;

        if (autoStartHost)
        {
            StartDirectHost();
        }
    }

    private void OnDestroy()
    {
        if (network != null)
        {
            network.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ---------- Connection rules (server) ----------

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool full = network.ConnectedClientsIds.Count >= config.MaxPlayers;
        bool shiftRunning = ShiftSpawner.Instance != null && ShiftSpawner.Instance.ShiftStarted;

        response.CreatePlayerObject = true;
        response.Pending = false;
        response.Approved = !full && !shiftRunning;
        if (!response.Approved)
        {
            response.Reason = full ? "The crew is full." : "The shift has already started.";
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // On a client, this fires when we lose the host or are refused.
        if (!network.IsServer && clientId == network.LocalClientId)
        {
            string reason = network.DisconnectReason;
            status = string.IsNullOrEmpty(reason) ? "Disconnected." : "Disconnected: " + reason;
            busy = false;
        }
    }

    // ---------- Actions ----------

    private void StartDirectHost()
    {
        network.NetworkConfig.NetworkTransport = directTransport;
        directTransport.SetConnectionData("127.0.0.1", port, "0.0.0.0"); // listen on all network cards
        if (!network.StartHost())
        {
            status = "Could not start the host.";
        }
    }

    private void StartDirectClient()
    {
        network.NetworkConfig.NetworkTransport = directTransport;
        directTransport.SetConnectionData(addressInput, port);
        status = "Connecting to " + addressInput + "...";
        if (!network.StartClient())
        {
            status = "Could not start the client.";
        }
    }

    private async void StartSteamHost()
    {
        busy = true;
        status = "Creating Steam lobby...";
        string lobbyName = steam.PlayerName + "'s crew";
        bool ok = await steam.HostAsync(lobbyName, config.MaxPlayers);
        status = ok ? "" : steam.LastError;
        busy = false;
    }

    private async void RefreshSteamLobbies()
    {
        busy = true;
        status = "Searching for lobbies...";
        lobbies = await steam.ListLobbiesAsync();
        status = lobbies.Count == 0 ? "No lobbies found. " + steam.LastError : "";
        busy = false;
    }

    private async void JoinSteamLobby(ulong lobbyId)
    {
        busy = true;
        status = "Joining lobby...";
        bool ok = await steam.JoinAsync(lobbyId);
        status = ok ? "Connecting..." : steam.LastError;
        busy = false;
    }

    private void Disconnect()
    {
        steam.LeaveLobby();
        network.Shutdown();
        status = "";
    }

    // ---------- On-screen menu ----------

    private void OnGUI()
    {
        if (network == null)
        {
            return;
        }

        if (network.IsListening)
        {
            DrawConnectedPanel();
        }
        else if (!autoStartHost)
        {
            DrawMainMenu();
        }
    }

    private void DrawMainMenu()
    {
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 200, 60, 400, Screen.height - 100), GUI.skin.box);
        GUILayout.Label("NIGHT SHIFT COURIERS");

        GUILayout.Space(8);
        GUILayout.Label("Direct connection");
        if (GUILayout.Button("Host game")) StartDirectHost();
        GUILayout.BeginHorizontal();
        addressInput = GUILayout.TextField(addressInput, GUILayout.Width(220));
        if (GUILayout.Button("Join by IP")) StartDirectClient();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Steam");
        if (steam != null && steam.IsAvailable)
        {
            GUI.enabled = !busy;
            if (GUILayout.Button("Host Steam lobby")) StartSteamHost();
            if (GUILayout.Button("Find Steam lobbies")) RefreshSteamLobbies();
            foreach (LobbyInfo lobby in lobbies)
            {
                if (GUILayout.Button(lobby.name + "  (" + lobby.members + "/" + lobby.maxMembers + ")"))
                {
                    JoinSteamLobby(lobby.lobbyId);
                }
            }
            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label("Steam support is not installed on this build.");
        }

        if (status != "")
        {
            GUILayout.Space(8);
            GUILayout.Label(status);
        }
        GUILayout.EndArea();
    }

    private void DrawConnectedPanel()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 260, 90, 240, 200));

        string role = network.IsHost ? "Hosting" : "Connected";
        GUILayout.Label(role + " - crew: " + network.ConnectedClientsIds.Count + "/" + config.MaxPlayers);

        bool shiftStarted = FloodManager.Instance != null; // the flood manager exists only while a shift runs
        if (network.IsServer && !shiftStarted)
        {
            if (GUILayout.Button("START SHIFT")) ShiftSpawner.Instance.StartShift();
            GUILayout.Label("Wait for your crew to join first.");
        }
        else if (!network.IsServer && !shiftStarted)
        {
            GUILayout.Label("Waiting for the host to start the shift...");
        }

        MicNoiseReporter mic = NetworkPlayer.Local != null ? NetworkPlayer.Local.GetComponent<MicNoiseReporter>() : null;
        if (mic != null && GUILayout.Button("Mic noise: " + (mic.UseMicrophone ? "ON" : "OFF")))
        {
            mic.UseMicrophone = !mic.UseMicrophone;
        }

        if (GUILayout.Button("Disconnect")) Disconnect();
        GUILayout.EndArea();
    }
}
