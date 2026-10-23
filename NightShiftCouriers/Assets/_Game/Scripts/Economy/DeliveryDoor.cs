using UnityEngine;

// The door of a delivery address. Press E to knock (some customers care how many times - plan 4.8).
public class DeliveryDoor : MonoBehaviour, IServerInteractable
{
    [SerializeField] private string address = "Address";

    public void ServerInteract(NetworkPlayer player)
    {
        if (DeliveryManager.Instance != null && !player.IsDead)
        {
            DeliveryManager.Instance.ServerKnock(address, transform.position);
        }
    }
}
