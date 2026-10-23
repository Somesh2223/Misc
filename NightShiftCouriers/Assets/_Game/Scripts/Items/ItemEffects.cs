using Unity.Netcode;
using UnityEngine;

// What each item does when used (plan section 4.5). SERVER only.
// One plain switch statement, so every item's behaviour is easy to find.
public static class ItemEffects
{
    private static GameNetworkConfig Config => GameNetworkConfig.Instance;

    public static void ServerUse(PlayerInventory inventory, int slot, ItemData item, Vector3 origin, Vector3 direction)
    {
        NetworkPlayer user = inventory.Player;

        switch (item.type)
        {
            case ItemType.Flashlight:
                if (inventory.GetUses(slot) > 0)
                {
                    inventory.ServerSetFlashlight(!inventory.FlashlightOn);
                }
                break;

            case ItemType.Rope:
                if (PlaceRope(origin, direction))
                {
                    inventory.ServerConsume(slot);
                }
                break;

            case ItemType.Plank:
                // On a raft the plank is a paddle; elsewhere it becomes a bridge.
                if (user.IsOnRaft)
                {
                    Paddle(user, 1f);
                }
                else if (PlacePlank(user))
                {
                    inventory.ServerConsume(slot);
                }
                break;

            case ItemType.Radio:
                // Passive: carrying a radio puts you on the long-range voice channel (see PlayerVoice).
                break;

            case ItemType.Decoy:
                ThrowDecoy(origin, direction);
                inventory.ServerConsume(slot);
                break;

            case ItemType.SaltPouch:
                PlaceSaltWard(user, item.duration);
                inventory.ServerConsume(slot);
                break;

            case ItemType.LemonChilliCharm:
                inventory.ServerSetCharm(item.charmAgainst, item.duration);
                CampaignManager.Instance.ServerMessage("The charm hides you from the " + item.charmAgainst + " for "
                                                       + Mathf.RoundToInt(item.duration) + " s.", user.OwnerClientId);
                inventory.ServerConsume(slot);
                break;

            case ItemType.RepairKit:
                if (Repair(user))
                {
                    inventory.ServerConsume(slot);
                }
                break;

            case ItemType.GlowSticks:
                DropGlowStick(origin, direction);
                inventory.ServerConsume(slot);
                break;
        }
    }

    // Right mouse with an empty slot: paddle by hand if standing on a raft.
    public static void ServerUseEmptyHand(NetworkPlayer user)
    {
        if (user.IsOnRaft)
        {
            Paddle(user, 0.5f);
        }
    }

    // ---------- Individual items ----------

    // A rope hangs straight down from where you aim (a ledge or wall top), and can be climbed.
    private static bool PlaceRope(Vector3 origin, Vector3 direction)
    {
        if (!Physics.Raycast(origin, direction, out RaycastHit anchor, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 top = anchor.point + anchor.normal * 0.2f;
        if (!Physics.Raycast(top, Vector3.down, out RaycastHit floor, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        if (top.y - floor.point.y < 1f)
        {
            return false; // nothing to climb
        }

        GameObject rope = Object.Instantiate(Config.ItemPrefabs.rope, top, Quaternion.identity);
        rope.GetComponent<PlacedRope>().ServerInit(top, floor.point);
        Track(rope);
        return true;
    }

    // A 4 m plank laid flat in front of you at your feet height, to bridge gaps between rooftops.
    private static bool PlacePlank(NetworkPlayer user)
    {
        Vector3 forward = user.Forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
        {
            return false;
        }
        forward.Normalize();

        const float length = 4f;
        Vector3 centre = user.Position + forward * (0.4f + length / 2f) - Vector3.up * 0.05f;
        GameObject plank = Object.Instantiate(Config.ItemPrefabs.plank, centre, Quaternion.LookRotation(forward));
        Track(plank);
        return true;
    }

    private static void ThrowDecoy(Vector3 origin, Vector3 direction)
    {
        GameObject decoy = Object.Instantiate(Config.ItemPrefabs.decoy, origin + direction * 0.6f, Quaternion.identity);
        Track(decoy);
        decoy.GetComponent<Rigidbody>().linearVelocity = direction * 10f + Vector3.up * 2f;
    }

    private static void PlaceSaltWard(NetworkPlayer user, float seconds)
    {
        GameObject ward = Object.Instantiate(Config.ItemPrefabs.saltWard, user.Position, Quaternion.identity);
        ward.GetComponent<SaltWard>().ServerInit(seconds);
        Track(ward);
    }

    private static void DropGlowStick(Vector3 origin, Vector3 direction)
    {
        GameObject stick = Object.Instantiate(Config.ItemPrefabs.glowStick, origin + direction * 0.5f, Quaternion.identity);
        Track(stick);
        stick.GetComponent<Rigidbody>().linearVelocity = direction * 5f;
    }

    // On a raft: patch the raft. Otherwise: partly repair the fragile package you are holding.
    private static bool Repair(NetworkPlayer user)
    {
        if (user.IsOnRaft && Raft.Current != null)
        {
            return Raft.Current.ServerRepair(1);
        }

        Package held = user.GetComponent<PlayerGrabber>().ServerHeldPackage;
        return held != null && held.ServerRepair(40f);
    }

    private static void Paddle(NetworkPlayer user, float strength)
    {
        if (Raft.Current != null)
        {
            Raft.Current.ServerPaddle(user, user.Forward, strength);
        }
    }

    // Buying lashing rope drops a lashing rope package next to the depot shop.
    public static void ServerDeliverLashingRope()
    {
        Vector3 position = ShopTerminal.DropPoint;
        ShiftSpawner.Instance.ServerSpawnPackage(Config.RaftPartLashing, position, "");
    }

    private static void Track(GameObject spawned)
    {
        ShiftSpawner.Instance.ServerSpawnAndTrack(spawned.GetComponent<NetworkObject>());
    }
}
