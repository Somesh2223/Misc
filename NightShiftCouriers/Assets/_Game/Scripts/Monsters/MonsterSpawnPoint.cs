using UnityEngine;

// Marks where the server spawns a Jal-Pishach, and which water it lives in.
public class MonsterSpawnPoint : MonoBehaviour
{
    public WaterVolume homeWater;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
