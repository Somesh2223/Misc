using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// A marked door/zone. Drop a package inside it (let go of it) to deliver.
// Needs a trigger Collider. The address text must match the package's destination.
// Only the server decides deliveries.
[RequireComponent(typeof(Collider))]
public class DeliveryPoint : MonoBehaviour
{
    [SerializeField] private string address = "Address";

    private static readonly List<DeliveryPoint> all = new List<DeliveryPoint>();

    public static IReadOnlyList<DeliveryPoint> All => all;
    public string Address => address;

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsServer || DeliveryManager.Instance == null)
        {
            return;
        }

        Package package = other.GetComponentInParent<Package>();
        if (package == null || !package.IsSpawned || package.IsHeld || package.IsDelivered)
        {
            return;
        }

        DeliveryManager.Instance.ServerTryDeliver(package, this);
    }
}
