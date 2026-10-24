using UnityEngine;

// Tuning for the AI Director (plan M5). Create via Assets > Create > Night Shift > Director Settings.
// The director watches how stressed the crew is and decides when to send more monsters and when to back off.
[CreateAssetMenu(fileName = "DirectorSettings", menuName = "Night Shift/Director Settings")]
public class DirectorSettings : ScriptableObject
{
    [Header("Budget (how many extra monsters at once)")]
    [Tooltip("Monster points the director can spend at the start of the night.")]
    public int baseBudget = 1;
    [Tooltip("Extra points per flood rise: the night gets worse as the water comes up.")]
    public int budgetPerFloodStage = 1;
    [Tooltip("Never more than this many director monsters at once.")]
    public int maxDirectorMonsters = 4;

    [Header("Monster costs (0 = the director never sends it)")]
    public int chudailCost = 2;
    public int vetalaCost = 1;
    public int mudCrawlerCost = 1;
    public int jalPishachCost = 2;
    [Tooltip("The Brahmarakshas belongs to its house; the director normally never sends it.")]
    public int brahmarakshasCost = 0;

    [Header("Timing")]
    [Tooltip("Seconds between spawn decisions.")]
    public float spawnInterval = 30f;
    [Tooltip("No director spawns for this many seconds after the shift starts.")]
    public float graceSeconds = 45f;
    [Tooltip("Seconds between checks (stress, scares).")]
    public float checkInterval = 1f;

    [Header("Stress (0-100)")]
    [Tooltip("Stress per point of damage the crew takes.")]
    public float stressPerDamage = 1f;
    [Tooltip("Stress per point of loudness for noises near the crew.")]
    public float stressPerLoudness = 0.5f;
    [Tooltip("Noises within this distance of a courier count.")]
    public float noiseRadius = 15f;
    [Tooltip("Stress per second while any monster is hunting.")]
    public float stressPerHuntSecond = 5f;
    [Tooltip("Stress lost per second when nothing is happening.")]
    public float stressDecayPerSecond = 4f;
    [Tooltip("Stress needed to reach the Peak phase.")]
    public float peakStress = 70f;
    [Tooltip("Seconds of Relax after a peak (no spawns, fewer scares).")]
    public float relaxSeconds = 30f;

    [Header("Ambient scares")]
    [Tooltip("If nothing has happened for this long, play a scare sound near someone.")]
    public float quietSecondsBeforeScare = 60f;
    [Tooltip("Scares play this far from the chosen courier (metres).")]
    public Vector2 scareDistance = new Vector2(12f, 20f);

    [Header("Spawning out of sight")]
    [Tooltip("A director spawn point must be at least this far from every courier.")]
    public float minSpawnDistance = 25f;
    [Tooltip("...and not inside any courier's view (half-angle of the view cone, degrees) with a clear line to it.")]
    public float viewConeHalfAngle = 70f;
}
