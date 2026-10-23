using UnityEngine;

// Tweakable package stats (plan rule 4). Create via Assets > Create > Night Shift > Package Data.
[CreateAssetMenu(fileName = "NewPackage", menuName = "Night Shift/Package Data")]
public class PackageData : ScriptableObject
{
    [Header("General")]
    public string displayName = "Package";
    [Tooltip("Money paid for a perfect delivery.")]
    public int baseValue = 100;
    [Tooltip("Rigidbody mass in kg. Heavy packages sink, light ones float.")]
    public float mass = 3f;

    [Header("Fragile")]
    public bool isFragile = false;
    [Tooltip("Hit points. At 0 the package is ruined.")]
    public float maxHealth = 100f;
    [Tooltip("Impacts slower than this (m/s) do no damage.")]
    public float safeImpactSpeed = 3f;
    [Tooltip("Damage per m/s of impact speed above the safe speed.")]
    public float damagePerSpeed = 10f;

    [Header("Water")]
    [Tooltip("If true, being under water ruins the payout over time.")]
    public bool wetSensitive = false;
    [Tooltip("Seconds fully submerged before a wet-sensitive package is worthless.")]
    public float secondsToRuin = 10f;
}
