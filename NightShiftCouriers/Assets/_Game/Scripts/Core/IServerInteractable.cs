// Something a player can use with E. Runs on the SERVER (it may change game state).
// Example: picking up an item, building a raft, pulling up a climbing teammate.
public interface IServerInteractable
{
    void ServerInteract(NetworkPlayer player);
}
