using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A streetlight a Signal Ghost can possess to flicker it (plan 4.10): to guide the living... or mislead them.
// Scene object; the flicker is sent to everyone by GhostWorld.
public class Streetlight : MonoBehaviour
{
    private static readonly List<Streetlight> all = new List<Streetlight>();

    [SerializeField, Tooltip("Unique number per scene so the network can refer to this light.")]
    private int lightId;
    [SerializeField] private Light lamp;

    private Coroutine flickering;

    public int LightId => lightId;

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public static Streetlight FindById(int id)
    {
        foreach (Streetlight light in all)
        {
            if (light.lightId == id)
            {
                return light;
            }
        }
        return null;
    }

    // The light closest to where a ghost is looking (within `width` metres of the aim line).
    public static Streetlight FindNearestToRay(Vector3 origin, Vector3 direction, float maxDistance, float width)
    {
        Streetlight best = null;
        float bestDistance = width;
        foreach (Streetlight light in all)
        {
            Vector3 toLight = light.transform.position - origin;
            float along = Vector3.Dot(toLight, direction);
            if (along < 0f || along > maxDistance)
            {
                continue;
            }
            float offLine = (toLight - direction * along).magnitude;
            if (offLine < bestDistance)
            {
                bestDistance = offLine;
                best = light;
            }
        }
        return best;
    }

    public void Flicker(float seconds)
    {
        if (flickering != null)
        {
            StopCoroutine(flickering);
        }
        flickering = StartCoroutine(FlickerRoutine(seconds));
    }

    private IEnumerator FlickerRoutine(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            lamp.enabled = !lamp.enabled;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.25f));
        }
        lamp.enabled = true;
        flickering = null;
    }
}
