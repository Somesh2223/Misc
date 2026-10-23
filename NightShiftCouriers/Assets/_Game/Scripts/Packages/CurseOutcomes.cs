using Unity.Netcode;
using UnityEngine;

// Runs the result of opening a cursed package (plan 4.9). SERVER only.
public static class CurseOutcomes
{
    public static void ServerOpen(Package package, NetworkPlayer opener)
    {
        GameNetworkConfig config = GameNetworkConfig.Instance;
        CurseTable.Entry result = config.CurseTable.Roll();
        Vector3 position = package.transform.position;

        switch (result.outcome)
        {
            case CurseOutcome.RareItem:
                DropRandomItem(position + Vector3.up * 0.3f);
                break;

            case CurseOutcome.BigMoney:
                if (DeliveryManager.Instance != null)
                {
                    DeliveryManager.Instance.ServerAddBonus(config.CurseTable.bigMoneyAmount);
                }
                break;

            case CurseOutcome.LoreReveal:
                CampaignManager.Instance.ServerUnlockRandomLore();
                break;

            case CurseOutcome.MonsterReleased:
                ShiftSpawner.Instance.ServerSpawnMonster(MonsterKind.Chudail, position);
                break;

            case CurseOutcome.FloodJump:
                if (FloodManager.Instance != null)
                {
                    FloodManager.Instance.ServerAdvanceStage();
                }
                break;

            case CurseOutcome.Screaming:
                package.ServerStartScreaming();
                break;
        }

        CampaignManager.Instance.ServerMessage("Courier " + opener.OwnerClientId + " opened a cursed package... " + result.message);
        package.ServerAnnounceOpened();

        // Everything but the screaming package vanishes once opened (its order is lost).
        if (result.outcome != CurseOutcome.Screaming)
        {
            if (DeliveryManager.Instance != null)
            {
                DeliveryManager.Instance.ServerMarkOpened(package);
            }
            package.ServerDespawn();
        }
    }

    private static void DropRandomItem(Vector3 position)
    {
        GameNetworkConfig config = GameNetworkConfig.Instance;
        ItemCatalog catalog = config.ItemCatalog;

        // Only shop items (no raft parts or badges).
        int index = -1;
        for (int tries = 0; tries < 20 && index < 0; tries++)
        {
            int candidate = Random.Range(0, catalog.items.Length);
            ItemData item = catalog.items[candidate];
            if (item.inShop && item.type != ItemType.LashingRope)
            {
                index = candidate;
            }
        }
        if (index < 0)
        {
            return;
        }

        GameObject instance = Object.Instantiate(config.ItemPrefabs.worldItem, position, Quaternion.identity);
        instance.GetComponent<WorldItem>().ServerInit(index);
        ShiftSpawner.Instance.ServerSpawnAndTrack(instance.GetComponent<NetworkObject>());
    }
}
