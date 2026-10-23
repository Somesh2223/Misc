using UnityEngine;

// Flags "key moments" for the body cam (plan 4.11): deaths, attacks, big drops, cursed packages...
// Called on each machine; only moments near the local player are kept.
public static class HighlightMoments
{
    private const float NearbyDistance = 30f;

    public static void Flag(string label, int score)
    {
        if (BodyCam.Local != null)
        {
            BodyCam.Local.AddMoment(label, score);
        }
    }

    public static void Flag(string label, int score, Vector3 where)
    {
        NetworkPlayer local = NetworkPlayer.Local;
        if (local != null && Vector3.Distance(local.transform.position, where) > NearbyDistance)
        {
            return;
        }
        Flag(label, score);
    }
}
