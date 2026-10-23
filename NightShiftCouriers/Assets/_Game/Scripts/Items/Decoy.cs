using Unity.Netcode;
using UnityEngine;

// A thrown firecracker: after a short fuse it bangs a few times, making loud noise away from you.
[RequireComponent(typeof(Rigidbody))]
public class Decoy : NetworkBehaviour
{
    [SerializeField] private float fuseSeconds = 2f;
    [SerializeField] private int bangs = 4;
    [SerializeField] private float secondsBetweenBangs = 0.8f;
    [SerializeField, Tooltip("Noise loudness of each bang.")]
    private float loudness = 7f;

    private float timer;
    private int bangsLeft;

    public override void OnNetworkSpawn()
    {
        timer = fuseSeconds;
        bangsLeft = bangs;
        if (!IsServer)
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        if (bangsLeft <= 0)
        {
            NetworkObject.Despawn(true);
            return;
        }

        bangsLeft--;
        timer = secondsBetweenBangs;
        bool inWater = WaterVolume.GetDepthAt(transform.position, transform.position.y - 0.5f) > 0.5f;
        GameEvents.RaiseNoise(transform.position, loudness, inWater);
        BangClientRpc();
    }

    [ClientRpc]
    private void BangClientRpc()
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Bang, transform.position, 1f, 50f);
    }
}
