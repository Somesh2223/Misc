using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// The Mud Crawler, a drowned thing that lives in the storm drains (plan M5).
// It moves fast under the city between drain grates, and grabs anyone who stands still on or next to one,
// holding them down while it drowns them in mud. A teammate can pull them free (E).
// Tells: gurgling under the street, bubbles, and the grate rattling for a moment before it grabs.
// Counters: keep moving near grates, never wait on one, and stay close enough to free a teammate.
public class MudCrawler : MonsterBase
{
    private const int NoGrate = -1;

    [Header("Grabbing")]
    [SerializeField, Tooltip("A player this close to the grate it is under can be grabbed.")]
    private float grabRadius = 2.2f;
    [SerializeField, Tooltip("A player must stand still this long before it grabs.")]
    private float stillSecondsNeeded = 1.5f;
    [SerializeField, Tooltip("Moving less than this (metres) counts as standing still.")]
    private float stillTolerance = 0.15f;
    [SerializeField, Tooltip("Damage per second while holding someone.")]
    private float holdDamagePerSecond = 8f;
    [SerializeField, Tooltip("It lets go after this long.")]
    private float maxHoldSeconds = 5f;
    [SerializeField, Tooltip("A teammate must be this close to pull the victim free.")]
    private float freeRange = 2.5f;

    [Header("Moving")]
    [SerializeField, Tooltip("It follows players who are within this distance of any grate.")]
    private float trackRange = 8f;
    [SerializeField, Tooltip("How far below the street it travels.")]
    private float tunnelDepth = 1.5f;
    [SerializeField, Tooltip("After an attack it retreats to a grate at least this far away.")]
    private float retreatDistance = 15f;

    [Header("Look")]
    [SerializeField] private Material visualMaterial;

    private readonly NetworkVariable<int> currentGrate = new NetworkVariable<int>(
        NoGrate, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> emerged = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static readonly List<MudCrawler> allCrawlers = new List<MudCrawler>();

    // Server
    private DrainGrate destination;
    private NetworkPlayer victim;
    private float holdTimer;
    private float holdDamageTimer;
    private readonly Dictionary<NetworkPlayer, Vector3> stillSpot = new Dictionary<NetworkPlayer, Vector3>();
    private readonly Dictionary<NetworkPlayer, float> stillSince = new Dictionary<NetworkPlayer, float>();

    // Every machine
    private Transform body;
    private AudioSource gurgleSource;
    private Vector3 lastPosition;
    private MonsterState shownState;
    private float bubbleTimer;

    public override void OnNetworkSpawn()
    {
        allCrawlers.Add(this);
        BuildBody();
        gurgleSource = ProceduralSounds.AddLoop(gameObject, ProceduralSounds.Gurgle, 0f, 18f);
        lastPosition = transform.position;

        if (IsServer)
        {
            DrainGrate start = DrainGrate.Nearest(transform.position);
            if (start != null)
            {
                transform.position = UnderGrate(start);
                currentGrate.Value = start.GrateId;
            }
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        allCrawlers.Remove(this);
        if (IsServer && victim != null && victim.IsSpawned)
        {
            victim.ServerSetHeld(false, Vector3.zero); // never leave someone stuck
        }
        victim = null;
        base.OnNetworkDespawn();
    }

    // ---------- MonsterBase rules ----------

    protected override bool IsActive()
    {
        return DrainGrate.All.Count > 0;
    }

    // Players standing near grates are the easiest prey.
    protected override float TargetScore(NetworkPlayer candidate)
    {
        DrainGrate grate = DrainGrate.Nearest(candidate.Position);
        float toGrate = grate != null ? HorizontalDistance(grate.Surface, candidate.Position) : 1000f;
        return toGrate * 3f + HorizontalDistance(transform.position, candidate.Position) * 0.2f;
    }

    protected override bool CanHearNoise(Vector3 position, float loudness, bool fromWater)
    {
        return true; // sound carries well down the drains
    }

    protected override bool CanSeeTarget()
    {
        return CanGrab(target);
    }

    protected override bool CanTrackTarget()
    {
        DrainGrate grate = DrainGrate.Nearest(target.Position);
        return grate != null && HorizontalDistance(grate.Surface, target.Position) <= trackRange;
    }

    protected override bool CanAttackTarget()
    {
        return CanGrab(target);
    }

    protected override void PerformAttack()
    {
        DrainGrate grate = DrainGrate.FindById(currentGrate.Value);
        if (grate == null)
        {
            return;
        }
        victim = target;
        holdTimer = maxHoldSeconds;
        holdDamageTimer = 0f;
        victim.ServerSetHeld(true, grate.Surface);
        victim.Health.TakeDamage(data.attackDamage);
        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.ServerUnlockLore(data.loreEntry);
        }
        emerged.Value = true;
        GrabClientRpc(grate.Surface);
    }

    protected override Vector3 GetWanderPoint()
    {
        List<DrainGrate> choices = new List<DrainGrate>();
        DrainGrate here = DrainGrate.FindById(currentGrate.Value);
        Vector3 from = here != null ? here.Surface : transform.position;

        if (State == MonsterState.Cooldown)
        {
            // Get away from where it just attacked.
            foreach (DrainGrate grate in DrainGrate.All)
            {
                if (HorizontalDistance(grate.Surface, from) >= retreatDistance)
                {
                    choices.Add(grate);
                }
            }
        }
        else
        {
            // Drift toward grates near the crew.
            foreach (DrainGrate grate in DrainGrate.All)
            {
                if (grate != here && IsNearAnyPlayer(grate.Surface, 25f))
                {
                    choices.Add(grate);
                }
            }
        }

        if (choices.Count == 0)
        {
            foreach (DrainGrate grate in DrainGrate.All)
            {
                if (grate != here)
                {
                    choices.Add(grate);
                }
            }
        }
        if (choices.Count == 0)
        {
            return from;
        }
        return choices[Random.Range(0, choices.Count)].Surface;
    }

    protected override void MoveToward(Vector3 point, float speed)
    {
        DrainGrate goal = DrainGrate.Nearest(point);
        if (goal == null)
        {
            return;
        }
        if (goal.GrateId == currentGrate.Value && destination == null)
        {
            return; // already here
        }

        destination = goal;
        currentGrate.Value = NoGrate; // travelling
        Vector3 wanted = UnderGrate(goal);
        transform.position = Vector3.MoveTowards(transform.position, wanted, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, wanted) < 0.05f)
        {
            currentGrate.Value = goal.GrateId;
            destination = null;
        }
    }

    protected override bool HasReached(Vector3 point)
    {
        DrainGrate goal = DrainGrate.Nearest(point);
        return goal != null && destination == null && currentGrate.Value == goal.GrateId;
    }

    // ---------- Per frame ----------

    protected override void Update()
    {
        if (IsSpawned && IsServer)
        {
            TrackStillness();
            if (victim != null)
            {
                UpdateHold();
                UpdateTells();
                return; // busy holding someone down
            }
        }

        base.Update();
        if (IsSpawned)
        {
            UpdateTells();
        }
    }

    private bool CanGrab(NetworkPlayer player)
    {
        if (player == null || player.IsDead || player.IsHeldDown || currentGrate.Value == NoGrate || SaltWard.Contains(player.Position))
        {
            return false;
        }
        DrainGrate grate = DrainGrate.FindById(currentGrate.Value);
        if (grate == null || HorizontalDistance(grate.Surface, player.Position) > grabRadius)
        {
            return false;
        }
        if (Mathf.Abs(player.Position.y - grate.Surface.y) > 1.5f)
        {
            return false; // on a balcony above, or on a raft
        }
        return StillSeconds(player) >= stillSecondsNeeded;
    }

    private void TrackStillness()
    {
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player == null)
            {
                continue;
            }
            if (!stillSpot.TryGetValue(player, out Vector3 spot) || Vector3.Distance(spot, player.Position) > stillTolerance)
            {
                stillSpot[player] = player.Position;
                stillSince[player] = Time.time;
            }
        }
    }

    private float StillSeconds(NetworkPlayer player)
    {
        return stillSince.TryGetValue(player, out float since) ? Time.time - since : 0f;
    }

    private void UpdateHold()
    {
        holdTimer -= Time.deltaTime;
        if (!victim.IsSpawned || victim.IsDead || holdTimer <= 0f)
        {
            Release();
            return;
        }

        holdDamageTimer -= Time.deltaTime;
        if (holdDamageTimer <= 0f)
        {
            holdDamageTimer = 1f;
            victim.Health.TakeDamage(holdDamagePerSecond);
            GameEvents.RaiseNoise(victim.Position, 3f, false); // thrashing and choking
        }
    }

    private void Release()
    {
        if (victim != null && victim.IsSpawned)
        {
            victim.ServerSetHeld(false, Vector3.zero);
        }
        bool wasHolding = victim != null;
        victim = null;
        emerged.Value = false;
        if (wasHolding && IsSpawned)
        {
            ChangeState(MonsterState.Cooldown);
        }
    }

    // A teammate pressed E on a player who is being held (server).
    public static bool ServerTryFree(NetworkPlayer held, NetworkPlayer helper)
    {
        foreach (MudCrawler crawler in allCrawlers)
        {
            if (crawler.victim != held)
            {
                continue;
            }
            if (helper == held || helper.IsDead || Vector3.Distance(helper.Position, held.Position) > crawler.freeRange)
            {
                return false;
            }
            crawler.Release();
            crawler.FreedClientRpc(held.Position);
            return true;
        }
        // Nobody is holding them any more (e.g. the crawler despawned): clear it anyway.
        held.ServerSetHeld(false, Vector3.zero);
        return true;
    }

    private Vector3 UnderGrate(DrainGrate grate)
    {
        return grate.Surface + Vector3.down * tunnelDepth;
    }

    private static bool IsNearAnyPlayer(Vector3 point, float distance)
    {
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead && HorizontalDistance(point, player.Position) <= distance)
            {
                return true;
            }
        }
        return false;
    }

    // ---------- Tells (every machine) ----------

    private void UpdateTells()
    {
        Vector3 moved = transform.position - lastPosition;
        bool moving = moved.magnitude > 0.5f * Time.deltaTime;
        lastPosition = transform.position;

        MonsterState state = State;
        bool hunting = state == MonsterState.Hunt || state == MonsterState.Investigate;
        float wanted = (moving ? (hunting ? 0.9f : 0.5f) : 0.15f) * GameSettings.SfxVolume;
        if (emerged.Value)
        {
            wanted = 1f * GameSettings.SfxVolume;
        }
        gurgleSource.volume = Mathf.MoveTowards(gurgleSource.volume, wanted, Time.deltaTime * 2f);

        // The rattle: the moment it starts winding up a grab.
        if (state != shownState)
        {
            shownState = state;
            if (state == MonsterState.Attack)
            {
                DrainGrate grate = DrainGrate.FindById(currentGrate.Value);
                if (grate != null)
                {
                    grate.Rattle(data.attackWindup);
                }
            }
        }

        // Bubbles at the grate it waits under.
        bubbleTimer -= Time.deltaTime;
        if (bubbleTimer <= 0f)
        {
            bubbleTimer = Random.Range(1.5f, 4f);
            DrainGrate here = DrainGrate.FindById(currentGrate.Value);
            if (here != null)
            {
                here.Bubble(hunting ? 3 : 1);
            }
        }

        // Only its arms and head show, and only while it holds someone.
        if (body != null)
        {
            bool show = emerged.Value;
            if (body.gameObject.activeSelf != show)
            {
                body.gameObject.SetActive(show);
            }
            if (show)
            {
                body.localPosition = new Vector3(0f, tunnelDepth - 0.6f + Mathf.Sin(Time.time * 6f) * 0.05f, 0f);
            }
        }
    }

    [ClientRpc]
    private void GrabClientRpc(Vector3 grateSurface)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Gurgle, grateSurface, 1f, 30f);
        ProceduralSounds.PlayAt(ProceduralSounds.Scream, grateSurface + Vector3.up, 0.7f, 35f);
        HighlightMoments.Flag("Mud Crawler grab", 75, grateSurface);
    }

    [ClientRpc]
    private void FreedClientRpc(Vector3 where)
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Splash, where, 0.8f, 20f);
        MessageFeed.Show("Pulled free of the drain!", 2.5f);
        HighlightMoments.Flag("Saved from the drain", 65, where);
    }

    // ---------- Look ----------

    private void BuildBody()
    {
        Color mud = new Color(0.22f, 0.17f, 0.1f);
        Color bone = new Color(0.6f, 0.58f, 0.5f);

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.4f, 0.5f), mud);           // head
        MakePart(PrimitiveType.Sphere, new Vector3(-0.1f, 0.2f, 0.2f), new Vector3(0.07f, 0.07f, 0.04f), bone);  // eyes
        MakePart(PrimitiveType.Sphere, new Vector3(0.1f, 0.2f, 0.2f), new Vector3(0.07f, 0.07f, 0.04f), bone);
        MakePart(PrimitiveType.Capsule, new Vector3(-0.4f, 0.3f, 0.25f), new Vector3(0.12f, 0.45f, 0.12f), mud);  // reaching arms
        MakePart(PrimitiveType.Capsule, new Vector3(0.4f, 0.3f, 0.25f), new Vector3(0.12f, 0.45f, 0.12f), mud);
        body.gameObject.SetActive(false);
    }

    private void MakePart(PrimitiveType shape, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(body, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = MakeMaterial(color);
    }

    private Material MakeMaterial(Color color)
    {
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
        return material;
    }
}
