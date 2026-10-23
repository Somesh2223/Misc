using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// A place where the crew can build a raft (plan section 4.4): a hidden workbench, a flooded godown...
// Bring the three parts into the parts zone, then two players press E at the workbench.
// Building takes a few seconds and is noisy. Only one raft per crew. Runs on the SERVER.
public class RaftSpot : MonoBehaviour, IServerInteractable
{
    [SerializeField, Tooltip("Trigger box where the parts must be dropped.")]
    private BoxCollider partsZone;
    [SerializeField, Tooltip("Where the finished raft appears (on the water).")]
    private Transform launchPoint;
    [SerializeField] private float buildSeconds = 6f;
    [SerializeField, Tooltip("Builders further away than this stop building.")]
    private float buildRange = 4f;
    [SerializeField, Tooltip("Noise of hammering and lashing (non-water noise).")]
    private float buildLoudness = 4f;

    private readonly List<NetworkPlayer> builders = new List<NetworkPlayer>();
    private float progress;
    private float tickTimer;

    private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    public void ServerInteract(NetworkPlayer player)
    {
        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null || campaign.Phase != ShiftPhase.Shift)
        {
            return;
        }
        if (Raft.Current != null)
        {
            campaign.ServerMessage("Your crew already has a raft.", player.OwnerClientId);
            return;
        }

        if (builders.Contains(player))
        {
            builders.Remove(player);
            campaign.ServerMessage("You stop building.", player.OwnerClientId);
        }
        else
        {
            builders.Add(player);
            campaign.ServerMessage("You start building the raft. Stay close!", player.OwnerClientId);
        }
    }

    private void Update()
    {
        if (!IsServer || builders.Count == 0)
        {
            return;
        }

        RemoveGoneBuilders();
        if (builders.Count == 0)
        {
            progress = 0f;
            return;
        }

        tickTimer -= Time.deltaTime;
        bool showStatus = tickTimer <= 0f;
        if (showStatus)
        {
            tickTimer = 1f;
        }

        Package floaters = FindPart(RaftPartType.Floaters);
        Package platform = FindPart(RaftPartType.Platform);
        Package lashing = FindPart(RaftPartType.LashingRope);
        if (floaters == null || platform == null || lashing == null)
        {
            if (showStatus)
            {
                Tell("Missing parts:" + (floaters == null ? " floaters" : "") + (platform == null ? " platform" : "")
                     + (lashing == null ? " lashing rope" : "") + " (drop them by the workbench)");
            }
            return;
        }

        // Two players needed (or everyone, if the crew is smaller than that).
        int required = Mathf.Min(2, NetworkManager.Singleton.ConnectedClientsIds.Count);
        if (builders.Count < required)
        {
            if (showStatus)
            {
                Tell("Building needs " + required + " people at the workbench (press E).");
            }
            return;
        }

        // A Builder on the crew builds faster (plan 4.7).
        float speed = 1f;
        foreach (NetworkPlayer builder in builders)
        {
            if (builder.Passive == RolePassive.Builder)
            {
                speed = 1.6f;
            }
        }
        progress += Time.deltaTime * speed;
        if (showStatus)
        {
            GameEvents.RaiseNoise(transform.position, buildLoudness, false);
            Tell("Building raft... " + Mathf.RoundToInt(100f * progress / buildSeconds) + "%");
        }

        if (progress >= buildSeconds)
        {
            Finish(floaters, platform, lashing);
        }
    }

    private void RemoveGoneBuilders()
    {
        for (int i = builders.Count - 1; i >= 0; i--)
        {
            NetworkPlayer builder = builders[i];
            if (builder == null || !builder.IsSpawned || builder.IsDead
                || Vector3.Distance(builder.Position, transform.position) > buildRange)
            {
                builders.RemoveAt(i);
            }
        }
    }

    private Package FindPart(RaftPartType type)
    {
        Bounds zone = partsZone.bounds;
        foreach (Package package in Package.All)
        {
            if (package.Data != null && package.Data.raftPart == type && !package.IsHeld
                && zone.Contains(package.transform.position))
            {
                return package;
            }
        }
        return null;
    }

    private void Finish(Package floaters, Package platform, Package lashing)
    {
        floaters.ServerDespawn();
        platform.ServerDespawn();
        lashing.ServerDespawn();

        GameObject raft = Instantiate(GameNetworkConfig.Instance.ItemPrefabs.raft, launchPoint.position, launchPoint.rotation);
        ShiftSpawner.Instance.ServerSpawnAndTrack(raft.GetComponent<NetworkObject>());

        builders.Clear();
        progress = 0f;
        CampaignManager.Instance.ServerMessage("The raft is ready! Stand on it and paddle (right mouse, best with a plank).");
    }

    private void Tell(string text)
    {
        foreach (NetworkPlayer builder in builders)
        {
            CampaignManager.Instance.ServerMessage(text, builder.OwnerClientId);
        }
    }
}
