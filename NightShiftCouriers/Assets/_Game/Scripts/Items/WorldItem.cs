using Unity.Netcode;
using UnityEngine;

// An item lying in the world. Press E on it to put it in your inventory.
public class WorldItem : NetworkBehaviour, IServerInteractable
{
    private readonly NetworkVariable<int> itemIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int pendingIndex = -1;
    private int payload; // server only: e.g. whose courier badge this is

    // What this item is (for the "[E] Pick up ..." prompt).
    public ItemData Item => GameNetworkConfig.Instance != null ? GameNetworkConfig.Instance.ItemCatalog.Get(itemIndex.Value) : null;

    public void ServerInit(int catalogIndex, int payloadValue = 0)
    {
        pendingIndex = catalogIndex;
        payload = payloadValue;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            itemIndex.Value = pendingIndex;
        }

        ItemData item = GameNetworkConfig.Instance.ItemCatalog.Get(itemIndex.Value);
        Renderer visual = GetComponentInChildren<Renderer>();
        if (item != null && visual != null)
        {
            visual.material.color = item.color;
        }
    }

    public void ServerInteract(NetworkPlayer player)
    {
        if (!IsSpawned)
        {
            return;
        }
        if (player.IsDead)
        {
            return;
        }
        if (player.Inventory.ServerTryAdd(itemIndex.Value, payload))
        {
            ItemData item = GameNetworkConfig.Instance.ItemCatalog.Get(itemIndex.Value);
            CampaignManager.Instance.ServerMessage("Picked up " + item.displayName + ".", player.OwnerClientId);
            NetworkObject.Despawn(true);
        }
        else
        {
            CampaignManager.Instance.ServerMessage("Inventory full.", player.OwnerClientId);
        }
    }
}
