// Something a player can use with E that only changes their own screen (e.g. opening the shop).
// Runs on the player's own machine, never on the server.
public interface ILocalInteractable
{
    void LocalInteract();
}
