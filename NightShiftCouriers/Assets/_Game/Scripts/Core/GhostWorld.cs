using Unity.Netcode;
using UnityEngine;

// Sends Signal Ghost effects to every player: flickering streetlights and the ghost bell (plan 4.10).
// Spawned once by the server when hosting starts.
public class GhostWorld : NetworkBehaviour
{
    public static GhostWorld Instance { get; private set; }

    [SerializeField] private float flickerSeconds = 4f;

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

    public void ServerFlicker(int lightId)
    {
        FlickerClientRpc(lightId);
    }

    public void ServerRingBell(Vector3 position)
    {
        BellClientRpc(position);
    }

    [ClientRpc]
    private void FlickerClientRpc(int lightId)
    {
        Streetlight light = Streetlight.FindById(lightId);
        if (light != null)
        {
            light.Flicker(flickerSeconds);
        }
    }

    [ClientRpc]
    private void BellClientRpc(Vector3 position)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Bell, position, 1f, 45f);
    }
}
