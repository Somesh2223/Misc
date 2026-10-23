using Unity.Netcode;
using UnityEngine;

// Removes a spawned object after some time (glow sticks, planks...). Server only.
public class TimedDespawn : NetworkBehaviour
{
    [SerializeField] private float lifetimeSeconds = 300f;

    private float timeLeft;

    public override void OnNetworkSpawn()
    {
        timeLeft = lifetimeSeconds;
        if (!IsServer)
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true; // only the server simulates physics
            }
        }
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            NetworkObject.Despawn(true);
        }
    }
}
