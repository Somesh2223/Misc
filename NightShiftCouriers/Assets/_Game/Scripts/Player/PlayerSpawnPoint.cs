using System.Collections.Generic;
using UnityEngine;

// Where players start (and respawn). Players are assigned spawn points in join order.
public class PlayerSpawnPoint : MonoBehaviour
{
    private static readonly List<PlayerSpawnPoint> all = new List<PlayerSpawnPoint>();

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // Returns false if the scene has no spawn points.
    public static bool TryGet(int index, out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;
        if (all.Count == 0)
        {
            return false;
        }

        PlayerSpawnPoint point = all[index % all.Count];
        position = point.transform.position;
        yaw = point.transform.eulerAngles.y;
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.5f);
    }
}
