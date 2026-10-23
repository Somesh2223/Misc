using UnityEngine;

// A physics package. Tracks its condition (0 = ruined, 1 = perfect) from impacts and water.
[RequireComponent(typeof(Rigidbody))]
public class Package : MonoBehaviour
{
    [SerializeField] private PackageData data;
    [SerializeField, Tooltip("Address this package must be delivered to (matches a DeliveryPoint).")]
    private string destinationAddress = "";

    [Header("Buoyancy")]
    [SerializeField, Tooltip("Packages lighter than this (kg) float; heavier ones sink.")]
    private float floatMassLimit = 4f;
    [SerializeField, Tooltip("Upward push (m/s^2) while under water, for floating packages.")]
    private float buoyancyAcceleration = 14f;
    [SerializeField, Tooltip("Water drag so packages do not rocket around underwater.")]
    private float waterDrag = 3f;

    private Rigidbody body;
    private Renderer cachedRenderer;
    private Color startColor;

    private float health;
    private float wetness; // seconds spent submerged
    private bool isHeld;
    private bool isDelivered;
    private float timeCreated;

    public PackageData Data => data;
    public string DestinationAddress => destinationAddress;
    public Rigidbody Body => body;
    public bool IsHeld => isHeld;
    public bool IsDelivered => isDelivered;
    public bool IsSubmerged { get; private set; }
    public float AgeSeconds => Time.time - timeCreated;

    // 1 = perfect, 0 = ruined. Multiplied into the payout.
    public float Condition
    {
        get
        {
            float result = 1f;
            if (data.isFragile)
            {
                result *= Mathf.Clamp01(health / data.maxHealth);
            }
            if (data.wetSensitive)
            {
                result *= 1f - Mathf.Clamp01(wetness / data.secondsToRuin);
            }
            return result;
        }
    }

    public void SetDestination(string address)
    {
        destinationAddress = address;
    }

    // Lets spawners assign data before Awake runs.
    public void SetData(PackageData newData)
    {
        data = newData;
        ApplyData();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null)
        {
            startColor = cachedRenderer.material.color;
        }
        timeCreated = Time.time;
        if (data != null)
        {
            ApplyData();
        }
    }

    private void ApplyData()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }
        body.mass = data.mass;
        health = data.maxHealth;
        wetness = 0f;
        UpdateLook();
    }

    public void SetHeld(bool held)
    {
        isHeld = held;
    }

    public void MarkDelivered()
    {
        isDelivered = true;
    }

    private void FixedUpdate()
    {
        if (data == null)
        {
            return;
        }
        HandleWater();
    }

    private void HandleWater()
    {
        float depth = WaterVolume.GetDepthAt(transform.position, GetFloorY());
        float surface = GetFloorY() + depth;
        IsSubmerged = depth > 0f && transform.position.y < surface;

        if (!IsSubmerged)
        {
            return;
        }

        // Light packages float, heavy ones sink
        if (data.mass <= floatMassLimit)
        {
            body.AddForce(Vector3.up * buoyancyAcceleration, ForceMode.Acceleration);
        }
        body.linearVelocity *= 1f - waterDrag * Time.fixedDeltaTime;

        if (data.wetSensitive)
        {
            wetness += Time.fixedDeltaTime;
            UpdateLook();
        }
    }

    private float GetFloorY()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Ignore ourselves if the ray hit our own collider
            if (hit.rigidbody != body)
            {
                return hit.point.y;
            }
        }
        return transform.position.y - 1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (data == null || !data.isFragile)
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed <= data.safeImpactSpeed)
        {
            return;
        }

        float damage = (impactSpeed - data.safeImpactSpeed) * data.damagePerSpeed;
        health = Mathf.Max(0f, health - damage);
        UpdateLook();

        // Hard impacts are loud
        GameEvents.RaiseNoise(transform.position, Mathf.Clamp(impactSpeed, 1f, 10f), false);
    }

    // Cracks: the package darkens and turns red-brown as it takes damage or gets wet.
    private void UpdateLook()
    {
        if (cachedRenderer == null || data == null)
        {
            return;
        }
        Color damaged = new Color(0.35f, 0.15f, 0.1f);
        float amount = 1f - Condition;
        cachedRenderer.material.color = Color.Lerp(startColor, damaged, amount);
    }
}
