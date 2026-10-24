using Unity.Netcode;
using UnityEngine;

// Small bits of shared world state that are not worth their own networked object (plan M5):
//  - which house doors are open or closed (a bitmask, so late joiners at the depot see the right doors)
//  - one-shot sounds the server wants everyone to hear (AI Director scares, door bashing...)
// Spawned once by the server when hosting starts, like GhostWorld.
public class WorldSync : NetworkBehaviour
{
    public static WorldSync Instance { get; private set; }

    // Bit N is set when door N has been flipped from how it starts. Up to 31 doors per scene.
    private readonly NetworkVariable<int> doorsToggled = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---------- Doors ----------

    public bool IsDoorClosed(int doorId, bool startsClosed)
    {
        bool flipped = IsValidDoor(doorId) && (doorsToggled.Value & (1 << doorId)) != 0;
        return startsClosed != flipped;
    }

    public void ServerSetDoorClosed(int doorId, bool closed, bool startsClosed)
    {
        if (!IsServer || !IsValidDoor(doorId))
        {
            return;
        }
        int bit = 1 << doorId;
        bool flipped = closed != startsClosed;
        doorsToggled.Value = flipped ? (doorsToggled.Value | bit) : (doorsToggled.Value & ~bit);
    }

    // Every door back to how it started (the crew is back at the depot).
    public void ServerResetDoors()
    {
        if (IsServer)
        {
            doorsToggled.Value = 0;
        }
    }

    private static bool IsValidDoor(int doorId)
    {
        return doorId >= 0 && doorId < 31;
    }

    // ---------- Sounds ----------

    // Plays a sound at a position on every machine.
    public void ServerPlaySound(WorldSound sound, Vector3 position, float volume = 1f, float maxDistance = 35f)
    {
        if (IsServer)
        {
            PlaySoundClientRpc((byte)sound, position, volume, maxDistance);
        }
    }

    [ClientRpc]
    private void PlaySoundClientRpc(byte sound, Vector3 position, float volume, float maxDistance)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Get((WorldSound)sound), position, volume, maxDistance);
    }
}
