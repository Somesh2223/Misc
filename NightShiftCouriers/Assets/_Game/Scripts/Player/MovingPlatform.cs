using UnityEngine;

// Something players can stand on while it moves (a raft). The player controller
// adds this frame's movement so players ride along instead of sliding off.
// Runs early so its movement is known before players move.
[DefaultExecutionOrder(-100)]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("True for rafts (lets monsters and the HUD know you are on a raft).")]
    public bool isRaft = true;

    private Vector3 lastPosition;

    public Vector3 FrameDelta { get; private set; }

    private void OnEnable()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        FrameDelta = transform.position - lastPosition;
        lastPosition = transform.position;
    }
}
