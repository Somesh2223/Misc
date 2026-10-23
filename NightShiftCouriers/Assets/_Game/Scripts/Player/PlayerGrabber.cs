using UnityEngine;

// R.E.P.O.-style "hold beam": the held package is pulled toward a point in front of the camera.
// E picks up / drops, left mouse throws.
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerGrabber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Grabbing")]
    [SerializeField, Tooltip("How far away you can grab a package (metres).")]
    private float grabRange = 3f;
    [SerializeField, Tooltip("Where the package floats in front of the camera (metres).")]
    private float holdDistance = 1.8f;
    [SerializeField, Tooltip("How hard the package is pulled to the hold point. Higher = stiffer.")]
    private float holdStrength = 12f;
    [SerializeField, Tooltip("Max speed the package may move at while held (m/s).")]
    private float maxHoldSpeed = 15f;
    [SerializeField, Tooltip("If the package gets this far from the hold point (stuck on a wall), it is dropped.")]
    private float breakDistance = 2.5f;

    [Header("Throwing")]
    [SerializeField, Tooltip("Throw speed for a 1 kg package (m/s). Heavier packages go slower.")]
    private float throwSpeed = 12f;

    [Header("Weight")]
    [SerializeField, Tooltip("At this carried mass (kg) you move at the slowest speed.")]
    private float heaviestMass = 15f;
    [SerializeField, Range(0.2f, 1f), Tooltip("Speed multiplier when carrying the heaviest load.")]
    private float slowestSpeedMultiplier = 0.55f;

    private PlayerInputReader input;
    private Package held;
    private bool heldHadGravity;

    public bool IsHolding => held != null;
    public Package HeldPackage => held;

    // 1.0 when empty-handed; lower when carrying something heavy.
    public float SpeedMultiplier
    {
        get
        {
            if (held == null)
            {
                return 1f;
            }
            float t = Mathf.Clamp01(held.Body.mass / heaviestMass);
            return Mathf.Lerp(1f, slowestSpeedMultiplier, t);
        }
    }

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (input.InteractPressedThisFrame)
        {
            if (held != null)
            {
                Drop();
            }
            else
            {
                TryGrab();
            }
        }

        if (held != null && input.ThrowPressedThisFrame)
        {
            Throw();
        }
    }

    private void FixedUpdate()
    {
        if (held == null)
        {
            return;
        }

        Vector3 target = cameraTransform.position + cameraTransform.forward * holdDistance;
        Vector3 toTarget = target - held.Body.position;

        if (toTarget.magnitude > breakDistance)
        {
            Drop();
            return;
        }

        Vector3 velocity = toTarget * holdStrength;
        velocity = Vector3.ClampMagnitude(velocity, maxHoldSpeed);
        held.Body.linearVelocity = velocity;
        held.Body.angularVelocity *= 0.8f; // stop wild spinning
    }

    private void TryGrab()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, grabRange, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Package package = hit.collider.GetComponentInParent<Package>();
        if (package == null || package.IsHeld || package.IsDelivered)
        {
            return;
        }

        held = package;
        held.SetHeld(true);
        heldHadGravity = held.Body.useGravity;
        held.Body.useGravity = false;
    }

    private void Drop()
    {
        if (held == null)
        {
            return;
        }
        held.Body.useGravity = heldHadGravity;
        held.SetHeld(false);
        held = null;
    }

    private void Throw()
    {
        Package thrown = held;
        Drop();

        float speed = throwSpeed / Mathf.Max(1f, thrown.Body.mass);
        thrown.Body.linearVelocity = cameraTransform.forward * speed;
        GameEvents.RaiseNoise(transform.position, 3f, false);
    }
}
