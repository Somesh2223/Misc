using UnityEngine;

// The depot extraction area (plan 4.3). At dawn, players outside it are "lost": they drop
// what they carry and lose their items.
[RequireComponent(typeof(BoxCollider))]
public class ExtractionZone : MonoBehaviour
{
    private static ExtractionZone instance;
    private BoxCollider zone;

    private void Awake()
    {
        instance = this;
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
    }

    // True if the position is inside the zone (or if the scene has no zone at all).
    public static bool Contains(Vector3 position)
    {
        if (instance == null)
        {
            return true;
        }
        return instance.zone.bounds.Contains(position + Vector3.up * 0.5f);
    }
}
