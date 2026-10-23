using UnityEngine;
using UnityEngine.InputSystem;

// Reads keyboard/mouse/gamepad input with the new Input System.
// The actions are created in code so there is no .inputactions asset to set up.
public class PlayerInputReader : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private InputAction throwAction;
    private InputAction useAction;
    private InputAction openAction;
    private InputAction journalAction;
    private readonly InputAction[] slotActions = new InputAction[4];

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();
    public bool SprintHeld => sprintAction.IsPressed();
    public bool CrouchHeld => crouchAction.IsPressed();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpPressedThisFrame => jumpAction.WasPressedThisFrame();
    public bool InteractPressedThisFrame => interactAction.WasPressedThisFrame();
    public bool ThrowPressedThisFrame => throwAction.WasPressedThisFrame();
    public bool UsePressedThisFrame => useAction.WasPressedThisFrame();
    public bool OpenPressedThisFrame => openAction.WasPressedThisFrame();
    public bool JournalPressedThisFrame => journalAction.WasPressedThisFrame();

    // The inventory slot (0-3) whose number key was pressed this frame, or -1.
    public int SlotPressedThisFrame
    {
        get
        {
            for (int i = 0; i < slotActions.Length; i++)
            {
                if (slotActions[i].WasPressedThisFrame())
                {
                    return i;
                }
            }
            return -1;
        }
    }

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/c");
        crouchAction.AddBinding("<Gamepad>/buttonEast");

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest");

        throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
        throwAction.AddBinding("<Gamepad>/rightTrigger");

        useAction = new InputAction("Use", InputActionType.Button, "<Mouse>/rightButton");
        useAction.AddBinding("<Gamepad>/leftTrigger");

        openAction = new InputAction("OpenPackage", InputActionType.Button, "<Keyboard>/f");
        journalAction = new InputAction("Journal", InputActionType.Button, "<Keyboard>/j");

        for (int i = 0; i < slotActions.Length; i++)
        {
            slotActions[i] = new InputAction("Slot" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
        jumpAction.Enable();
        interactAction.Enable();
        throwAction.Enable();
        useAction.Enable();
        openAction.Enable();
        journalAction.Enable();
        foreach (InputAction slot in slotActions)
        {
            slot.Enable();
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
        jumpAction.Disable();
        interactAction.Disable();
        throwAction.Disable();
        useAction.Disable();
        openAction.Disable();
        journalAction.Disable();
        foreach (InputAction slot in slotActions)
        {
            slot.Disable();
        }
    }
}
