using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// A door inside a haunted house (plan M5). Press E to open or close it.
// Closed doors slow the Brahmarakshas: it has to bash them open, unless two couriers hold the door shut.
// Scene object. The open/closed state lives in WorldSync so every player sees the same doors.
// Layout: this object sits in the middle of the doorway, the hinge is a child at one side, and the panel hangs off the hinge.
public class HouseDoor : MonoBehaviour, IServerInteractable
{
    private static readonly List<HouseDoor> all = new List<HouseDoor>();
    public static IReadOnlyList<HouseDoor> All => all;

    [SerializeField, Tooltip("Unique number per scene (0-30) so the network can refer to this door.")]
    private int doorId;
    [SerializeField] private bool startsClosed = true;
    [SerializeField, Tooltip("The panel turns around this.")]
    private Transform hinge;
    [SerializeField, Tooltip("The door panel's collider (blocks players and sight while closed).")]
    private Collider panel;
    [SerializeField] private float openAngle = 95f;
    [SerializeField, Tooltip("Degrees per second.")]
    private float swingSpeed = 240f;
    [SerializeField, Tooltip("Players this close to the doorway count as holding the door.")]
    private float holdRadius = 1.6f;
    [SerializeField, Tooltip("How close a player must be to open or close it.")]
    private float useRange = 3f;

    private NavMeshObstacle obstacle;
    private bool shownClosed;
    private bool firstFrame = true;

    public int DoorId => doorId;
    public bool IsClosed => WorldSync.Instance != null ? WorldSync.Instance.IsDoorClosed(doorId, startsClosed) : startsClosed;

    private void Awake()
    {
        // While closed the door cuts a hole in the NavMesh, so monsters path around it or stop at it.
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(1.6f, 2.4f, 0.3f);
            obstacle.center = new Vector3(0f, 1.2f, 0f);
        }
        obstacle.carving = true;
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // ---------- Using it ----------

    public void ServerInteract(NetworkPlayer player)
    {
        if (player.IsDead || Vector3.Distance(player.Position, transform.position) > useRange)
        {
            return;
        }
        ServerSetClosed(!IsClosed);
    }

    // Returns false if the door could not close (someone is standing in the doorway).
    public bool ServerSetClosed(bool closed)
    {
        if (WorldSync.Instance == null || closed == IsClosed)
        {
            return false;
        }
        if (closed && AnyoneInDoorway())
        {
            return false;
        }
        WorldSync.Instance.ServerSetDoorClosed(doorId, closed, startsClosed);
        return true;
    }

    // Living players near the doorway: two or more can hold it shut against the Brahmarakshas.
    public int HolderCount()
    {
        int count = 0;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead && FlatDistance(player.Position, transform.position) <= holdRadius)
            {
                count++;
            }
        }
        return count;
    }

    public bool AnyPlayerWithin(float distance)
    {
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead && Vector3.Distance(player.Position, transform.position) <= distance)
            {
                return true;
            }
        }
        return false;
    }

    // Which side of the door a point is on (+1 or -1).
    public float SideOf(Vector3 point)
    {
        return Mathf.Sign(Vector3.Dot(point - transform.position, transform.forward));
    }

    // True if a closed door is in the way between two points. Sight checks use this because
    // door panels are on the Ignore Raycast layer (so the NavMesh bake leaves them out).
    public static bool BlocksLine(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float length = direction.magnitude;
        if (length < 0.01f)
        {
            return false;
        }
        Ray ray = new Ray(from, direction / length);
        foreach (HouseDoor door in all)
        {
            if (door.panel != null && door.IsClosed && door.panel.Raycast(ray, out RaycastHit hit, length))
            {
                return true;
            }
        }
        return false;
    }

    private bool AnyoneInDoorway()
    {
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead && FlatDistance(player.Position, transform.position) < 0.7f)
            {
                return true;
            }
        }
        return false;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------- Showing it (every machine) ----------

    private void Update()
    {
        bool closed = IsClosed;
        if (firstFrame)
        {
            firstFrame = false;
            shownClosed = closed;
            SetHingeAngle(closed ? 0f : openAngle);
        }
        else if (closed != shownClosed)
        {
            shownClosed = closed;
            ProceduralSounds.PlayAt(closed ? ProceduralSounds.DoorSlam : ProceduralSounds.DoorCreak, transform.position + Vector3.up, 0.8f, 25f);
        }

        if (hinge != null)
        {
            float current = hinge.localEulerAngles.y;
            float wanted = closed ? 0f : openAngle;
            hinge.localRotation = Quaternion.Euler(0f, Mathf.MoveTowardsAngle(current, wanted, swingSpeed * Time.deltaTime), 0f);
        }

        if (obstacle.enabled != closed)
        {
            obstacle.enabled = closed;
        }
    }

    private void SetHingeAngle(float angle)
    {
        if (hinge != null)
        {
            hinge.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, holdRadius);
    }
}
