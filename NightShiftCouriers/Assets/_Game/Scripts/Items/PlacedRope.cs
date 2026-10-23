using Unity.Netcode;
using UnityEngine;

// A rope hanging from a ledge. Its collider is Climbable, so players can climb it (Space to grab).
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Climbable))]
public class PlacedRope : NetworkBehaviour
{
    private readonly NetworkVariable<Vector3> top = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> bottom = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField, Tooltip("The thin cylinder that shows the rope.")]
    private Transform visual;

    private Vector3 pendingTop;
    private Vector3 pendingBottom;

    public void ServerInit(Vector3 topPoint, Vector3 bottomPoint)
    {
        pendingTop = topPoint;
        pendingBottom = bottomPoint;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            top.Value = pendingTop;
            bottom.Value = pendingBottom;
        }

        // Stretch the collider and the visual between the two points (on every machine).
        float length = top.Value.y - bottom.Value.y;
        transform.position = top.Value;

        BoxCollider box = GetComponent<BoxCollider>();
        box.size = new Vector3(0.4f, length, 0.4f); // wider than the rope so it is easy to grab
        box.center = new Vector3(0f, -length / 2f, 0f);

        if (visual != null)
        {
            visual.localPosition = new Vector3(0f, -length / 2f, 0f);
            visual.localScale = new Vector3(0.06f, length / 2f, 0.06f); // a Unity cylinder is 2 m tall
        }
    }
}
