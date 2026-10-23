using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// The networked side of a player (plan sections 4.1 and 5).
// - The OWNER moves their own player (owner-authoritative) and publishes position, look and state.
// - Everyone else sees a smoothed copy.
// - The SERVER checks the owner is not moving impossibly fast and snaps them back if they are.
// - Monsters and other server code read player state from here, never from the owner's controller.
public class NetworkPlayer : NetworkBehaviour
{
    private const byte FlagSwimming = 1;
    private const byte FlagCrouching = 2;
    private const byte FlagSprinting = 4;

    private static readonly List<NetworkPlayer> allPlayers = new List<NetworkPlayer>();
    private static string serverVoiceChannel; // one voice channel per hosted session (server only)

    public static IReadOnlyList<NetworkPlayer> All => allPlayers;
    public static NetworkPlayer Local { get; private set; }

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Tooltip("The visible capsule other players see.")]
    private GameObject bodyVisual;

    [Header("Smoothing (other players)")]
    [SerializeField, Tooltip("Higher = other players follow the real position more tightly.")]
    private float remoteSmoothing = 15f;

    [Header("Server checks")]
    [SerializeField, Tooltip("Fastest believable horizontal speed (m/s). Sprint is 7; this leaves room for shoves and lag.")]
    private float maxHorizontalSpeed = 14f;
    [SerializeField, Tooltip("Seconds between server speed checks.")]
    private float checkInterval = 0.5f;

    // Written by the owner, read by everyone.
    private readonly NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> netYaw = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> netWaterDepth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<byte> netFlags = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Written by the server: the Vivox channel name everyone in this crew joins.
    private readonly NetworkVariable<FixedString64Bytes> voiceChannel = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> netAimOrigin = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Vector3> netAimDirection = new NetworkVariable<Vector3>(
        Vector3.forward, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private PlayerController controller;
    private PlayerHealth health;
    private CharacterController characterController;

    // Server-side speed check
    private Vector3 lastCheckedPosition;
    private float checkTimer;
    private bool checkStarted;

    public PlayerHealth Health => health;
    public Vector3 Position => IsOwner ? transform.position : netPosition.Value;
    public float WaterDepth => netWaterDepth.Value;
    public bool IsSwimming => (netFlags.Value & FlagSwimming) != 0;
    public bool IsCrouching => (netFlags.Value & FlagCrouching) != 0;
    public bool IsSprinting => (netFlags.Value & FlagSprinting) != 0;
    public bool IsDead => health != null && health.IsDead;
    public string VoiceChannel => voiceChannel.Value.ToString();
    public Vector3 AimOrigin => netAimOrigin.Value;
    public Vector3 AimDirection => netAimDirection.Value;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        allPlayers.Add(this);

        if (IsOwner)
        {
            SetUpLocalPlayer();
        }
        else
        {
            SetUpRemotePlayer();
        }

        if (IsServer)
        {
            lastCheckedPosition = transform.position;
            if (string.IsNullOrEmpty(serverVoiceChannel))
            {
                serverVoiceChannel = "nsc" + Guid.NewGuid().ToString("N");
            }
            voiceChannel.Value = new FixedString64Bytes(serverVoiceChannel);
        }
    }

    public override void OnNetworkDespawn()
    {
        allPlayers.Remove(this);
        if (IsServer && allPlayers.Count == 0)
        {
            serverVoiceChannel = null; // next hosted session gets a fresh channel
        }
        if (Local == this)
        {
            Local = null;
        }
    }

    // ---------- Setup ----------

    private void SetUpLocalPlayer()
    {
        Local = this;

        // Only the owner runs the movement and input scripts.
        GetComponent<PlayerInputReader>().enabled = true;
        controller.enabled = true;
        foreach (Behaviour behaviour in GetComponents<Behaviour>())
        {
            if (behaviour is PlayerStamina || behaviour is PlayerNoise || behaviour is MicNoiseReporter || behaviour is PlayerVoice)
            {
                behaviour.enabled = true;
            }
        }

        if (bodyVisual != null)
        {
            bodyVisual.SetActive(false); // you do not see your own capsule
        }

        // Use this player's camera instead of any menu camera in the scene.
        foreach (Camera other in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!other.transform.IsChildOf(transform))
            {
                other.enabled = false;
            }
        }
        foreach (AudioListener other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (!other.transform.IsChildOf(transform))
            {
                other.enabled = false;
            }
        }

        MoveToSpawnPoint();
    }

    private void SetUpRemotePlayer()
    {
        // Remote players are just a smoothed copy. No input, no camera, no collider of their own.
        characterController.enabled = false;
        transform.position = netPosition.Value; // appear where the player is, not at the origin
        transform.rotation = Quaternion.Euler(0f, netYaw.Value, 0f);
        if (cameraTransform != null)
        {
            cameraTransform.gameObject.SetActive(false);
        }
    }

    private void MoveToSpawnPoint()
    {
        int index = (int)OwnerClientId;
        if (PlayerSpawnPoint.TryGet(index, out Vector3 position, out float yaw))
        {
            controller.Teleport(position, yaw);
            netPosition.Value = position;
        }
    }

    // ---------- Per-frame sync ----------

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsOwner)
        {
            PublishLocalState();
        }
        else
        {
            FollowOwner();
        }

        if (IsServer && !IsOwner)
        {
            CheckRemoteSpeed();
        }
    }

    private void PublishLocalState()
    {
        netPosition.Value = transform.position;
        netYaw.Value = transform.eulerAngles.y;
        netWaterDepth.Value = controller.WaterDepth;

        byte flags = 0;
        if (controller.IsSwimming) flags |= FlagSwimming;
        if (controller.IsCrouching) flags |= FlagCrouching;
        if (controller.IsSprinting) flags |= FlagSprinting;
        netFlags.Value = flags;

        netAimOrigin.Value = cameraTransform.position;
        netAimDirection.Value = cameraTransform.forward;
    }

    private void FollowOwner()
    {
        float blend = 1f - Mathf.Exp(-remoteSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, netPosition.Value, blend);
        Quaternion wanted = Quaternion.Euler(0f, netYaw.Value, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted, blend);
    }

    // ---------- Server validation ----------

    private void CheckRemoteSpeed()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer < checkInterval)
        {
            return;
        }

        Vector3 now = netPosition.Value;
        if (!checkStarted)
        {
            // The first reading is just the player appearing at their spawn point.
            checkStarted = true;
            lastCheckedPosition = now;
            checkTimer = 0f;
            return;
        }

        Vector3 moved = now - lastCheckedPosition;
        moved.y = 0f;
        float allowed = maxHorizontalSpeed * checkTimer + 1f; // +1 m of slack for lag

        if (float.IsNaN(now.x) || moved.magnitude > allowed)
        {
            Debug.LogWarning("Player " + OwnerClientId + " moved " + moved.magnitude.ToString("0.0") + " m in "
                             + checkTimer.ToString("0.00") + " s. Snapping back.");
            TeleportOwnerClientRpc(lastCheckedPosition, netYaw.Value, OwnerOnly());
        }
        else
        {
            lastCheckedPosition = now;
        }
        checkTimer = 0f;
    }

    // ---------- Server -> owner commands ----------

    public void ServerRespawn()
    {
        int index = (int)OwnerClientId;
        if (PlayerSpawnPoint.TryGet(index, out Vector3 position, out float yaw))
        {
            lastCheckedPosition = position;
            TeleportOwnerClientRpc(position, yaw, OwnerOnly());
        }
    }

    public void ServerAddImpulse(Vector3 velocity)
    {
        ImpulseClientRpc(velocity, OwnerOnly());
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 position, float yaw, ClientRpcParams rpcParams = default)
    {
        controller.Teleport(position, yaw);
        netPosition.Value = position;
    }

    [ClientRpc]
    private void ImpulseClientRpc(Vector3 velocity, ClientRpcParams rpcParams = default)
    {
        controller.AddImpulse(velocity);
    }

    private ClientRpcParams OwnerOnly()
    {
        ClientRpcParams rpcParams = default;
        rpcParams.Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } };
        return rpcParams;
    }

    // ---------- Owner -> server reports ----------

    // Called by the owner's PlayerNoise. The server uses its own idea of where the player is,
    // so a cheater cannot fake noise from somewhere else.
    public void ReportNoise(float loudness, bool fromWater)
    {
        if (IsServer)
        {
            GameEvents.RaiseNoise(Position, Mathf.Clamp(loudness, 0f, 10f), fromWater);
        }
        else
        {
            ReportNoiseServerRpc(loudness, fromWater);
        }
    }

    [ServerRpc]
    private void ReportNoiseServerRpc(float loudness, bool fromWater)
    {
        GameEvents.RaiseNoise(Position, Mathf.Clamp(loudness, 0f, 10f), fromWater);
    }
}
