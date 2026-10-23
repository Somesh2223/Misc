using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
#if FACEPUNCH_TRANSPORT
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
#endif

// Steam lobbies on top of the Facepunch transport (plan section 2 and 5).
// Everything Steam-specific lives in this one file. If the Facepunch transport package is not
// installed (or you are on a platform it does not support) the service reports "not available"
// and the game falls back to direct IP connections.
//
// Steam must be running. App ID 480 ("Spacewar") is for testing only - replace it with your own App ID.
public class SteamLobbyService : MonoBehaviour
{
    [SerializeField, Tooltip("Steam App ID. 480 is Valve's public test app.")]
    private uint steamAppId = 480;

    [SerializeField, Tooltip("Only lobbies with this tag show up in the browser.")]
    private string gameTag = "NightShiftCouriers";

    public string LastError { get; private set; } = "";

#if FACEPUNCH_TRANSPORT
    private Lobby currentLobby;
    private bool hasLobby;
    private bool joinCallbackHooked;

    public bool IsAvailable => true;
    public bool IsInLobby => hasLobby;
    public string PlayerName => SteamClient.IsValid ? SteamClient.Name : "";

    // ---------- Setup ----------

    private bool EnsureSteamStarted()
    {
        if (SteamClient.IsValid)
        {
            return true;
        }

        try
        {
            SteamClient.Init(steamAppId, false);
        }
        catch (Exception e)
        {
            LastError = "Steam is not running or could not start: " + e.Message;
            Debug.LogWarning(LastError);
            return false;
        }

        if (!joinCallbackHooked)
        {
            SteamFriends.OnGameLobbyJoinRequested += OnLobbyJoinRequested; // "Join game" from the Steam friends list
            joinCallbackHooked = true;
        }
        return true;
    }

    private FacepunchTransport PrepareTransport()
    {
        FacepunchTransport transport = NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        if (transport == null)
        {
            transport = NetworkManager.Singleton.gameObject.AddComponent<FacepunchTransport>();
        }
        NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
        return transport;
    }

    private void Update()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.RunCallbacks();
        }
    }

    // ---------- Host ----------

    public async Task<bool> HostAsync(string lobbyName, int maxPlayers)
    {
        if (!EnsureSteamStarted())
        {
            return false;
        }

        Lobby? created = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (!created.HasValue)
        {
            LastError = "Steam could not create a lobby.";
            return false;
        }

        currentLobby = created.Value;
        hasLobby = true;
        currentLobby.SetPublic();
        currentLobby.SetJoinable(true);
        currentLobby.SetData("game", gameTag);
        currentLobby.SetData("name", lobbyName);

        PrepareTransport();
        return NetworkManager.Singleton.StartHost();
    }

    // ---------- Join ----------

    public async Task<bool> JoinAsync(ulong lobbyId)
    {
        if (!EnsureSteamStarted())
        {
            return false;
        }

        Lobby? joined = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (!joined.HasValue)
        {
            LastError = "Could not join that lobby (full or closed).";
            return false;
        }

        currentLobby = joined.Value;
        hasLobby = true;

        FacepunchTransport transport = PrepareTransport();
        transport.targetSteamId = currentLobby.Owner.Id;
        return NetworkManager.Singleton.StartClient();
    }

    private async void OnLobbyJoinRequested(Lobby lobby, SteamId friend)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            await JoinAsync(lobby.Id.Value);
        }
    }

    // ---------- Browser ----------

    public async Task<List<LobbyInfo>> ListLobbiesAsync()
    {
        List<LobbyInfo> result = new List<LobbyInfo>();
        if (!EnsureSteamStarted())
        {
            return result;
        }

        Lobby[] found = await SteamMatchmaking.LobbyList
            .WithMaxResults(20)
            .WithKeyValue("game", gameTag)
            .RequestAsync();

        if (found == null)
        {
            return result;
        }

        foreach (Lobby lobby in found)
        {
            LobbyInfo info = new LobbyInfo();
            info.lobbyId = lobby.Id.Value;
            info.name = lobby.GetData("name");
            info.members = lobby.MemberCount;
            info.maxMembers = lobby.MaxMembers;
            result.Add(info);
        }
        return result;
    }

    // ---------- Leave ----------

    public void LeaveLobby()
    {
        if (hasLobby)
        {
            currentLobby.Leave();
            hasLobby = false;
        }
    }

    // Stop new players joining mid-shift (plan section 5: late join only at the depot).
    public void SetJoinable(bool joinable)
    {
        if (hasLobby)
        {
            currentLobby.SetJoinable(joinable);
        }
    }

    private void OnDestroy()
    {
        if (joinCallbackHooked)
        {
            SteamFriends.OnGameLobbyJoinRequested -= OnLobbyJoinRequested;
        }
        LeaveLobby();
    }

#else
    // Facepunch transport not installed: stubs so the rest of the game still compiles.
    public bool IsAvailable => false;
    public bool IsInLobby => false;
    public string PlayerName => "";

    public Task<bool> HostAsync(string lobbyName, int maxPlayers)
    {
        LastError = "Steam support is not installed (Facepunch transport package missing).";
        return Task.FromResult(false);
    }

    public Task<bool> JoinAsync(ulong lobbyId)
    {
        LastError = "Steam support is not installed (Facepunch transport package missing).";
        return Task.FromResult(false);
    }

    public Task<List<LobbyInfo>> ListLobbiesAsync()
    {
        return Task.FromResult(new List<LobbyInfo>());
    }

    public void LeaveLobby() { }
    public void SetJoinable(bool joinable) { }
#endif
}
