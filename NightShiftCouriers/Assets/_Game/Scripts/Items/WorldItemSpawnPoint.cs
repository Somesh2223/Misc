using UnityEngine;

// Marks where the server places an item pickup when the shift starts (e.g. a rare lashing rope).
public class WorldItemSpawnPoint : MonoBehaviour
{
    public ItemData item;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
}
