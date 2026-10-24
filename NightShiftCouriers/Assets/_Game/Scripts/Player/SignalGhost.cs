using Unity.Netcode;
using UnityEngine;

// What a dead player can do as a Signal Ghost (plan 4.10). Living players cannot tell if a ghost is helping or trolling.
//   E            - possess the streetlight you look at: it flickers
//   Right mouse  - ring a bell where you look: monsters hear it (long cooldown)
//   Radio        - ghosts talk on the radio channel (see PlayerVoice)
//   Monsters are shown on screen (WorldMarkersHUD)
[RequireComponent(typeof(NetworkPlayer))]
public class SignalGhost : NetworkBehaviour
{
    [SerializeField] private float flickerCooldown = 6f;
    [SerializeField] private float bellCooldown = 30f;
    [SerializeField, Tooltip("Noise loudness of the ghost bell.")]
    private float bellLoudness = 6f;
    [SerializeField] private float reach = 60f;

    private NetworkPlayer player;
    private PlayerInputReader input;
    private float localFlickerReady;
    private float localBellReady;
    private double serverFlickerReady;
    private double serverBellReady;

    private void Awake()
    {
        player = GetComponent<NetworkPlayer>();
        input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || !player.IsDead || UIState.InputBlocked)
        {
            return;
        }

        if (input.InteractPressedThisFrame && Time.time >= localFlickerReady)
        {
            localFlickerReady = Time.time + flickerCooldown;
            FlickerServerRpc(player.AimOrigin, player.AimDirection);
        }
        if (input.UsePressedThisFrame && Time.time >= localBellReady)
        {
            localBellReady = Time.time + bellCooldown;
            RingBellServerRpc(player.AimOrigin, player.AimDirection);
        }
    }

    // Seconds until the bell can ring again (for the ghost's HUD).
    public float BellCooldownLeft => Mathf.Max(0f, localBellReady - Time.time);
    public float FlickerCooldownLeft => Mathf.Max(0f, localFlickerReady - Time.time);

    [ServerRpc]
    private void FlickerServerRpc(Vector3 origin, Vector3 direction)
    {
        double now = NetworkManager.ServerTime.Time;
        if (!player.IsDead || now < serverFlickerReady || GhostWorld.Instance == null)
        {
            return;
        }

        Streetlight light = Streetlight.FindNearestToRay(origin, direction.normalized, reach, 6f);
        if (light != null)
        {
            serverFlickerReady = now + flickerCooldown;
            GhostWorld.Instance.ServerFlicker(light.LightId);
        }
    }

    [ServerRpc]
    private void RingBellServerRpc(Vector3 origin, Vector3 direction)
    {
        double now = NetworkManager.ServerTime.Time;
        if (!player.IsDead || now < serverBellReady || GhostWorld.Instance == null)
        {
            return;
        }
        serverBellReady = now + bellCooldown;

        Vector3 point = origin + direction.normalized * 20f;
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, reach, ~(1 << 2), QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
        }

        bool inWater = WaterVolume.GetDepthAt(point, point.y - 0.5f) > 0.3f;
        GameEvents.RaiseNoise(point, bellLoudness, inWater);
        GhostWorld.Instance.ServerRingBell(point);
    }
}
