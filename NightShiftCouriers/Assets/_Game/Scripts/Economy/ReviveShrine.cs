using UnityEngine;

// A shrine (plan 4.10): bring a fallen teammate's courier ID badge here and press E to revive them.
// The depot has one. Runs on the server (it is used through PlayerGrabber's E interaction).
public class ReviveShrine : MonoBehaviour, IServerInteractable
{
    public void ServerInteract(NetworkPlayer player)
    {
        if (player.IsDead)
        {
            return;
        }

        bool revivedSomeone = false;
        for (int slot = 0; slot < PlayerInventory.SlotCount; slot++)
        {
            ItemData item = player.Inventory.GetItem(slot);
            if (item == null || item.type != ItemType.CourierBadge)
            {
                continue;
            }

            ulong ownerId = (ulong)player.Inventory.GetPayload(slot);
            player.Inventory.ServerRemove(slot);

            foreach (NetworkPlayer fallen in NetworkPlayer.All)
            {
                if (fallen.OwnerClientId == ownerId && fallen.IsDead)
                {
                    Vector3 spot = transform.position + transform.forward * 1.5f;
                    fallen.Health.ServerReviveAt(spot);
                    CampaignManager.Instance.ServerMessage("Courier " + ownerId + " has been brought back!");
                    revivedSomeone = true;
                }
            }
        }

        if (!revivedSomeone)
        {
            CampaignManager.Instance.ServerMessage("Bring a fallen courier's ID badge here to revive them.", player.OwnerClientId);
        }
    }
}
