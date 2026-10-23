using UnityEngine;
using UnityEngine.InputSystem;

// First-person controller built on CharacterController (section 4.1).
// Walk, sprint, crouch, jump, swim, climb, a shared stamina pool, and wetness.
// Only the owner runs this; NetworkPlayer publishes the result to everyone else.
[DefaultExecutionOrder(100)] // after moving platforms (rafts) have moved
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("The camera that sits at eye height. Child of the player.")]
    private Transform cameraTransform;

    [Header("Speeds (metres per second)")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField, Tooltip("Speed while wading in shallow water.")]
    private float wadeSpeed = 3f;
    [SerializeField] private float swimSpeed = 2.5f;
    [SerializeField, Tooltip("How quickly speed changes (higher = snappier).")]
    private float acceleration = 12f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouching")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField, Tooltip("Camera height as a fraction of the capsule height.")]
    private float eyeHeightFraction = 0.9f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Mouse Look")]
    [SerializeField, Tooltip("Degrees per mouse pixel.")]
    private float lookSensitivity = 0.12f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Stamina Costs")]
    [SerializeField, Tooltip("Stamina per second while sprinting.")]
    private float sprintCostPerSecond = 15f;
    [SerializeField, Tooltip("Stamina per second while swimming.")]
    private float swimCostPerSecond = 8f;
    [SerializeField, Tooltip("Stamina per jump.")]
    private float jumpCost = 10f;

    [Header("Water")]
    [SerializeField, Tooltip("Water deeper than this (metres) slows you down to a wade.")]
    private float wadeDepth = 0.3f;
    [SerializeField, Tooltip("Water deeper than this (metres) makes you swim. Roughly waist height.")]
    private float swimDepth = 1.0f;
    [SerializeField, Tooltip("How fast you rise/sink while swimming.")]
    private float swimVerticalSpeed = 2.5f;

    [Header("Climbing")]
    [SerializeField, Tooltip("How far in front of you a climbable surface can be grabbed (metres).")]
    private float climbReach = 0.8f;
    [SerializeField] private float climbSpeed = 2f;
    [SerializeField, Tooltip("Stamina per second just for holding on.")]
    private float climbHoldCostPerSecond = 6f;
    [SerializeField, Tooltip("Extra stamina per second while moving on the wall.")]
    private float climbMoveCostPerSecond = 6f;
    [SerializeField, Tooltip("How far up and forward you pull yourself at the top of a wall.")]
    private float mantleUp = 1.3f;
    [SerializeField] private float mantleForward = 0.8f;

    [Header("Wetness")]
    [SerializeField, Tooltip("Wetness gained per second while wading (0-1 scale). Swimming soaks you instantly.")]
    private float wetPerSecond = 0.5f;
    [SerializeField, Tooltip("Wetness lost per second out in the rain.")]
    private float dryOutsidePerSecond = 0.01f;
    [SerializeField, Tooltip("Wetness lost per second under a roof (indoors).")]
    private float dryIndoorsPerSecond = 0.08f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Speed lost when fully soaked (0.15 = 15% slower).")]
    private float soakedSlowdown = 0.15f;

    private CharacterController controller;
    private PlayerInputReader input;
    private PlayerStamina stamina;
    private PlayerGrabber grabber; // optional
    private PlayerHealth health;   // optional

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float lookPitch;
    private bool isCrouching;
    private bool isSprinting;
    private bool isSwimming;
    private float waterDepth;
    private Vector3 externalVelocity; // shoves from monsters etc., fades out
    private bool isClimbing;
    private Vector3 climbNormal;
    private float wetness;
    private MovingPlatform platform; // what we are standing on, if it moves
    private readonly Collider[] standUpHits = new Collider[8];

    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsSwimming => isSwimming;
    public float WaterDepth => waterDepth;
    public bool IsGrounded => controller.isGrounded;
    public float CurrentSpeed => horizontalVelocity.magnitude;
    public float Pitch => lookPitch;
    public bool IsClimbing => isClimbing;
    public bool IsOnRaft => platform != null && platform.isRaft;
    // 0 = dry, 1 = soaked. Wet players are slower and louder (PlayerNoise reads this).
    public float Wetness => wetness;

    // Input, but nothing while a menu is open.
    private Vector2 MoveInput => UIState.MenuOpen ? Vector2.zero : input.Move;
    private Vector2 LookInput => UIState.MenuOpen ? Vector2.zero : input.Look;
    private bool JumpPressed => !UIState.MenuOpen && input.JumpPressedThisFrame;
    private bool JumpHeld => !UIState.MenuOpen && input.JumpHeld;
    private bool CrouchHeld => !UIState.MenuOpen && input.CrouchHeld;
    private bool SprintHeld => !UIState.MenuOpen && input.SprintHeld;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        stamina = GetComponent<PlayerStamina>();
        grabber = GetComponent<PlayerGrabber>();
        health = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Moves the player instantly (spawning, respawning, server snap-back).
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        controller.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        externalVelocity = Vector3.zero;
        isClimbing = false;
    }

    // A teammate grabbed our hand while we were climbing: end up next to them (section 4.1).
    public void PullUpTo(Vector3 helperFeet, Vector3 awayFromEdge)
    {
        Vector3 target = helperFeet + awayFromEdge.normalized * 0.7f + Vector3.up * 0.05f;
        Teleport(target, transform.eulerAngles.y);
        stamina.Restore(15f); // a little breather
    }

    // Gives the player a push (e.g. a monster dragging them down). Fades out quickly.
    public void AddImpulse(Vector3 velocity)
    {
        externalVelocity += velocity;
    }

    private void Update()
    {
        externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, 6f * Time.deltaTime);
        HandleCursorToggle();

        if (health != null && health.IsDead)
        {
            isClimbing = false;
            return; // no moving while dead
        }

        UpdateWaterState();
        UpdateWetness();
        RideMovingPlatform();
        HandleLook();
        HandleCrouch();

        bool spentStamina;
        if (isClimbing)
        {
            spentStamina = HandleClimbing();
        }
        else if (isSwimming)
        {
            spentStamina = HandleSwimMovement();
        }
        else
        {
            spentStamina = HandleGroundMovement();
        }

        stamina.Tick(spentStamina);
    }

    // Esc frees the mouse (for the editor), clicking locks it again.
    private void HandleCursorToggle()
    {
        if (UIState.MenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ---------- Water ----------

    private void UpdateWaterState()
    {
        waterDepth = WaterVolume.GetDepthAt(transform.position, GetGroundY());
        isSwimming = waterDepth >= swimDepth;
    }

    // The Y of the floor below the player (feet height, or the floor when swimming above it).
    private float GetGroundY()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }
        return transform.position.y;
    }

    // ---------- Look ----------

    private void HandleLook()
    {
        Vector2 look = LookInput * lookSensitivity;

        transform.Rotate(Vector3.up * look.x);

        lookPitch -= look.y;
        lookPitch = Mathf.Clamp(lookPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(lookPitch, 0f, 0f);
    }

    // ---------- Crouch ----------

    private void HandleCrouch()
    {
        bool wantsCrouch = CrouchHeld && !isSwimming && !isClimbing;

        if (wantsCrouch)
        {
            isCrouching = true;
        }
        else if (isCrouching && CanStandUp())
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        float newHeight = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, newHeight / 2f, 0f);

        Vector3 camPos = cameraTransform.localPosition;
        camPos.y = newHeight * eyeHeightFraction;
        cameraTransform.localPosition = camPos;
    }

    // Stops you standing up inside a low ceiling.
    private bool CanStandUp()
    {
        // Check the space a standing body would fill, starting a little above the floor
        // so standing on the ground does not count as "blocked".
        float radius = controller.radius * 0.9f;
        Vector3 bottom = transform.position + Vector3.up * (radius + 0.1f);
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);

        int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, standUpHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            // Our own CharacterController (and anything parented to us) is not a ceiling.
            if (!standUpHits[i].transform.IsChildOf(transform))
            {
                return false;
            }
        }
        return true;
    }

    // ---------- Ground movement (walk, sprint, crouch, wade, jump) ----------

    // Returns true if stamina was spent this frame.
    private bool HandleGroundMovement()
    {
        bool spentStamina = false;
        Vector2 moveInput = MoveInput;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Space in front of a climbable surface grabs on instead of jumping.
        if (JumpPressed && TryFindClimbable(out RaycastHit climbHit))
        {
            StartClimbing(climbHit);
            return true;
        }

        // Sprint needs stamina, forward-ish movement, and not being exhausted.
        isSprinting = SprintHeld && isMoving && !isCrouching
                      && !stamina.IsExhausted && waterDepth < wadeDepth;

        float targetSpeed = walkSpeed;
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (waterDepth >= wadeDepth)
        {
            targetSpeed = wadeSpeed;
        }
        else if (isSprinting)
        {
            targetSpeed = sprintSpeed;
        }

        if (grabber != null)
        {
            targetSpeed *= grabber.SpeedMultiplier;
        }
        targetSpeed *= 1f - soakedSlowdown * wetness;

        if (isSprinting)
        {
            stamina.Spend(sprintCostPerSecond * Time.deltaTime);
            spentStamina = true;
        }

        Vector3 wishDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wishDirection * targetSpeed, acceleration * Time.deltaTime);

        // Gravity and jumping
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // small push keeps the controller stuck to the ground
        }

        if (controller.isGrounded && JumpPressed && !isCrouching && stamina.TrySpend(jumpCost))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            spentStamina = true;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity + externalVelocity;
        controller.Move(velocity * Time.deltaTime);
        return spentStamina;
    }

    // ---------- Swimming ----------

    // Returns true if stamina was spent this frame.
    private bool HandleSwimMovement()
    {
        isSprinting = false;
        verticalVelocity = 0f;

        Vector2 moveInput = MoveInput;
        // Swim in the direction you are looking, so looking up swims up.
        Vector3 wishDirection = cameraTransform.right * moveInput.x + cameraTransform.forward * moveInput.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

        float speed = swimSpeed;
        if (grabber != null)
        {
            speed *= grabber.SpeedMultiplier;
        }
        bool exhausted = stamina.IsExhausted;
        if (exhausted)
        {
            speed *= 0.4f; // tired swimmers barely move
        }

        Vector3 targetVelocity = wishDirection * speed;

        // Jump swims up, crouch dives down.
        if (JumpHeld)
        {
            targetVelocity.y += swimVerticalSpeed;
        }
        if (CrouchHeld)
        {
            targetVelocity.y -= swimVerticalSpeed;
        }

        // Without input the player slowly floats back toward the surface.
        bool hasInput = wishDirection.sqrMagnitude > 0.01f || JumpHeld || CrouchHeld;
        if (!hasInput)
        {
            targetVelocity.y = 0.5f;
        }

        Vector3 currentVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * 0.5f * Time.deltaTime);
        horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        Vector3 move = currentVelocity + externalVelocity;
        controller.Move(move * Time.deltaTime);

        // Keep the head from leaving the water too far (can't fly out of the surface).
        KeepAtSurface();

        bool paddling = hasInput;
        if (paddling && !exhausted)
        {
            stamina.Spend(swimCostPerSecond * Time.deltaTime);
        }
        return paddling && !exhausted;
    }

    private void KeepAtSurface()
    {
        float groundY = GetGroundY();
        float surfaceY = groundY + waterDepth;
        float maxFeetY = surfaceY - swimDepth * 0.7f; // chest-deep floating height
        if (transform.position.y > maxFeetY)
        {
            Vector3 position = transform.position;
            position.y = maxFeetY;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }
    }

    // ---------- Climbing (section 4.1) ----------

    private bool TryFindClimbable(out RaycastHit hit)
    {
        Vector3 chest = transform.position + Vector3.up * (controller.height * 0.6f);
        if (Physics.Raycast(chest, transform.forward, out hit, climbReach + controller.radius, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Climbable>() != null;
        }
        return false;
    }

    private void StartClimbing(RaycastHit hit)
    {
        isClimbing = true;
        isSprinting = false;
        climbNormal = Flatten(hit.normal, -transform.forward);
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    // Returns true if stamina was spent this frame.
    private bool HandleClimbing()
    {
        // Out of stamina, or Ctrl: let go and fall.
        if (stamina.IsExhausted || CrouchHeld)
        {
            isClimbing = false;
            return false;
        }

        // Space: push off the wall.
        if (JumpPressed)
        {
            isClimbing = false;
            horizontalVelocity = climbNormal * 3f;
            verticalVelocity = 3f;
            return true;
        }

        Vector2 move = MoveInput;

        // Is the surface still in front of our chest?
        Vector3 chest = transform.position + Vector3.up * (controller.height * 0.6f);
        bool stillOnWall = Physics.Raycast(chest, -climbNormal, out RaycastHit wallHit,
                               climbReach + controller.radius + 0.2f, ~0, QueryTriggerInteraction.Ignore)
                           && wallHit.collider.GetComponentInParent<Climbable>() != null;

        if (!stillOnWall)
        {
            if (move.y > 0.1f)
            {
                Mantle(); // climbed past the top: pull up onto the ledge
            }
            isClimbing = false;
            return true;
        }

        climbNormal = Flatten(wallHit.normal, climbNormal);
        Vector3 right = Vector3.Cross(Vector3.up, -climbNormal);
        Vector3 velocity = (Vector3.up * move.y + right * move.x) * climbSpeed - climbNormal * 0.5f;
        controller.Move(velocity * Time.deltaTime);

        // Climbing down onto the floor ends the climb.
        if (controller.isGrounded && move.y < -0.1f)
        {
            isClimbing = false;
        }

        float cost = climbHoldCostPerSecond;
        if (move.sqrMagnitude > 0.01f)
        {
            cost += climbMoveCostPerSecond;
        }
        stamina.Spend(cost * Time.deltaTime);
        return true;
    }

    private void Mantle()
    {
        controller.Move(Vector3.up * mantleUp);
        controller.Move(-climbNormal * mantleForward);
        verticalVelocity = 0f;
    }

    private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = fallback;
            direction.y = 0f;
        }
        return direction.normalized;
    }

    // ---------- Wetness (section 4.1) ----------

    private void UpdateWetness()
    {
        if (isSwimming)
        {
            wetness = 1f;
        }
        else if (waterDepth >= wadeDepth)
        {
            wetness = Mathf.Min(1f, wetness + wetPerSecond * Time.deltaTime);
        }
        else
        {
            bool indoors = Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.up, 15f, ~0, QueryTriggerInteraction.Ignore);
            float dryRate = indoors ? dryIndoorsPerSecond : dryOutsidePerSecond;
            wetness = Mathf.Max(0f, wetness - dryRate * Time.deltaTime);
        }
    }

    // ---------- Moving platforms (rafts) ----------

    private void RideMovingPlatform()
    {
        platform = null;
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            platform = hit.collider.GetComponentInParent<MovingPlatform>();
        }

        if (platform != null && platform.FrameDelta.sqrMagnitude > 0f)
        {
            controller.Move(platform.FrameDelta);
        }
    }
}
