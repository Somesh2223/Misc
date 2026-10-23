using Unity.Netcode;
using UnityEngine;

// Four item slots per player (plan section 4.5). SERVER-authoritative: the server adds, uses and removes items.
// The owner picks a slot with 1-4 and uses it with the right mouse button.
public class PlayerInventory : NetworkBehaviour
{
    public const int SlotCount = 4;
    private const int Empty = -1;

    [Header("Flashlight")]
    [SerializeField, Tooltip("A spot light on the player, switched on and off by the flashlight item.")]
    private Light flashlight;

    private NetworkList<int> items;   // index into the ItemCatalog, or -1
    private NetworkList<int> uses;    // uses (or flashlight battery seconds) left in each slot
    private NetworkList<int> payloads; // extra data per slot (a courier badge: whose badge it is)

    private readonly NetworkVariable<int> selected = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> flashlightOn = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> charmUntil = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> charmKind = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerInputReader input;
    private NetworkPlayer player;
    private float batteryTimer;

    public NetworkPlayer Player => player;
    public int SelectedSlot => selected.Value;
    public bool FlashlightOn => flashlightOn.Value;

    private static ItemCatalog Catalog => GameNetworkConfig.Instance.ItemCatalog;

    private void Awake()
    {
        // NetworkLists must be created before the object is spawned.
        items = new NetworkList<int>();
        uses = new NetworkList<int>();
        payloads = new NetworkList<int>();
        input = GetComponent<PlayerInputReader>();
        player = GetComponent<NetworkPlayer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ServerClearAll();
        }
        flashlightOn.OnValueChanged += OnFlashlightChanged;
        OnFlashlightChanged(false, flashlightOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        flashlightOn.OnValueChanged -= OnFlashlightChanged;
    }

    private void OnFlashlightChanged(bool previous, bool current)
    {
        if (flashlight != null)
        {
            flashlight.enabled = current;
        }
    }

    // ---------- Reading ----------

    public ItemData GetItem(int slot)
    {
        if (slot < 0 || slot >= items.Count)
        {
            return null;
        }
        return Catalog.Get(items[slot]);
    }

    public int GetUses(int slot)
    {
        if (slot < 0 || slot >= uses.Count)
        {
            return 0;
        }
        return uses[slot];
    }

    public int GetPayload(int slot)
    {
        if (slot < 0 || slot >= payloads.Count)
        {
            return 0;
        }
        return payloads[slot];
    }

    public bool HasItem(ItemType type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = Catalog.Get(items[i]);
            if (item != null && item.type == type)
            {
                return true;
            }
        }
        return false;
    }

    // True while a lemon-chilli charm hides this player from that monster type.
    public bool IsCharmedAgainst(MonsterKind kind)
    {
        if (NetworkManager == null)
        {
            return false;
        }
        return charmKind.Value == (int)kind && NetworkManager.ServerTime.Time < charmUntil.Value;
    }

    // ---------- Owner: input ----------

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        // The flashlight follows where the player looks (everyone sees the beam).
        if (flashlight != null)
        {
            flashlight.transform.localRotation = Quaternion.Euler(player.Pitch, 0f, 0f);
        }

        if (IsServer)
        {
            DrainBattery();
        }

        if (!IsOwner || UIState.MenuOpen || player.IsDead)
        {
            return;
        }

        int slot = input.SlotPressedThisFrame;
        if (slot >= 0)
        {
            selected.Value = slot;
        }

        if (input.UsePressedThisFrame)
        {
            UseServerRpc(selected.Value, player.AimOrigin, player.AimDirection);
        }
    }

    [ServerRpc]
    private void UseServerRpc(int slot, Vector3 origin, Vector3 direction)
    {
        if (player.IsDead || slot < 0 || slot >= SlotCount)
        {
            return;
        }
        // The aim must start near the player.
        if ((origin - player.Position).sqrMagnitude > 9f)
        {
            return;
        }

        ItemData item = GetItem(slot);
        if (item == null)
        {
            ItemEffects.ServerUseEmptyHand(player);
            return;
        }
        ItemEffects.ServerUse(this, slot, item, origin, direction.normalized);
    }

    // ---------- Shop ----------

    // Called from the shop window on the owner's machine.
    public void RequestBuy(int catalogIndex)
    {
        BuyServerRpc(catalogIndex);
    }

    [ServerRpc]
    private void BuyServerRpc(int catalogIndex)
    {
        ItemData item = Catalog.Get(catalogIndex);
        CampaignManager campaign = CampaignManager.Instance;
        if (item == null || campaign == null || !campaign.ShopOpen)
        {
            return;
        }

        bool isRaftPart = item.type == ItemType.LashingRope;
        if (!isRaftPart && FreeSlot() < 0)
        {
            campaign.ServerMessage("Inventory full.", OwnerClientId);
            return;
        }
        if (!campaign.ServerTrySpend(item.price))
        {
            campaign.ServerMessage("Not enough money for " + item.displayName + ".", OwnerClientId);
            return;
        }

        if (isRaftPart)
        {
            ItemEffects.ServerDeliverLashingRope();
        }
        else
        {
            ServerTryAdd(catalogIndex);
        }
        campaign.ServerMessage("Bought " + item.displayName + ".", OwnerClientId);
    }

    // ---------- Server: changing the inventory ----------

    public void ServerClearAll()
    {
        if (!IsServer)
        {
            return;
        }
        items.Clear();
        uses.Clear();
        payloads.Clear();
        for (int i = 0; i < SlotCount; i++)
        {
            items.Add(Empty);
            uses.Add(0);
            payloads.Add(0);
        }
        flashlightOn.Value = false;
    }

    public bool ServerTryAdd(int catalogIndex, int payload = 0)
    {
        ItemData item = Catalog.Get(catalogIndex);
        int slot = FreeSlot();
        if (!IsServer || item == null || slot < 0)
        {
            return false;
        }
        items[slot] = catalogIndex;
        uses[slot] = item.uses;
        payloads[slot] = payload;
        return true;
    }

    public void ServerRemove(int slot)
    {
        if (!IsServer || slot < 0 || slot >= SlotCount)
        {
            return;
        }
        items[slot] = Empty;
        uses[slot] = 0;
        payloads[slot] = 0;
    }

    // Removes every item of one type (e.g. stale courier badges when the crew is back at the depot).
    public void ServerRemoveAll(ItemType type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = Catalog.Get(items[i]);
            if (item != null && item.type == type)
            {
                ServerRemove(i);
            }
        }
    }

    // Uses up one charge; the item disappears when it has none left.
    public void ServerConsume(int slot)
    {
        if (!IsServer || slot < 0 || slot >= SlotCount)
        {
            return;
        }
        uses[slot] = uses[slot] - 1;
        if (uses[slot] <= 0)
        {
            ServerRemove(slot);
        }
    }

    public void ServerSetFlashlight(bool on)
    {
        flashlightOn.Value = on;
    }

    public void ServerSetCharm(MonsterKind kind, float seconds)
    {
        charmKind.Value = (int)kind;
        charmUntil.Value = NetworkManager.ServerTime.Time + seconds;
    }

    private int FreeSlot()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == Empty)
            {
                return i;
            }
        }
        return -1;
    }

    // While on, the flashlight uses one battery charge per second. A dead flashlight stays in the slot.
    private void DrainBattery()
    {
        if (!flashlightOn.Value)
        {
            return;
        }

        batteryTimer += Time.deltaTime;
        if (batteryTimer < 1f)
        {
            return;
        }
        batteryTimer = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = Catalog.Get(items[i]);
            if (item != null && item.type == ItemType.Flashlight && uses[i] > 0)
            {
                uses[i] = uses[i] - 1;
                if (uses[i] <= 0)
                {
                    flashlightOn.Value = false;
                }
                return;
            }
        }
        flashlightOn.Value = false; // no flashlight left in the inventory
    }
}
