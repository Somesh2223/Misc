using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// A physics package. The SERVER simulates it; clients only show the synced position (NetworkTransform).
// Tracks its condition (0 = ruined, 1 = perfect) from impacts and water, and who is holding it.
[RequireComponent(typeof(Rigidbody))]
public class Package : NetworkBehaviour
{
    [Header("Buoyancy")]
    [SerializeField, Tooltip("Packages lighter than this (kg) float; heavier ones sink.")]
    private float floatMassLimit = 4f;
    [SerializeField, Tooltip("Upward push (m/s^2) while under water, for floating packages.")]
    private float buoyancyAcceleration = 14f;
    [SerializeField, Tooltip("Water drag so packages do not rocket around underwater.")]
    private float waterDrag = 3f;

    [Header("Holding")]
    [SerializeField, Tooltip("How hard the package is pulled to the hold point. Higher = stiffer.")]
    private float holdStrength = 12f;
    [SerializeField, Tooltip("Max speed the package may move at while held (m/s).")]
    private float maxHoldSpeed = 15f;
    [SerializeField, Tooltip("A holder whose hold point gets this far from the package (stuck on a wall) lets go.")]
    private float breakDistance = 2.5f;

    // What the server decides and every client displays.
    private readonly NetworkVariable<int> dataIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> destination = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> wetness = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> delivered = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody body;
    private Renderer cachedRenderer;
    private PackageData data;
    private Color baseColor = Color.white;

    // Server only
    private int pendingDataIndex;
    private string pendingDestination = "";
    private float wetSeconds;
    private float spawnTime;
    private readonly List<PlayerGrabber> holders = new List<PlayerGrabber>();
    private readonly List<PlayerGrabber> tooFar = new List<PlayerGrabber>();

    public PackageData Data => data;
    public Rigidbody Body => body;
    public string DestinationAddress => destination.Value.ToString();
    public bool IsDelivered => delivered.Value;
    public bool IsHeld => holders.Count > 0;
    public int HolderCount => holders.Count;
    public bool CanBeThrown => data != null && data.holdersRequired == 1 && holders.Count == 1;
    public float AgeSeconds => Time.time - spawnTime;

    // True once enough players hold it (1 for normal packages, 2 for heavy ones).
    private bool IsLifted => data != null && holders.Count >= data.holdersRequired;

    // 1 = perfect, 0 = ruined. Multiplied into the payout.
    public float Condition
    {
        get
        {
            if (data == null)
            {
                return 1f;
            }

            float result = 1f;
            if (data.isFragile)
            {
                result *= Mathf.Clamp01(health.Value / data.maxHealth);
            }
            if (data.wetSensitive)
            {
                result *= 1f - Mathf.Clamp01(wetness.Value / data.secondsToRuin);
            }
            return result;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    // Server: call before Spawn() to choose what kind of package this is.
    public void ServerInit(int catalogIndex, string destinationAddress)
    {
        pendingDataIndex = catalogIndex;
        pendingDestination = destinationAddress;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            dataIndex.Value = pendingDataIndex;
            destination.Value = new FixedString64Bytes(pendingDestination);
            spawnTime = Time.time;
        }

        ResolveData();
        if (data == null)
        {
            return; // ResolveData already logged why
        }

        if (IsServer)
        {
            health.Value = data.maxHealth;
            body.mass = data.mass;
        }
        else
        {
            body.isKinematic = true; // only the server simulates physics
        }

        health.OnValueChanged += OnConditionChanged;
        wetness.OnValueChanged += OnConditionChanged;
        UpdateLook();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnConditionChanged;
        wetness.OnValueChanged -= OnConditionChanged;

        if (IsServer)
        {
            ReleaseAllHolders();
        }
    }

    private void ResolveData()
    {
        PackageCatalog catalog = GameNetworkConfig.Instance != null ? GameNetworkConfig.Instance.PackageCatalog : null;
        data = catalog != null ? catalog.Get(dataIndex.Value) : null;
        if (data == null)
        {
            Debug.LogError("Package has no PackageData. Is the PackageCatalog set on GameNetworkConfig?");
            return;
        }

        transform.localScale = data.size;
        baseColor = data.color;
    }

    // ---------- Server: physics ----------

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned || data == null)
        {
            return;
        }
        HandleWater();
        HandleHolding();
    }

    private void HandleWater()
    {
        float floorY = GetFloorY();
        float depth = WaterVolume.GetDepthAt(transform.position, floorY);
        bool submerged = depth > 0f && transform.position.y < floorY + depth;
        if (!submerged)
        {
            return;
        }

        // Light packages float, heavy ones sink
        if (data.mass <= floatMassLimit)
        {
            body.AddForce(Vector3.up * buoyancyAcceleration, ForceMode.Acceleration);
        }
        body.linearVelocity *= 1f - waterDrag * Time.fixedDeltaTime;

        if (data.wetSensitive)
        {
            wetSeconds += Time.fixedDeltaTime;
            if (wetSeconds - wetness.Value >= 0.25f) // send updates in small steps, not every physics tick
            {
                wetness.Value = wetSeconds;
            }
        }
    }

    private float GetFloorY()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody != body)
            {
                return hit.point.y;
            }
        }
        return transform.position.y - 1f;
    }

    private void HandleHolding()
    {
        // Drop anyone who left, died, or whose hold point is stuck far away.
        tooFar.Clear();
        for (int i = holders.Count - 1; i >= 0; i--)
        {
            PlayerGrabber holder = holders[i];
            if (holder == null || !holder.IsSpawned)
            {
                holders.RemoveAt(i);
                continue;
            }
            if ((holder.HoldPoint - body.position).magnitude > breakDistance)
            {
                tooFar.Add(holder);
            }
        }
        foreach (PlayerGrabber holder in tooFar)
        {
            holder.ServerDrop();
        }

        if (holders.Count == 0)
        {
            body.useGravity = true;
            return;
        }

        if (!IsLifted)
        {
            // One person grabbed a two-person package: it cannot be lifted yet.
            body.useGravity = true;
            return;
        }

        // Pull toward the average of everyone's hold point.
        Vector3 target = Vector3.zero;
        foreach (PlayerGrabber holder in holders)
        {
            target += holder.HoldPoint;
        }
        target /= holders.Count;

        body.useGravity = false;
        Vector3 velocity = Vector3.ClampMagnitude((target - body.position) * holdStrength, maxHoldSpeed);
        body.linearVelocity = velocity;
        body.angularVelocity *= 0.8f; // stop wild spinning
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || data == null || !data.isFragile)
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed <= data.safeImpactSpeed)
        {
            return;
        }

        float damage = (impactSpeed - data.safeImpactSpeed) * data.damagePerSpeed;
        health.Value = Mathf.Max(0f, health.Value - damage);

        // Hard impacts are loud
        GameEvents.RaiseNoise(transform.position, Mathf.Clamp(impactSpeed, 1f, 10f), false);
    }

    // ---------- Server: holders ----------

    public bool ServerTryAddHolder(PlayerGrabber grabber)
    {
        if (!IsServer || delivered.Value || data == null)
        {
            return false;
        }
        if (holders.Contains(grabber) || holders.Count >= data.holdersRequired)
        {
            return false;
        }

        holders.Add(grabber);
        RefreshLoads();
        return true;
    }

    // Called by PlayerGrabber.ServerDrop (the grabber has already forgotten the package).
    public void ServerRemoveHolder(PlayerGrabber grabber)
    {
        holders.Remove(grabber);
        if (holders.Count == 0)
        {
            body.useGravity = true;
        }
        RefreshLoads();
    }

    private void ReleaseAllHolders()
    {
        List<PlayerGrabber> copy = new List<PlayerGrabber>(holders);
        holders.Clear();
        foreach (PlayerGrabber holder in copy)
        {
            if (holder != null && holder.IsSpawned)
            {
                holder.ServerForget(this);
            }
        }
        body.useGravity = true;
    }

    // Each holder carries a share of the weight once the package is lifted.
    private void RefreshLoads()
    {
        float load = IsLifted ? body.mass / Mathf.Max(1, holders.Count) : body.mass;
        foreach (PlayerGrabber holder in holders)
        {
            holder.ServerSetLoad(load);
        }
    }

    // ---------- Server: delivery ----------

    public void ServerMarkDelivered()
    {
        if (!IsServer)
        {
            return;
        }
        delivered.Value = true;
        ReleaseAllHolders();
        Invoke(nameof(ServerDespawn), 0.3f);
    }

    public void ServerDespawn()
    {
        if (IsServer && IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    // ---------- Look (all machines) ----------

    private void OnConditionChanged(float previous, float current)
    {
        UpdateLook();
    }

    // The package darkens and turns red-brown as it takes damage or gets wet.
    private void UpdateLook()
    {
        if (cachedRenderer == null || data == null)
        {
            return;
        }
        Color damaged = new Color(0.35f, 0.15f, 0.1f);
        cachedRenderer.material.color = Color.Lerp(baseColor, damaged, 1f - Condition);
    }
}
