using UnityEngine;

// Tweakable monster stats (plan rule 4). Create via Assets > Create > Night Shift > Monster Data.
[CreateAssetMenu(fileName = "NewMonster", menuName = "Night Shift/Monster Data")]
public class MonsterData : ScriptableObject
{
    public string displayName = "Monster";

    [Header("Movement (m/s)")]
    public float wanderSpeed = 1.5f;
    public float investigateSpeed = 3f;
    public float huntSpeed = 4.5f;

    [Header("Senses")]
    [Tooltip("Hearing radius in metres per 1.0 of noise loudness. A loudness 2 splash is heard at 2x this.")]
    public float hearingRadiusPerLoudness = 8f;
    [Tooltip("A noise this fraction of the hearing radius away or closer sends the monster straight into a hunt.")]
    [Range(0.1f, 1f)] public float huntNoiseFraction = 0.4f;
    [Tooltip("How far the monster can see a target (metres).")]
    public float sightRange = 14f;
    [Tooltip("The hunt ends if the target is further away than this.")]
    public float forgetDistance = 30f;
    [Tooltip("Seconds the monster keeps hunting after it loses its target.")]
    public float loseTargetSeconds = 4f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    [Tooltip("Seconds between starting an attack and the hit landing (the player's chance to react).")]
    public float attackWindup = 0.7f;
    public float attackDamage = 35f;
    [Tooltip("Seconds the monster retreats after attacking.")]
    public float cooldownSeconds = 4f;

    [Header("Idle / Investigate")]
    public float idleSecondsMin = 2f;
    public float idleSecondsMax = 5f;
    [Tooltip("Seconds the monster searches around a noise before giving up.")]
    public float searchSeconds = 5f;
}
