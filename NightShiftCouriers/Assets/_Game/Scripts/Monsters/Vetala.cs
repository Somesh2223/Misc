using Unity.Netcode;
using UnityEngine;

// The Vetala, a hanging corpse spirit (plan section 4.6). Hangs upside down from trees, wires and balconies.
// - Walk under it with a light on and it drops on you.
// - It can latch onto a package someone carries nearby: the package gets heavier, loses value,
//   and screams now and then (the noise draws other monsters).
// Tells: swaying silhouettes, packages suddenly heavier, laughter from above.
// Counters: lights off near trees and wires; pass a possessed package to a teammate; the lemon-chilli charm.
public class Vetala : MonsterBase
{
    private const ulong NoPackage = ulong.MaxValue;

    [Header("Dropping")]
    [SerializeField, Tooltip("It drops on lit players whose feet are within this distance of the spot under it.")]
    private float dropRadius = 3f;
    [SerializeField, Tooltip("How far it can reach once dropped (metres).")]
    private float grabReach = 1.6f;

    [Header("Possession")]
    [SerializeField, Tooltip("Carried packages within this distance (metres, sideways) can be possessed.")]
    private float latchRadius = 6f;
    [SerializeField, Range(0f, 1f), Tooltip("Chance per second to latch onto a package carried nearby.")]
    private float latchChancePerSecond = 0.2f;
    [SerializeField] private float screamInterval = 5f;
    [SerializeField, Tooltip("Noise of a possessed package's scream.")]
    private float screamLoudness = 5f;

    [Header("Tells")]
    [SerializeField, Tooltip("Seconds between random laughs when someone is near.")]
    private float laughIntervalMin = 20f;
    [SerializeField] private float laughIntervalMax = 40f;
    [SerializeField] private Material visualMaterial;

    // The package it is possessing (everyone can see this, e.g. to hide the body while latched).
    private readonly NetworkVariable<ulong> latchedId = new NetworkVariable<ulong>(
        NoPackage, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 perch;
    private Transform swayPivot;
    private Renderer[] renderers;

    // Server
    private Package latched;
    private ulong victim;
    private float latchCheckTimer;
    private float screamTimer;
    private float laughTimer;

    public override void OnNetworkSpawn()
    {
        perch = transform.position;
        BuildBody();
        laughTimer = Random.Range(laughIntervalMin, laughIntervalMax);
        base.OnNetworkSpawn();
    }

    // ---------- MonsterBase rules ----------

    protected override bool IsActive()
    {
        return latchedId.Value == NoPackage; // while possessing a package it is "inside" it
    }

    protected override bool CanHearNoise(Vector3 position, float loudness, bool fromWater)
    {
        return false; // it waits; it does not chase sounds
    }

    protected override bool CanSeeTarget()
    {
        return IsLitAndBelow(target, dropRadius);
    }

    protected override bool CanTrackTarget()
    {
        return IsLitAndBelow(target, dropRadius * 2f);
    }

    protected override bool CanAttackTarget()
    {
        return !target.IsDead && Vector3.Distance(transform.position, target.Position + Vector3.up * 1.2f) <= grabReach;
    }

    protected override void PerformAttack()
    {
        target.ServerAddImpulse(Vector3.down * 3f);
        target.Health.TakeDamage(data.attackDamage);
        LaughClientRpc();
    }

    protected override Vector3 GetWanderPoint()
    {
        return perch; // it always goes back to where it hangs
    }

    protected override void MoveToward(Vector3 point, float speed)
    {
        // During a hunt it drops toward the victim's head height; otherwise it climbs back up.
        if (State == MonsterState.Hunt && target != null)
        {
            point = target.Position + Vector3.up * 1.2f;
        }
        transform.position = Vector3.MoveTowards(transform.position, point, speed * Time.deltaTime);
    }

    protected override bool HasReached(Vector3 point)
    {
        return Vector3.Distance(transform.position, point) < 0.3f;
    }

    private bool IsLitAndBelow(NetworkPlayer player, float radius)
    {
        bool lit = player.Inventory != null && player.Inventory.FlashlightOn;
        bool below = player.Position.y < perch.y - 1f;
        return lit && below && HorizontalDistance(perch, player.Position) <= radius;
    }

    // ---------- Per frame ----------

    protected override void Update()
    {
        base.Update();
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            if (latched == null)
            {
                TryLatch();
            }
            else
            {
                UpdatePossession();
            }
            UpdateLaughter();
        }

        // Swaying silhouette; hidden while it is inside a package.
        if (swayPivot != null)
        {
            swayPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f) * 12f);
        }
        bool visible = latchedId.Value == NoPackage;
        foreach (Renderer part in renderers)
        {
            part.enabled = visible;
        }
    }

    // ---------- Possession (server) ----------

    private void TryLatch()
    {
        latchCheckTimer -= Time.deltaTime;
        if (latchCheckTimer > 0f || (State != MonsterState.Idle && State != MonsterState.Wander))
        {
            return;
        }
        latchCheckTimer = 1f;

        foreach (Package package in Package.All)
        {
            if (!package.IsHeld || package.IsPossessed || package.IsDelivered
                || HorizontalDistance(perch, package.transform.position) > latchRadius)
            {
                continue;
            }

            PlayerGrabber holder = package.ServerFirstHolder();
            NetworkPlayer carrier = holder != null ? holder.GetComponent<NetworkPlayer>() : null;
            if (carrier == null || carrier.Inventory.IsCharmedAgainst(MonsterKind.Vetala))
            {
                continue;
            }

            if (Random.value < latchChancePerSecond)
            {
                latched = package;
                victim = carrier.OwnerClientId;
                latchedId.Value = package.NetworkObjectId;
                package.ServerSetPossessed(true, victim);
                screamTimer = screamInterval;
                LaughAtClientRpc(package.transform.position);
                CampaignManager.Instance.ServerMessage("The package suddenly feels much heavier...", victim);
                return;
            }
        }
    }

    private void UpdatePossession()
    {
        if (latched == null || !latched.IsSpawned || latched.IsDelivered)
        {
            Release();
            return;
        }

        // Handing the package to a teammate breaks the spell.
        if (latched.ServerIsHeldBySomeoneOtherThan(victim))
        {
            latched.ServerSetPossessed(false, 0);
            CampaignManager.Instance.ServerMessage("Something lets go of the package with a hiss.");
            Release();
            return;
        }

        screamTimer -= Time.deltaTime;
        if (screamTimer <= 0f)
        {
            screamTimer = screamInterval;
            GameEvents.RaiseNoise(latched.transform.position, screamLoudness, false);
            ScreamAtClientRpc(latched.transform.position);
        }
    }

    private void Release()
    {
        latched = null;
        latchedId.Value = NoPackage;
        transform.position = perch;
        ChangeState(MonsterState.Cooldown);
    }

    private void UpdateLaughter()
    {
        laughTimer -= Time.deltaTime;
        if (laughTimer > 0f)
        {
            return;
        }
        laughTimer = Random.Range(laughIntervalMin, laughIntervalMax);

        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && Vector3.Distance(player.Position, perch) < 15f)
            {
                LaughClientRpc();
                return;
            }
        }
    }

    [ClientRpc]
    private void LaughClientRpc()
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Laugh, transform.position, 0.8f, 25f);
        HighlightMoments.Flag("Vetala laughed", 30, transform.position);
    }

    [ClientRpc]
    private void LaughAtClientRpc(Vector3 position)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Laugh, position, 0.8f, 25f);
        HighlightMoments.Flag("Possessed package!", 70, position);
    }

    [ClientRpc]
    private void ScreamAtClientRpc(Vector3 position)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Scream, position, 0.9f, 40f);
    }

    // ---------- Look: an upside-down figure on a rope ----------

    private void BuildBody()
    {
        GameObject pivot = new GameObject("Sway");
        pivot.transform.SetParent(transform, false);
        swayPivot = pivot.transform;

        Color rope = new Color(0.3f, 0.25f, 0.2f);
        Color skin = new Color(0.75f, 0.75f, 0.7f);
        Color hair = new Color(0.05f, 0.05f, 0.05f);

        MakePart(PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.04f, 0.5f, 0.04f), rope);
        MakePart(PrimitiveType.Capsule, new Vector3(0f, -0.8f, 0f), new Vector3(0.45f, 0.8f, 0.35f), skin);   // body, head down
        MakePart(PrimitiveType.Sphere, new Vector3(0f, -1.75f, 0f), new Vector3(0.3f, 0.3f, 0.3f), skin);    // head
        MakePart(PrimitiveType.Cube, new Vector3(0f, -2.2f, 0f), new Vector3(0.3f, 0.7f, 0.1f), hair);       // hair hanging down
        renderers = pivot.GetComponentsInChildren<Renderer>();
    }

    private void MakePart(PrimitiveType shape, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(swayPivot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;

        Material material;
        if (visualMaterial != null)
        {
            material = new Material(visualMaterial);
        }
        else
        {
            bool usingUrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = usingUrp ? Shader.Find("Universal Render Pipeline/Lit") : null;
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
        }
        material.color = color;
        part.GetComponent<Renderer>().material = material;
    }
}
