using UnityEngine;

// A marked door/zone. Drop a package inside it (let go of it) to deliver.
// Needs a trigger Collider. The address text must match the package's destination.
[RequireComponent(typeof(Collider))]
public class DeliveryPoint : MonoBehaviour
{
    [SerializeField] private string address = "Address";

    public string Address => address;

    private DeliveryManager manager;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        manager = FindFirstObjectByType<DeliveryManager>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (manager == null)
        {
            return;
        }

        Package package = other.GetComponentInParent<Package>();
        if (package == null || package.IsHeld || package.IsDelivered)
        {
            return;
        }

        manager.TryDeliver(package, this);
    }
}
