using UnityEngine;

// The depot shop (plan 4.15). Press E on it to open the shop window.
// Bought raft parts appear at the drop point next to it.
public class ShopTerminal : MonoBehaviour, ILocalInteractable
{
    [SerializeField, Tooltip("Where bought raft parts appear.")]
    private Transform dropPoint;

    private static ShopTerminal instance;

    public static Vector3 DropPoint
    {
        get
        {
            if (instance == null)
            {
                return Vector3.zero;
            }
            return instance.dropPoint != null ? instance.dropPoint.position : instance.transform.position + Vector3.up;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    public void LocalInteract()
    {
        ShopWindow.Open();
    }
}
