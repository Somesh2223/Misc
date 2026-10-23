using Unity.Netcode;
using UnityEngine;

// R.E.P.O.-style "hold beam", server-authoritative. The owner asks (grab / drop / throw),
// the server decides and moves the package. E grabs and drops, left mouse throws.
// Heavy packages need two players: each grabs it, then it lifts (see Package).
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(NetworkPlayer))]
public class PlayerGrabber : NetworkBehaviour
{
    private const ulong NoPackage = ulong.MaxValue;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Grabbing")]
    [SerializeField, Tooltip("How far away you can grab a package (metres).")]
    private float grabRange = 3f;
    [SerializeField, Tooltip("Where the package floats in front of the camera (metres).")]
    private float holdDistance = 1.8f;

    [Header("Throwing")]
    [SerializeField, Tooltip("Throw speed for a 1 kg package (m/s). Heavier packages go slower.")]
    private float throwSpeed = 12f;

    [Header("Weight")]
    [SerializeField, Tooltip("At this carried mass (kg) you move at the slowest speed.")]
    private float heaviestMass = 15f;
    [SerializeField, Range(0.2f, 1f), Tooltip("Speed multiplier when carrying the heaviest load.")]
    private float slowestSpeedMultiplier = 0.55f;

    // Written by the server so every client (and the owner's HUD / speed) knows what is held.
    private readonly NetworkVariable<ulong> heldId = new NetworkVariable<ulong>(
        NoPackage, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> carriedLoad = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerInputReader input;
    private NetworkPlayer networkPlayer;
    private Package held; // server only

    public bool IsHolding => heldId.Value != NoPackage;
    // The package this player holds, on the server (null elsewhere or when empty-handed).
    public Package ServerHeldPackage => held;

    public Package HeldPackage
    {
        get
        {
            if (heldId.Value == NoPackage || NetworkManager == null)
            {
                return null;
            }
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(heldId.Value, out NetworkObject spawned))
            {
                return spawned.GetComponent<Package>();
            }
            return null;
        }
    }

    // Where the held package is pulled to (server uses the owner's published aim).
    public Vector3 HoldPoint => networkPlayer.AimOrigin + networkPlayer.AimDirection * holdDistance;

    // 1.0 when empty-handed; lower when carrying something heavy.
    public float SpeedMultiplier
    {
        get
        {
            if (!IsHolding)
            {
                return 1f;
            }
            float t = Mathf.Clamp01(carriedLoad.Value / heaviestMass);
            return Mathf.Lerp(1f, slowestSpeedMultiplier, t);
        }
    }

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            ServerDrop();
        }
    }

    // ---------- Owner: input -> requests ----------

    private void Update()
    {
        if (!IsSpawned || !IsOwner || networkPlayer.IsDead)
        {
            return;
        }

        if (input.InteractPressedThisFrame)
        {
            if (IsHolding)
            {
                DropServerRpc();
            }
            else if (!TryLocalInteract())
            {
                GrabServerRpc(cameraTransform.position, cameraTransform.forward);
            }
        }

        if (IsHolding && input.ThrowPressedThisFrame)
        {
            ThrowServerRpc(cameraTransform.forward);
        }
    }

    // Things like the shop terminal only open a menu on this machine.
    private bool TryLocalInteract()
    {
        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, grabRange, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        ILocalInteractable local = hit.collider.GetComponentInParent<ILocalInteractable>();
        if (local == null)
        {
            return false;
        }
        local.LocalInteract();
        return true;
    }

    // ---------- Server: decisions ----------

    [ServerRpc]
    private void GrabServerRpc(Vector3 origin, Vector3 direction)
    {
        if (held != null || networkPlayer.IsDead)
        {
            return;
        }

        // The ray must start near the player, so nobody can grab things from across the map.
        if ((origin - networkPlayer.Position).sqrMagnitude > 9f)
        {
            return;
        }

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, grabRange, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Package package = hit.collider.GetComponentInParent<Package>();
        if (package == null)
        {
            // Not a package: maybe an item to pick up, a raft spot, or a climbing teammate.
            IServerInteractable interactable = hit.collider.GetComponentInParent<IServerInteractable>();
            if (interactable != null && !ReferenceEquals(interactable, networkPlayer))
            {
                interactable.ServerInteract(networkPlayer);
            }
            return;
        }
        if (!package.IsSpawned)
        {
            return;
        }

        if (package.ServerTryAddHolder(this))
        {
            held = package;
            heldId.Value = package.NetworkObjectId;
        }
    }

    [ServerRpc]
    private void DropServerRpc()
    {
        ServerDrop();
    }

    [ServerRpc]
    private void ThrowServerRpc(Vector3 direction)
    {
        if (held == null || !held.CanBeThrown)
        {
            return;
        }

        Package thrown = held;
        ServerDrop();

        float speed = throwSpeed / Mathf.Max(1f, thrown.Body.mass);
        thrown.Body.linearVelocity = direction.normalized * speed;
        GameEvents.RaiseNoise(networkPlayer.Position, 3f, false);
    }

    public void ServerDrop()
    {
        if (!IsServer || held == null)
        {
            return;
        }

        Package dropped = held;
        held = null;
        heldId.Value = NoPackage;
        carriedLoad.Value = 0f;
        dropped.ServerRemoveHolder(this);
    }

    // Called by the Package when it was released from outside (delivered, despawned).
    public void ServerForget(Package package)
    {
        if (held == package)
        {
            held = null;
            heldId.Value = NoPackage;
            carriedLoad.Value = 0f;
        }
    }

    // Called by the Package when the weight each holder carries changes.
    public void ServerSetLoad(float load)
    {
        carriedLoad.Value = load;
    }
}
