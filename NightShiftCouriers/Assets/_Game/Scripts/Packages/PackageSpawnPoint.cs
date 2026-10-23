using UnityEngine;

// Marks where the server spawns a package when the shift starts.
public class PackageSpawnPoint : MonoBehaviour
{
    public PackageData data;
    [Tooltip("Address it must be delivered to (matches a DeliveryPoint).")]
    public string destination = "";
    [Tooltip("Always spawn this package cursed (plan 4.9). Otherwise addressed packages are cursed by chance.")]
    public bool alwaysCursed = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.4f, 0.5f));
    }
}
