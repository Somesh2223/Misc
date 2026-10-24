using UnityEngine;

// Marks where the server spawns a monster.
// Jal-Pishach: also set the water it lives in. Vetala: place it where it hangs (a branch, wire or balcony).
// Brahmarakshas: place it inside a HauntedHouse. Mud Crawler: anywhere near drain grates.
// Director Only points are left empty at the start of the shift; the AI Director fills them later (plan 4.6).
public class MonsterSpawnPoint : MonoBehaviour
{
    public MonsterKind kind = MonsterKind.JalPishach;
    [Tooltip("Jal-Pishach only: the water it lives in.")]
    public WaterVolume homeWater;
    [Tooltip("Skipped when the shift starts. The AI Director spawns a monster here when the night needs one.")]
    public bool directorOnly = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = directorOnly ? new Color(1f, 0.5f, 0f) : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
