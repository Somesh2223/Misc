using UnityEngine;

// Shared monster brain: the state machine and the hearing/sight plumbing.
// A specific monster (e.g. JalPishach) only fills in how it moves, what it can see, and how it attacks.
public abstract class MonsterBase : MonoBehaviour
{
    [SerializeField] protected MonsterData data;

    protected PlayerController player;
    protected PlayerHealth playerHealth;

    private MonsterState state = MonsterState.Idle;
    private float stateTimer;
    private float loseTargetTimer;
    private bool searching;
    private Vector3 targetPoint;
    private bool attackHit;

    public MonsterState State => state;
    public Vector3 TargetPoint => targetPoint;

    // ---------- Things each monster must define ----------

    // Is this monster currently active in the world at all?
    protected abstract bool IsActive();
    // Should this noise be able to attract the monster?
    protected abstract bool CanHearNoise(Vector3 position, float loudness, bool fromWater);
    // Can the monster see the player right now (starts a hunt)?
    protected abstract bool CanSeePlayer();
    // Can the monster still follow the player while hunting?
    protected abstract bool CanTrackPlayer();
    // Is the player close enough, and in the right place, to attack?
    protected abstract bool CanAttackPlayer();
    // Deal the actual hit.
    protected abstract void PerformAttack();
    // A random point the monster is allowed to walk/swim to.
    protected abstract Vector3 GetWanderPoint();
    // Move toward a point at a speed.
    protected abstract void MoveToward(Vector3 point, float speed);
    // True when the monster is near enough to a point.
    protected abstract bool HasReached(Vector3 point);

    // Optional hooks
    protected virtual void OnStateEntered(MonsterState newState) { }

    // ---------- Setup ----------

    protected virtual void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        ChangeState(MonsterState.Idle);
    }

    protected virtual void OnEnable()
    {
        GameEvents.NoiseMade += OnNoiseMade;
    }

    protected virtual void OnDisable()
    {
        GameEvents.NoiseMade -= OnNoiseMade;
    }

    // ---------- State machine ----------

    protected virtual void Update()
    {
        if (player == null || !IsActive())
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsDead && state != MonsterState.Cooldown)
        {
            ChangeState(MonsterState.Cooldown);
        }

        switch (state)
        {
            case MonsterState.Idle: UpdateIdle(); break;
            case MonsterState.Wander: UpdateWander(); break;
            case MonsterState.Investigate: UpdateInvestigate(); break;
            case MonsterState.Hunt: UpdateHunt(); break;
            case MonsterState.Attack: UpdateAttack(); break;
            case MonsterState.Cooldown: UpdateCooldown(); break;
        }
    }

    protected void ChangeState(MonsterState newState)
    {
        state = newState;
        searching = false;

        switch (newState)
        {
            case MonsterState.Idle:
                stateTimer = Random.Range(data.idleSecondsMin, data.idleSecondsMax);
                break;
            case MonsterState.Wander:
                targetPoint = GetWanderPoint();
                break;
            case MonsterState.Hunt:
                loseTargetTimer = data.loseTargetSeconds;
                break;
            case MonsterState.Attack:
                stateTimer = data.attackWindup;
                attackHit = false;
                break;
            case MonsterState.Cooldown:
                stateTimer = data.cooldownSeconds;
                targetPoint = GetWanderPoint();
                break;
        }

        OnStateEntered(newState);
    }

    private void UpdateIdle()
    {
        if (TryStartHuntFromSight())
        {
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(MonsterState.Wander);
        }
    }

    private void UpdateWander()
    {
        if (TryStartHuntFromSight())
        {
            return;
        }

        MoveToward(targetPoint, data.wanderSpeed);
        if (HasReached(targetPoint))
        {
            ChangeState(MonsterState.Idle);
        }
    }

    private void UpdateInvestigate()
    {
        if (TryStartHuntFromSight())
        {
            return;
        }

        if (!searching)
        {
            MoveToward(targetPoint, data.investigateSpeed);
            if (HasReached(targetPoint))
            {
                searching = true;
                stateTimer = data.searchSeconds;
            }
        }
        else
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                ChangeState(MonsterState.Wander);
            }
        }
    }

    private void UpdateHunt()
    {
        float distance = HorizontalDistanceToPlayer();

        if (CanTrackPlayer() && distance < data.forgetDistance)
        {
            targetPoint = player.transform.position;
            loseTargetTimer = data.loseTargetSeconds;
        }
        else
        {
            loseTargetTimer -= Time.deltaTime;
            if (loseTargetTimer <= 0f)
            {
                ChangeState(MonsterState.Wander);
                return;
            }
        }

        MoveToward(targetPoint, data.huntSpeed);

        if (CanAttackPlayer())
        {
            ChangeState(MonsterState.Attack);
        }
    }

    private void UpdateAttack()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f && !attackHit)
        {
            attackHit = true;
            if (CanAttackPlayer())
            {
                PerformAttack(); // the player can still dodge by leaving range during the wind-up
            }
            ChangeState(MonsterState.Cooldown);
        }
    }

    private void UpdateCooldown()
    {
        MoveToward(targetPoint, data.wanderSpeed * 1.5f);
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(MonsterState.Idle);
        }
    }

    // ---------- Senses ----------

    private bool TryStartHuntFromSight()
    {
        if (CanSeePlayer() && HorizontalDistanceToPlayer() <= data.sightRange)
        {
            targetPoint = player.transform.position;
            ChangeState(MonsterState.Hunt);
            return true;
        }
        return false;
    }

    private void OnNoiseMade(Vector3 position, float loudness, bool fromWater)
    {
        if (player == null || !IsActive())
        {
            return;
        }
        if (state == MonsterState.Attack || state == MonsterState.Cooldown)
        {
            return;
        }
        if (!CanHearNoise(position, loudness, fromWater))
        {
            return;
        }

        float hearingRadius = loudness * data.hearingRadiusPerLoudness;
        float distance = HorizontalDistance(transform.position, position);
        if (distance > hearingRadius)
        {
            return;
        }

        targetPoint = position;

        if (distance <= hearingRadius * data.huntNoiseFraction)
        {
            if (state != MonsterState.Hunt)
            {
                ChangeState(MonsterState.Hunt);
            }
            loseTargetTimer = data.loseTargetSeconds;
        }
        else if (state != MonsterState.Hunt)
        {
            ChangeState(MonsterState.Investigate);
            targetPoint = position;
        }
    }

    // ---------- Helpers ----------

    protected float HorizontalDistanceToPlayer()
    {
        return HorizontalDistance(transform.position, player.transform.position);
    }

    protected static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
