using Unity.Netcode;
using UnityEngine;

// A dangerous spot (a fallen live wire in a puddle...): hurts players standing in it. Server only.
[RequireComponent(typeof(BoxCollider))]
public class HazardZone : MonoBehaviour
{
    [SerializeField] private float damagePerSecond = 40f;

    private BoxCollider zone;

    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
    }

    private void Update()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsServer)
        {
            return;
        }

        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (!player.IsDead && zone.bounds.Contains(player.Position + Vector3.up * 0.5f))
            {
                player.Health.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}
