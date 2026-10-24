using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// The Brahmarakshas, the ghost of a learned man who misused his knowledge (plan M5).
// Lives in one haunted house and never leaves it. Slow but very strong.
// Tells: a low chant you can hear through the walls, candles that light up when it is home and flicker
// when it hunts, and doors that swing shut on their own.
// Counters: close doors behind you (it has to bash them), hold a door shut with two couriers,
// and throw a firecracker decoy to keep it busy while the package goes in.
[RequireComponent(typeof(NavMeshAgent))]
public class Brahmarakshas : MonsterBase
{
    [Header("Doors")]
    [SerializeField, Tooltip("A closed door this close, between it and where it wants to go, gets bashed.")]
    private float doorCheckDistance = 2.2f;
    [SerializeField, Tooltip("Seconds of bashing before the door gives way.")]
    private float bashSeconds = 2.5f;
    [SerializeField, Tooltip("This many couriers at a door hold it shut.")]
    private int holdersNeeded = 2;
    [SerializeField, Tooltip("Seconds between doors it closes by itself (its tell).")]
    private Vector2 closeDoorInterval = new Vector2(20f, 40f);

    [Header("Senses")]
    [SerializeField, Tooltip("Noises this far outside its house still reach it.")]
    private float hearingMargin = 6f;
    [SerializeField, Tooltip("A noise at least this loud (a firecracker) holds its attention.")]
    private float distractLoudness = 6f;
    [SerializeField, Tooltip("Seconds a firecracker keeps it busy.")]
    private float distractSeconds = 12f;
    [SerializeField, Tooltip("While distracted it still notices couriers this close.")]
    private float distractedNoticeRange = 3f;

    [Header("Look")]
    [SerializeField] private Material visualMaterial;

    private NavMeshAgent agent;
    private HauntedHouse house;
    private AudioSource chantSource;

    // Server
    private HouseDoor bashingDoor;
    private float bashTimer;
    private float bangTimer;
    private float distractedUntil;
    private float closeDoorTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        BuildBody();
        chantSource = ProceduralSounds.AddLoop(gameObject, ProceduralSounds.Chant, 0.5f, 30f);

        house = HauntedHouse.FindContaining(transform.position, 2f);
        if (house == null)
        {
            house = HauntedHouse.Nearest(transform.position);
        }

        if (IsServer)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            closeDoorTimer = Random.Range(closeDoorInterval.x, closeDoorInterval.y);
        }
        else
        {
            agent.enabled = false;
        }

        base.OnNetworkSpawn();
    }

    // ---------- MonsterBase rules ----------

    protected override bool IsActive()
    {
        return agent.enabled ? agent.isOnNavMesh : true;
    }

    // Players inside its house come first; everyone outside is "very far away".
    protected override float TargetScore(NetworkPlayer candidate)
    {
        float distance = HorizontalDistance(transform.position, candidate.Position);
        return InHouse(candidate.Position) ? distance : 10000f + distance;
    }

    protected override bool CanHearNoise(Vector3 position, float loudness, bool fromWater)
    {
        if (house != null && !house.Contains(position, hearingMargin))
        {
            return false;
        }
        if (loudness >= distractLoudness)
        {
            distractedUntil = Time.time + distractSeconds;
        }
        return true;
    }

    protected override bool CanSeeTarget()
    {
        if (!InHouse(target.Position))
        {
            return false;
        }
        float distance = HorizontalDistanceToTarget();
        if (IsDistracted && distance > distractedNoticeRange)
        {
            return false;
        }
        return distance <= data.sightRange && HasLineOfSight(target.Position);
    }

    protected override bool CanTrackTarget()
    {
        // It follows anyone still inside; the moment they step out, it loses them.
        return InHouse(target.Position);
    }

    protected override bool CanAttackTarget()
    {
        if (target.IsDead || !InHouse(target.Position) || SaltWard.Contains(target.Position))
        {
            return false;
        }
        return Vector3.Distance(transform.position, target.Position) <= data.attackRange && HasLineOfSight(target.Position);
    }

    protected override void PerformAttack()
    {
        Vector3 push = target.Position - transform.position;
        push.y = 0f;
        target.ServerAddImpulse(push.normalized * 7f + Vector3.up * 3f);
        target.Health.TakeDamage(data.attackDamage);
        if (CampaignManager.Instance != null)
        {
            CampaignManager.Instance.ServerUnlockLore(data.loreEntry);
        }
        RoarClientRpc();
    }

    protected override Vector3 GetWanderPoint()
    {
        Vector3 random = house != null ? house.RandomPointInside() : transform.position + Random.insideUnitSphere * 8f;
        if (NavMesh.SamplePosition(random, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return KeepInside(hit.position);
        }
        return transform.position;
    }

    protected override void MoveToward(Vector3 point, float speed)
    {
        if (!agent.enabled || !agent.isOnNavMesh || bashingDoor != null)
        {
            return;
        }
        point = KeepInside(point);
        agent.speed = speed;
        if ((agent.destination - point).sqrMagnitude > 0.25f)
        {
            agent.SetDestination(point);
        }
    }

    protected override bool HasReached(Vector3 point)
    {
        // Its goal may have been moved inside the walls, so compare against that.
        if (HorizontalDistance(transform.position, KeepInside(point)) < 1.3f)
        {
            return true;
        }
        // Cannot get any closer (a closed door it chose not to bash, or a wall): count it as reached.
        return agent.enabled && !agent.pathPending && agent.hasPath && agent.remainingDistance < 0.3f;
    }

    // ---------- Per frame ----------

    protected override void Update()
    {
        if (IsSpawned && IsServer && IsActive())
        {
            if (bashingDoor != null)
            {
                UpdateBash();
                UpdateChant();
                return; // the state machine waits while it bashes
            }
            CheckForDoorAhead();
            UpdateDoorTell();
        }

        base.Update();
        if (IsSpawned)
        {
            UpdateChant();
        }
    }

    private bool IsDistracted => Time.time < distractedUntil;

    private bool InHouse(Vector3 position)
    {
        return house == null || house.Contains(position, 0.5f);
    }

    private Vector3 KeepInside(Vector3 point)
    {
        return house != null ? house.ClampInside(point) : point;
    }

    private bool HasLineOfSight(Vector3 targetFeet)
    {
        Vector3 eyes = transform.position + Vector3.up * 2.2f;
        Vector3 targetChest = targetFeet + Vector3.up * 1.2f;
        int mask = ~(1 << 2); // ignore players and trigger zones
        if (Physics.Linecast(eyes, targetChest, mask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return !HouseDoor.BlocksLine(eyes, targetChest);
    }

    // ---------- Doors (server) ----------

    private void CheckForDoorAhead()
    {
        MonsterState state = State;
        if (state == MonsterState.Attack || state == MonsterState.Idle)
        {
            return;
        }

        Vector3 goal = state == MonsterState.Hunt && target != null ? target.Position : TargetPoint;
        foreach (HouseDoor door in HouseDoor.All)
        {
            if (!door.IsClosed || HorizontalDistance(transform.position, door.transform.position) > doorCheckDistance)
            {
                continue;
            }
            if (door.SideOf(transform.position) == door.SideOf(goal))
            {
                continue; // the door is not between it and its goal
            }

            if (state == MonsterState.Hunt || state == MonsterState.Investigate)
            {
                // Chasing someone: smash through.
                bashingDoor = door;
                bashTimer = bashSeconds;
                bangTimer = 0f;
                agent.ResetPath();
            }
            else
            {
                // Just walking around its own house: it opens the door like the owner it once was.
                door.ServerSetClosed(false);
            }
            return;
        }
    }

    private void UpdateBash()
    {
        if (agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
        }

        bangTimer -= Time.deltaTime;
        if (bangTimer <= 0f)
        {
            bangTimer = 0.6f;
            if (WorldSync.Instance != null)
            {
                WorldSync.Instance.ServerPlaySound(WorldSound.DoorSlam, bashingDoor.transform.position + Vector3.up, 1f, 40f);
            }
            GameEvents.RaiseNoise(bashingDoor.transform.position, 2f, false);
        }

        bashTimer -= Time.deltaTime;
        if (bashTimer > 0f)
        {
            return;
        }

        HouseDoor door = bashingDoor;
        bashingDoor = null;

        if (!door.IsClosed)
        {
            return; // someone opened it for us
        }
        if (door.HolderCount() >= holdersNeeded)
        {
            // Held shut: it gives up and goes elsewhere.
            DoorHeldClientRpc(door.transform.position);
            ChangeState(MonsterState.Wander);
            return;
        }
        door.ServerSetClosed(false);
        if (WorldSync.Instance != null)
        {
            WorldSync.Instance.ServerPlaySound(WorldSound.Roar, transform.position + Vector3.up * 2f, 0.8f, 40f);
        }
    }

    // The tell: now and then a door in its house swings shut by itself.
    private void UpdateDoorTell()
    {
        closeDoorTimer -= Time.deltaTime;
        if (closeDoorTimer > 0f)
        {
            return;
        }
        closeDoorTimer = Random.Range(closeDoorInterval.x, closeDoorInterval.y);

        System.Collections.Generic.List<HouseDoor> open = new System.Collections.Generic.List<HouseDoor>();
        foreach (HouseDoor door in HouseDoor.All)
        {
            if (!door.IsClosed && (house == null || house.Contains(door.transform.position, 1f)) && !door.AnyPlayerWithin(2f))
            {
                open.Add(door);
            }
        }
        if (open.Count > 0)
        {
            open[Random.Range(0, open.Count)].ServerSetClosed(true);
        }
    }

    // ---------- Tells (every machine) ----------

    private void UpdateChant()
    {
        if (chantSource == null)
        {
            return;
        }
        MonsterState state = State;
        bool angry = state == MonsterState.Hunt || state == MonsterState.Attack;
        float wanted = (angry ? 1f : 0.5f) * GameSettings.SfxVolume;
        chantSource.volume = Mathf.MoveTowards(chantSource.volume, wanted, Time.deltaTime);
        chantSource.pitch = Mathf.MoveTowards(chantSource.pitch, angry ? 1.15f : 1f, Time.deltaTime * 0.5f);
    }

    [ClientRpc]
    private void RoarClientRpc()
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Roar, transform.position + Vector3.up * 2f, 1f, 45f);
        HighlightMoments.Flag("Brahmarakshas attack", 80, transform.position);
    }

    [ClientRpc]
    private void DoorHeldClientRpc(Vector3 doorPosition)
    {
        MessageFeed.Show("The door holds. The chanting moves away.", 3f);
        HighlightMoments.Flag("Held the door", 60, doorPosition);
    }

    // ---------- Look ----------

    private void BuildBody()
    {
        Color ash = new Color(0.55f, 0.55f, 0.52f);
        Color saffron = new Color(0.85f, 0.45f, 0.08f);
        Color eyes = new Color(1f, 0.05f, 0.02f);
        Color thread = new Color(0.9f, 0.88f, 0.8f);

        // Tall and gaunt, about 2.6 m.
        MakePart(PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.55f, 0.6f), saffron); // dhoti
        MakePart(PrimitiveType.Capsule, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 0.75f, 0.45f), ash);       // chest
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 2.35f, 0f), new Vector3(0.42f, 0.48f, 0.42f), ash);     // head
        MakePart(PrimitiveType.Cube, new Vector3(0f, 1.65f, 0.21f), new Vector3(0.04f, 0.9f, 0.02f), thread);  // sacred thread
        MakePart(PrimitiveType.Sphere, new Vector3(-0.09f, 2.4f, 0.18f), new Vector3(0.08f, 0.05f, 0.05f), eyes);
        MakePart(PrimitiveType.Sphere, new Vector3(0.09f, 2.4f, 0.18f), new Vector3(0.08f, 0.05f, 0.05f), eyes);
        // Long arms that reach past its knees.
        MakePart(PrimitiveType.Capsule, new Vector3(-0.45f, 1.25f, 0f), new Vector3(0.16f, 0.8f, 0.16f), ash);
        MakePart(PrimitiveType.Capsule, new Vector3(0.45f, 1.25f, 0f), new Vector3(0.16f, 0.8f, 0.16f), ash);

        // A faint red glow so it shows up in the dark.
        GameObject glow = new GameObject("Eye glow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, 2.4f, 0.4f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = eyes;
        light.range = 3f;
        light.intensity = 1.2f;
    }

    private void MakePart(PrimitiveType shape, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(transform, false);
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
