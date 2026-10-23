using UnityEngine;

// Marks where the server spawns a monster when the shift starts.
// Jal-Pishach: also set the water it lives in. Vetala: place it where it hangs (a branch, wire or balcony).
public class MonsterSpawnPoint : MonoBehaviour
{
    public MonsterKind kind = MonsterKind.JalPishach;
    [Tooltip("Jal-Pishach only: the water it lives in.")]
    public WaterVolume homeWater;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
