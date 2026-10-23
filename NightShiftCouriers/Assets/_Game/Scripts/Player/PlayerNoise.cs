using UnityEngine;

// Turns what the player is doing into noise events that monsters can hear (section 4.6).
// Wading and swimming are "water noise"; crouch-wading is quiet.
[RequireComponent(typeof(PlayerController))]
public class PlayerNoise : MonoBehaviour
{
    [SerializeField, Tooltip("Seconds between noise events while moving.")]
    private float noiseInterval = 0.4f;
    [SerializeField, Tooltip("Loudness of normal walking on dry ground.")]
    private float walkLoudness = 1f;
    [SerializeField, Tooltip("Loudness multiplier for sprinting.")]
    private float sprintMultiplier = 2.5f;
    [SerializeField, Tooltip("Loudness multiplier for crouching.")]
    private float crouchMultiplier = 0.25f;
    [SerializeField, Tooltip("Loudness multiplier for splashing through water (before crouch).")]
    private float splashMultiplier = 2f;
    [SerializeField, Tooltip("Loudness of swimming.")]
    private float swimLoudness = 2.5f;

    private PlayerController player;
    private float timer;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }
        timer = noiseInterval;

        if (player.CurrentSpeed < 0.3f && !player.IsSwimming)
        {
            return; // standing still is silent
        }

        bool inWater = player.WaterDepth > 0.3f;
        float loudness = walkLoudness;

        if (player.IsSprinting)
        {
            loudness *= sprintMultiplier;
        }
        if (player.IsCrouching)
        {
            loudness *= crouchMultiplier;
        }
        if (inWater)
        {
            loudness *= splashMultiplier;
        }
        if (player.IsSwimming)
        {
            loudness = swimLoudness;
        }

        GameEvents.RaiseNoise(transform.position, loudness, inWater);
    }
}
