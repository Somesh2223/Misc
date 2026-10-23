using UnityEngine;
using UnityEngine.InputSystem;

// First-person controller built on CharacterController (section 4.1).
// Walk, sprint, crouch, jump, swim, and a shared stamina pool.
// Movement is owner-side only for now; networking comes in Milestone 2.
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

    private CharacterController controller;
    private PlayerInputReader input;
    private PlayerStamina stamina;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float lookPitch;
    private bool isCrouching;
    private bool isSprinting;
    private bool isSwimming;
    private float waterDepth;

    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public bool IsSwimming => isSwimming;
    public float WaterDepth => waterDepth;
    public bool IsGrounded => controller.isGrounded;
    public float CurrentSpeed => horizontalVelocity.magnitude;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        stamina = GetComponent<PlayerStamina>();
    }

    private void Start()
    {
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCursorToggle();
        UpdateWaterState();
        HandleLook();
        HandleCrouch();

        bool spentStamina;
        if (isSwimming)
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
        Vector2 look = input.Look * lookSensitivity;

        transform.Rotate(Vector3.up * look.x);

        lookPitch -= look.y;
        lookPitch = Mathf.Clamp(lookPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(lookPitch, 0f, 0f);
    }

    // ---------- Crouch ----------

    private void HandleCrouch()
    {
        bool wantsCrouch = input.CrouchHeld && !isSwimming;

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
        float radius = controller.radius * 0.9f;
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
        return !Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
    }

    // ---------- Ground movement (walk, sprint, crouch, wade, jump) ----------

    // Returns true if stamina was spent this frame.
    private bool HandleGroundMovement()
    {
        bool spentStamina = false;
        Vector2 moveInput = input.Move;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // Sprint needs stamina, forward-ish movement, and not being exhausted.
        isSprinting = input.SprintHeld && isMoving && !isCrouching
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

        if (controller.isGrounded && input.JumpPressedThisFrame && !isCrouching && stamina.TrySpend(jumpCost))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            spentStamina = true;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
        return spentStamina;
    }

    // ---------- Swimming ----------

    // Returns true if stamina was spent this frame.
    private bool HandleSwimMovement()
    {
        isSprinting = false;
        verticalVelocity = 0f;

        Vector2 moveInput = input.Move;
        // Swim in the direction you are looking, so looking up swims up.
        Vector3 wishDirection = cameraTransform.right * moveInput.x + cameraTransform.forward * moveInput.y;
        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

        float speed = swimSpeed;
        bool exhausted = stamina.IsExhausted;
        if (exhausted)
        {
            speed *= 0.4f; // tired swimmers barely move
        }

        Vector3 targetVelocity = wishDirection * speed;

        // Jump swims up, crouch dives down.
        if (input.JumpHeld)
        {
            targetVelocity.y += swimVerticalSpeed;
        }
        if (input.CrouchHeld)
        {
            targetVelocity.y -= swimVerticalSpeed;
        }

        // Without input the player slowly floats back toward the surface.
        bool hasInput = wishDirection.sqrMagnitude > 0.01f || input.JumpHeld || input.CrouchHeld;
        if (!hasInput)
        {
            targetVelocity.y = 0.5f;
        }

        Vector3 currentVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * 0.5f * Time.deltaTime);
        horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        Vector3 move = new Vector3(currentVelocity.x, currentVelocity.y, currentVelocity.z);
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
}
