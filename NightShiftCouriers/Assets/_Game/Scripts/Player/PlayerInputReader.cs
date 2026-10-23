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

    public Vector2 Move => moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction.ReadValue<Vector2>();
    public bool SprintHeld => sprintAction.IsPressed();
    public bool CrouchHeld => crouchAction.IsPressed();
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpPressedThisFrame => jumpAction.WasPressedThisFrame();

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
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
        jumpAction.Disable();
    }
}
