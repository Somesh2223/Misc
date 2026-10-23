using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
    // "address|address|..." of tonight's orders, so the journal can show each customer's note.
    private readonly NetworkVariable<FixedString512Bytes> orderAddresses = new NetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString512Bytes> orderSummary = new NetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<DeliveryOrder> orders = new List<DeliveryOrder>();
    private readonly Dictionary<string, int> knocks = new Dictionary<string, int>();

    // Money earned this shift (paid into the campaign bank when the shift ends).
    public int Money => money.Value;
    public string OrderSummary => orderSummary.Value.ToString();

    // Tonight's delivery addresses (every machine).
    public string[] OrderAddresses
    {
        get
        {
            string all = orderAddresses.Value.ToString();
            return all == "" ? new string[0] : all.Split('|');
        }
    }

    // Server: true once every order is delivered or lost.
    public bool AllOrdersResolved
    {
        get
        {
            if (orders.Count == 0)
            {
                return false;
            }
            foreach (DeliveryOrder order in orders)
            {
                if (order.status == OrderStatus.Pending)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer)
        {
            money.Value = 0;
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
        if (!IsServer || !IsSpawned)
        {
            return;
        }
        CheckForLostPackages();
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

        // The customer's rule (plan 4.8): a bonus for following it, trouble for breaking it.
        CustomerData customer = GameNetworkConfig.Instance.CustomerDirectory.Find(order.address);
        if (customer != null && customer.rule != CustomerRule.None && customer.rule != CustomerRule.NobodyAnswers)
        {
            if (RuleFollowed(customer, point.transform.position))
            {
                payout = Mathf.RoundToInt(payout * customer.ruleBonus);
                MessageClientRpc(new FixedString128Bytes(customer.customerName + " seems pleased. Bonus pay!"));
            }
            else
            {
                RuleBroken(customer, point.transform.position);
            }
        }
        if (customer != null && customer.loreEntry >= 0)
        {
            CampaignManager.Instance.ServerUnlockLore(customer.loreEntry);
        }

        order.status = OrderStatus.Delivered;
        order.payout = payout;
        money.Value += payout;
        GameEvents.RaisePackageDelivered(package, payout);
        MessageClientRpc(new FixedString128Bytes("Delivered to " + order.address + "!  +" + payout));

        package.ServerMarkDelivered();
        RebuildSummary();
    }

    // Extra money from outside a delivery (cash in a cursed package).
    public void ServerAddBonus(int amount)
    {
        money.Value += amount;
    }

    // A cursed package was opened and is gone: its order is lost, quietly.
    public void ServerMarkOpened(Package package)
    {
        DeliveryOrder order = FindOrder(package);
        if (order != null && order.status == OrderStatus.Pending)
        {
            order.status = OrderStatus.Lost;
            RebuildSummary();
        }
    }

    // ---------- Customer rules ----------

    public void ServerKnock(string address, Vector3 doorPosition)
    {
        knocks.TryGetValue(address, out int count);
        count++;
        knocks[address] = count;
        KnockClientRpc(doorPosition);

        CustomerData customer = GameNetworkConfig.Instance.CustomerDirectory.Find(address);
        if (customer != null && customer.rule == CustomerRule.KnockOnce && count == 2)
        {
            // Something answers the second knock.
            RuleBroken(customer, doorPosition);
        }
    }

    private bool RuleFollowed(CustomerData customer, Vector3 doorPosition)
    {
        switch (customer.rule)
        {
            case CustomerRule.ComeAlone:
                return CountPlayersNear(doorPosition, 10f, false) <= 1;
            case CustomerRule.NoLights:
                return CountPlayersNear(doorPosition, 10f, true) == 0;
            case CustomerRule.BeforeSecondRise:
                return FloodManager.Instance == null || FloodManager.Instance.StageIndex < 2;
            case CustomerRule.KnockOnce:
                knocks.TryGetValue(customer.address, out int count);
                return count == 1;
        }
        return true;
    }

    private static int CountPlayersNear(Vector3 position, float radius, bool onlyWithLights)
    {
        int count = 0;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player.IsDead || Vector3.Distance(player.Position, position) > radius)
            {
                continue;
            }
            if (onlyWithLights && (player.Inventory == null || !player.Inventory.FlashlightOn))
            {
                continue;
            }
            count++;
        }
        return count;
    }

    // Breaking a rule can trigger an event (plan 4.8): something comes for you, or a strange "gift" appears.
    private void RuleBroken(CustomerData customer, Vector3 doorPosition)
    {
        float roll = Random.value;
        if (roll < 0.5f)
        {
            MessageClientRpc(new FixedString128Bytes("You broke " + customer.customerName + "'s rule. Something is coming..."));
            ShiftSpawner.Instance.ServerSpawnMonster(MonsterKind.Chudail, doorPosition + Vector3.back * 6f);
        }
        else if (roll < 0.8f)
        {
            MessageClientRpc(new FixedString128Bytes("A package is left on the step for you. It is warm."));
            ShiftSpawner.Instance.ServerSpawnPackage(GameNetworkConfig.Instance.PackageCatalog.Get(0),
                doorPosition + Vector3.up * 0.5f, "", true);
        }
        else
        {
            MessageClientRpc(new FixedString128Bytes("Behind the door, someone laughs at you."));
        }
    }

    [ClientRpc]
    private void KnockClientRpc(Vector3 doorPosition)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Bang, doorPosition, 0.3f, 15f);
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

        StringBuilder addresses = new StringBuilder();
        foreach (DeliveryOrder order in orders)
        {
            if (addresses.Length > 0)
            {
                addresses.Append('|');
            }
            addresses.Append(order.address);
        }
        string addressText = addresses.ToString();
        if (addressText.Length > 450)
        {
            addressText = addressText.Substring(0, 450);
        }
        orderAddresses.Value = new FixedString512Bytes(addressText);
    }

    [ClientRpc]
    private void MessageClientRpc(FixedString128Bytes text)
    {
        MessageFeed.Show(text.ToString());
    }
}
