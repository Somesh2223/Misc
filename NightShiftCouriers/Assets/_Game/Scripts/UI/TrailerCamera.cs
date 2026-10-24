using UnityEngine;
using UnityEngine.InputSystem;

// Trailer tool (plan M5): F9 detaches a free-flying camera from your player.
//   WASD move, mouse look, Q/E down/up, Shift fast, Ctrl slow, mouse wheel changes speed.
//   F1 hides the HUD, F10 toggles the retro look, so you can film clean shots.
// Your courier stands still while you fly (other players and monsters keep going).
public class TrailerCamera : MonoBehaviour
{
    [SerializeField, Tooltip("Normal flying speed (m/s).")]
    private float speed = 4f;
    [SerializeField] private float fastMultiplier = 4f;
    [SerializeField] private float slowMultiplier = 0.25f;
    [SerializeField, Tooltip("Mouse look degrees per pixel of movement.")]
    private float lookSensitivity = 0.12f;
    [SerializeField, Tooltip("Higher = the camera glides to a stop more quickly (smooth shots).")]
    private float smoothing = 6f;

    private Camera flyCamera;
    private Camera playerCamera;
    private Vector3 velocity;
    private float yaw;
    private float pitch;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (keyboard.f9Key.wasPressedThisFrame && !UIState.MenuOpen)
        {
            if (UIState.FreeCam) Stop(); else Begin();
        }
        if (UIState.FreeCam)
        {
            Fly(keyboard);
        }
    }

    private void Begin()
    {
        NetworkPlayer local = NetworkPlayer.Local;
        playerCamera = local != null ? local.PlayerCamera : Camera.main;
        if (playerCamera == null)
        {
            return;
        }

        if (flyCamera == null)
        {
            GameObject cameraObject = new GameObject("Trailer Camera");
            flyCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>().enabled = false;
        }
        flyCamera.CopyFrom(playerCamera);
        flyCamera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
        Vector3 angles = playerCamera.transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        velocity = Vector3.zero;

        // Copy the retro effect too, so the trailer looks like the game.
        RetroCameraEffect retro = playerCamera.GetComponent<RetroCameraEffect>();
        if (retro != null)
        {
            RetroCameraEffect copy = flyCamera.GetComponent<RetroCameraEffect>();
            if (copy == null)
            {
                copy = flyCamera.gameObject.AddComponent<RetroCameraEffect>();
            }
            copy.CopySettingsFrom(retro);
        }

        flyCamera.gameObject.SetActive(true);
        flyCamera.depth = playerCamera.depth + 1f;
        UIState.FreeCam = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Stop()
    {
        UIState.FreeCam = false;
        if (flyCamera != null)
        {
            flyCamera.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Fly(Keyboard keyboard)
    {
        if (flyCamera == null || UIState.MenuOpen)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                speed = Mathf.Clamp(speed * (scroll > 0f ? 1.15f : 0.87f), 0.2f, 40f);
            }
        }
        flyCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 wish = Vector3.zero;
        if (keyboard.wKey.isPressed) wish += Vector3.forward;
        if (keyboard.sKey.isPressed) wish += Vector3.back;
        if (keyboard.aKey.isPressed) wish += Vector3.left;
        if (keyboard.dKey.isPressed) wish += Vector3.right;
        wish = flyCamera.transform.TransformDirection(wish);
        if (keyboard.eKey.isPressed) wish += Vector3.up;
        if (keyboard.qKey.isPressed) wish += Vector3.down;

        float currentSpeed = speed;
        if (keyboard.leftShiftKey.isPressed) currentSpeed *= fastMultiplier;
        if (keyboard.leftCtrlKey.isPressed) currentSpeed *= slowMultiplier;

        velocity = Vector3.Lerp(velocity, wish.normalized * currentSpeed, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        flyCamera.transform.position += velocity * Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        if (UIState.FreeCam && !UIState.HideHud)
        {
            UITheme.Apply();
            GUI.Label(new Rect(10, 10, 600, 22), "FREE CAMERA (F9 to return)  speed " + speed.ToString("0.0") + " m/s  -  F1 hides this text");
        }
    }
}
