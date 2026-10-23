using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Tracks the night's orders: pick up -> deliver -> get paid. SERVER-authoritative (plan section 5):
// money, payouts and orders are decided on the server and copied to every player's HUD.
public class DeliveryManager : NetworkBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("Payout")]
    [SerializeField, Tooltip("Deliver faster than this many seconds for a speed bonus.")]
    private float parSeconds = 240f;
    [SerializeField, Tooltip("Largest speed bonus (0.25 = +25%).")]
    private float maxSpeedBonus = 0.25f;

    [Header("Cleanup")]
    [SerializeField, Tooltip("Packages that fall below this height are counted as lost.")]
    private float killHeight = -20f;

    private readonly NetworkVariable<int> money = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> quota = new NetworkVariable<int>(
        400, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> shiftOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString512Bytes> orderSummary = new NetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<DeliveryOrder> orders = new List<DeliveryOrder>();
    private int pendingQuota = 400;
    private string lastMessage = "";
    private float messageTimer;

    public int Quota => quota.Value;
    public int Money => money.Value;
    public bool ShiftOver => shiftOver.Value;
    public bool QuotaMet => money.Value >= quota.Value;
    public string OrderSummary => orderSummary.Value.ToString();
    public string Message => messageTimer > 0f ? lastMessage : "";

    // Server: call before Spawn().
    public void ServerInit(int quotaAmount)
    {
        pendingQuota = quotaAmount;
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer)
        {
            quota.Value = pendingQuota;
            money.Value = 0;
            shiftOver.Value = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---------- Server ----------

    // The shift spawner calls this for every package it creates.
    public void ServerRegisterPackage(Package package)
    {
        if (!IsServer || string.IsNullOrEmpty(package.DestinationAddress))
        {
            return;
        }

        DeliveryOrder order = new DeliveryOrder();
        order.package = package;
        order.address = package.DestinationAddress;
        order.packageName = package.Data.displayName;
        orders.Add(order);
        RebuildSummary();
    }

    private void Update()
    {
        messageTimer -= Time.deltaTime;

        if (!IsServer || !IsSpawned)
        {
            return;
        }

        CheckForLostPackages();
        CheckShiftOver();

        if (shiftOver.Value && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame
            && ShiftSpawner.Instance != null)
        {
            ShiftSpawner.Instance.RestartShift();
        }
    }

    // Called by a DeliveryPoint when a loose package sits inside it (server only).
    public void ServerTryDeliver(Package package, DeliveryPoint point)
    {
        DeliveryOrder order = FindOrder(package);
        if (order == null || order.status != OrderStatus.Pending)
        {
            return;
        }

        if (order.address != point.Address)
        {
            MessageClientRpc(new FixedString128Bytes("Wrong address! This one goes to " + order.address + "."));
            return;
        }

        int payout = CalculatePayout(package);
        order.status = OrderStatus.Delivered;
        order.payout = payout;
        money.Value += payout;
        GameEvents.RaisePackageDelivered(package, payout);
        MessageClientRpc(new FixedString128Bytes("Delivered to " + order.address + "!  +" + payout));

        package.ServerMarkDelivered();
        RebuildSummary();
    }

    // Money = base value x condition x speed bonus (plan section 4.15).
    public int CalculatePayout(Package package)
    {
        float value = package.Data.baseValue * package.Condition;

        float timeLeftFraction = Mathf.Clamp01((parSeconds - package.AgeSeconds) / parSeconds);
        float speedBonus = 1f + maxSpeedBonus * timeLeftFraction;

        return Mathf.RoundToInt(value * speedBonus);
    }

    private DeliveryOrder FindOrder(Package package)
    {
        foreach (DeliveryOrder order in orders)
        {
            if (order.package == package)
            {
                return order;
            }
        }
        return null;
    }

    private void CheckForLostPackages()
    {
        bool changed = false;
        foreach (DeliveryOrder order in orders)
        {
            if (order.status != OrderStatus.Pending)
            {
                continue;
            }

            bool gone = order.package == null || !order.package.IsSpawned;
            bool fellOut = !gone && order.package.transform.position.y < killHeight;
            if (gone || fellOut)
            {
                order.status = OrderStatus.Lost;
                if (fellOut)
                {
                    order.package.ServerDespawn();
                }
                MessageClientRpc(new FixedString128Bytes("A package was lost!"));
                changed = true;
            }
        }

        if (changed)
        {
            RebuildSummary();
        }
    }

    private void CheckShiftOver()
    {
        if (shiftOver.Value || orders.Count == 0)
        {
            return;
        }

        foreach (DeliveryOrder order in orders)
        {
            if (order.status == OrderStatus.Pending)
            {
                return;
            }
        }
        shiftOver.Value = true;
    }

    private void RebuildSummary()
    {
        StringBuilder text = new StringBuilder();
        foreach (DeliveryOrder order in orders)
        {
            text.Append(order.packageName).Append(" -> ").Append(order.address).Append("  [").Append(order.status);
            if (order.status == OrderStatus.Delivered)
            {
                text.Append(" +").Append(order.payout);
            }
            text.Append("]\n");
        }

        // FixedString512 holds ~500 bytes; cut the text rather than throw if there are many orders.
        string result = text.ToString();
        if (result.Length > 450)
        {
            result = result.Substring(0, 450);
        }
        orderSummary.Value = new FixedString512Bytes(result);
    }

    [ClientRpc]
    private void MessageClientRpc(FixedString128Bytes text)
    {
        lastMessage = text.ToString();
        messageTimer = 4f;
    }
}
