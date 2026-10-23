using UnityEngine;

// The Water Ghoul (section 4.6). Lives in deep water, hunts anything that splashes or swims.
// Tells: V-shaped ripples on the surface while it moves.
// Counters: stay out of the water, or crouch-wade slowly (quiet), and keep moving to shore.
public class JalPishach : MonsterBase
{
    [Header("Water")]
    [SerializeField] private WaterVolume homeWater;
    [SerializeField, Tooltip("The monster sleeps until the water surface is at least this high (world Y).")]
    private float minSurfaceHeight = 0.5f;
    [SerializeField, Tooltip("How far below the surface the body hides while stalking.")]
    private float submergeDepth = 0.5f;
    [SerializeField, Tooltip("Keep this far from the edge of the water.")]
    private float edgeMargin = 1f;
    [SerializeField] private float turnSpeed = 6f;

    [Header("Attack")]
    [SerializeField, Tooltip("Downward drag speed applied to the victim (m/s).")]
    private float dragDownSpeed = 5f;

    [Header("Tell: ripples")]
    [SerializeField] private Color rippleColor = new Color(0.85f, 0.95f, 1f);

    private Transform body;
    private Transform ripple;
    private Renderer[] bodyRenderers;
    private Renderer[] rippleRenderers;
    private float currentHeightOffset;

    protected override void Start()
    {
        base.Start();
        BuildBody();
        BuildRipple();
    }

    // ---------- Visuals built in code so the monster needs no prefab ----------

    private void BuildBody()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Body";
        Destroy(visual.GetComponent<Collider>());
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lying flat
        visual.GetComponent<Renderer>().material.color = new Color(0.1f, 0.15f, 0.12f);
        body = visual.transform;
        bodyRenderers = new Renderer[] { visual.GetComponent<Renderer>() };
    }

    private void BuildRipple()
    {
        GameObject root = new GameObject("Ripple");
        root.transform.SetParent(transform, false);
        ripple = root.transform;

        // Two thin slabs forming a "V" trailing behind the monster
        MakeRippleArm(root.transform, 28f);
        MakeRippleArm(root.transform, -28f);
        rippleRenderers = root.GetComponentsInChildren<Renderer>();
    }

    private void MakeRippleArm(Transform parent, float angle)
    {
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(arm.GetComponent<Collider>());
        arm.transform.SetParent(parent, false);
        arm.transform.localScale = new Vector3(0.08f, 0.02f, 2.2f);
        arm.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        // The slab's front end sits at the root, so the centre is half its length behind.
        arm.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, -1.1f);
        arm.GetComponent<Renderer>().material.color = rippleColor;
    }

    // ---------- MonsterBase rules ----------

    protected override bool IsActive()
    {
        return homeWater != null && homeWater.SurfaceHeight >= minSurfaceHeight;
    }

    protected override bool CanHearNoise(Vector3 position, float loudness, bool fromWater)
    {
        // Only splashing/swimming noises, and only ones over its own water.
        if (!fromWater)
        {
            return false;
        }
        return IsOverWater(position, 2f);
    }

    protected override bool CanSeePlayer()
    {
        // It sees swimmers, not people wading or standing on shore.
        return player.IsSwimming && IsOverWater(player.transform.position, 0f);
    }

    protected override bool CanTrackPlayer()
    {
        // Follows anyone still in the water. Climbing out loses it.
        return player.WaterDepth >= 0.3f && IsOverWater(player.transform.position, 1f);
    }

    protected override bool CanAttackPlayer()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            return false;
        }
        bool inWater = player.WaterDepth >= 0.3f;
        return inWater && HorizontalDistanceToPlayer() <= data.attackRange;
    }

    protected override void PerformAttack()
    {
        // Drag the victim down and toward the monster.
        Vector3 toMonster = transform.position - player.transform.position;
        toMonster.y = 0f;
        player.AddImpulse(Vector3.down * dragDownSpeed + toMonster.normalized * 2f);
        playerHealth.TakeDamage(data.attackDamage);
        GameEvents.RaiseNoise(transform.position, 3f, true); // the splash of the attack
    }

    protected override Vector3 GetWanderPoint()
    {
        Bounds area = homeWater.Area;
        float x = Random.Range(area.min.x + edgeMargin, area.max.x - edgeMargin);
        float z = Random.Range(area.min.z + edgeMargin, area.max.z - edgeMargin);
        return new Vector3(x, 0f, z);
    }

    protected override void MoveToward(Vector3 point, float speed)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);

        Vector3 next = transform.position + transform.forward * speed * Time.deltaTime;
        transform.position = KeepInsideWater(next);
    }

    protected override bool HasReached(Vector3 point)
    {
        return HorizontalDistance(transform.position, point) < 1f;
    }

    // ---------- Per-frame look ----------

    protected override void Update()
    {
        base.Update();
        UpdateHeightAndVisibility();
    }

    private void UpdateHeightAndVisibility()
    {
        bool active = IsActive();
        SetRenderers(bodyRenderers, active);

        if (!active || body == null)
        {
            SetRenderers(rippleRenderers, false);
            return;
        }

        // Stay submerged, but rise toward the surface during an attack wind-up.
        float wantedOffset = State == MonsterState.Attack ? -0.1f : -submergeDepth;
        currentHeightOffset = Mathf.MoveTowards(currentHeightOffset, wantedOffset, 3f * Time.deltaTime);

        Vector3 position = transform.position;
        position.y = homeWater.SurfaceHeight + currentHeightOffset;
        transform.position = position;

        // Ripples show whenever it is moving (not idle) - this is the tell.
        bool moving = State != MonsterState.Idle && !(State == MonsterState.Investigate && HasReached(TargetPoint));
        SetRenderers(rippleRenderers, moving);
        if (ripple != null)
        {
            Vector3 ripplePosition = new Vector3(transform.position.x, homeWater.SurfaceHeight + 0.03f, transform.position.z);
            ripple.position = ripplePosition;
            ripple.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            float size = State == MonsterState.Hunt ? 1.4f : 1f;
            ripple.localScale = Vector3.one * size;
        }
    }

    private static void SetRenderers(Renderer[] renderers, bool visible)
    {
        if (renderers == null)
        {
            return;
        }
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }

    // ---------- Helpers ----------

    private bool IsOverWater(Vector3 position, float margin)
    {
        Bounds area = homeWater.Area;
        return position.x >= area.min.x - margin && position.x <= area.max.x + margin
            && position.z >= area.min.z - margin && position.z <= area.max.z + margin;
    }

    private Vector3 KeepInsideWater(Vector3 position)
    {
        Bounds area = homeWater.Area;
        position.x = Mathf.Clamp(position.x, area.min.x + 0.5f, area.max.x - 0.5f);
        position.z = Mathf.Clamp(position.z, area.min.z + 0.5f, area.max.z - 0.5f);
        return position;
    }
}
