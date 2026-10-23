using Unity.Netcode;
using UnityEngine;

// A crew-built raft (plan section 4.4). SERVER simulated: it floats on the flood water,
// is paddled by riders (right mouse with a plank, or by hand), takes hits, and breaks into parts.
// Carries 2 players; a third tips it over.
[RequireComponent(typeof(MovingPlatform))]
public class Raft : NetworkBehaviour
{
    public static Raft Current { get; private set; }

    [Header("Durability")]
    [SerializeField] private int maxHitPoints = 3;
    [SerializeField, Tooltip("Hitting something faster than this (m/s) damages the raft.")]
    private float damageSpeed = 1.5f;

    [Header("Riders")]
    [SerializeField] private int maxRiders = 2;
    [SerializeField, Tooltip("Half the deck size (x = width, y = length) in metres.")]
    private Vector2 deckHalfSize = new Vector2(1.2f, 0.9f);

    [Header("Paddling")]
    [SerializeField, Tooltip("Speed added per paddle stroke (m/s).")]
    private float paddleImpulse = 1.2f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField, Tooltip("How quickly the raft slows down in the water.")]
    private float waterDrag = 0.6f;
    [SerializeField, Tooltip("Noise of each paddle stroke (water noise: the Jal-Pishach hears it).")]
    private float paddleLoudness = 2.5f;

    [Header("Floating")]
    [SerializeField, Tooltip("Water shallower than this (metres) grounds the raft.")]
    private float minWaterDepth = 0.4f;

    private readonly NetworkVariable<int> hitPoints = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 velocity;
    private bool broken;

    public int HitPoints => hitPoints.Value;
    public int MaxHitPoints => maxHitPoints;

    public override void OnNetworkSpawn()
    {
        Current = this;
        if (IsServer)
        {
            hitPoints.Value = maxHitPoints;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    // ---------- Server simulation ----------

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned || broken)
        {
            return;
        }

        Float();
        Drift();
        CheckRiders();
    }

    private void Float()
    {
        float floorY = transform.position.y - 20f;
        if (Physics.Raycast(transform.position - Vector3.up * 0.3f, Vector3.down, out RaycastHit floor, 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            floorY = floor.point.y;
        }

        float depth = WaterVolume.GetDepthAt(transform.position, floorY);
        Vector3 position = transform.position;
        if (depth < minWaterDepth)
        {
            // Beached: sits on the ground and cannot be paddled.
            position.y = floorY;
            velocity = Vector3.zero;
        }
        else
        {
            position.y = Mathf.Lerp(position.y, floorY + depth, 5f * Time.fixedDeltaTime);
        }
        transform.position = position;
    }

    private void Drift()
    {
        Vector3 step = velocity * Time.fixedDeltaTime;
        if (step.sqrMagnitude > 0f)
        {
            Vector3 halfExtents = new Vector3(deckHalfSize.x, 0.1f, deckHalfSize.y);
            int mask = ~(1 << 2); // ignore players and trigger zones (Ignore Raycast layer)
            bool blocked = Physics.BoxCast(transform.position + Vector3.up * 0.2f, halfExtents, step.normalized,
                out RaycastHit hit, transform.rotation, step.magnitude + 0.05f, mask, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform);

            if (blocked)
            {
                if (velocity.magnitude > damageSpeed)
                {
                    ServerDamage(1);
                }
                velocity = -velocity * 0.3f; // bounce off
            }
            else
            {
                transform.position += step;
            }
        }

        velocity *= Mathf.Clamp01(1f - waterDrag * Time.fixedDeltaTime);
    }

    private void CheckRiders()
    {
        int riders = 0;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead && IsOnDeck(player.Position))
            {
                riders++;
            }
        }

        if (riders > maxRiders)
        {
            ServerBreak("Too many people! The raft tipped over.");
        }
    }

    private bool IsOnDeck(Vector3 position)
    {
        Vector3 local = transform.InverseTransformPoint(position);
        return Mathf.Abs(local.x) <= deckHalfSize.x && Mathf.Abs(local.z) <= deckHalfSize.y
               && local.y > -0.3f && local.y < 1.2f;
    }

    // ---------- Server API ----------

    public void ServerPaddle(NetworkPlayer paddler, Vector3 direction, float strength)
    {
        if (!IsOnDeck(paddler.Position))
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        velocity += direction.normalized * paddleImpulse * strength;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        GameEvents.RaiseNoise(transform.position, paddleLoudness * strength, true);
        SplashClientRpc();
    }

    public void ServerDamage(int amount)
    {
        if (broken)
        {
            return;
        }
        hitPoints.Value -= amount;
        CampaignManager.Instance.ServerMessage("The raft is damaged! (" + Mathf.Max(0, hitPoints.Value) + "/" + maxHitPoints + ")");
        if (hitPoints.Value <= 0)
        {
            ServerBreak("The raft broke apart!");
        }
    }

    // Returns false if there was nothing to repair.
    public bool ServerRepair(int amount)
    {
        if (broken || hitPoints.Value >= maxHitPoints)
        {
            return false;
        }
        hitPoints.Value = Mathf.Min(maxHitPoints, hitPoints.Value + amount);
        CampaignManager.Instance.ServerMessage("Raft patched (" + hitPoints.Value + "/" + maxHitPoints + ")");
        return true;
    }

    // The raft falls apart: its three parts float around in the water and can be fished out and rebuilt.
    public void ServerBreak(string reason)
    {
        if (broken)
        {
            return;
        }
        broken = true;

        GameNetworkConfig config = GameNetworkConfig.Instance;
        Vector3 centre = transform.position + Vector3.up * 0.5f;
        ShiftSpawner.Instance.ServerSpawnPackage(config.RaftPartFloaters, centre + new Vector3(1.2f, 0f, 0f), "");
        ShiftSpawner.Instance.ServerSpawnPackage(config.RaftPartPlatform, centre + new Vector3(-1.2f, 0f, 0.5f), "");
        ShiftSpawner.Instance.ServerSpawnPackage(config.RaftPartLashing, centre + new Vector3(0f, 0f, -1.2f), "");

        CampaignManager.Instance.ServerMessage(reason);
        NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void SplashClientRpc()
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Splash, transform.position, 0.8f, 25f);
    }
}
