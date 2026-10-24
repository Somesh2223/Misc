using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A storm drain grate in the street (plan M5). The Mud Crawler travels between these under the city.
// Scene object. Tells near a grate: gurgling, bubbles, and the grate rattling just before it grabs.
public class DrainGrate : MonoBehaviour
{
    private static readonly List<DrainGrate> all = new List<DrainGrate>();
    public static IReadOnlyList<DrainGrate> All => all;

    [SerializeField, Tooltip("Unique number per scene so the network can refer to this grate.")]
    private int grateId;
    [SerializeField, Tooltip("The metal plate that shakes when the Mud Crawler is under it.")]
    private Transform plate;

    private Coroutine rattling;
    private Vector3 plateRest;

    public int GrateId => grateId;
    // The middle of the grate, at street level.
    public Vector3 Surface => transform.position;

    private void OnEnable()
    {
        all.Add(this);
        if (plate != null)
        {
            plateRest = plate.localPosition;
        }
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public static DrainGrate FindById(int id)
    {
        foreach (DrainGrate grate in all)
        {
            if (grate.grateId == id)
            {
                return grate;
            }
        }
        return null;
    }

    public static DrainGrate Nearest(Vector3 position)
    {
        DrainGrate best = null;
        float bestDistance = float.MaxValue;
        foreach (DrainGrate grate in all)
        {
            Vector3 offset = grate.transform.position - position;
            offset.y *= 0.5f; // height matters less than distance along the street
            float distance = offset.sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = grate;
            }
        }
        return best;
    }

    // ---------- Tells (every machine) ----------

    public void Rattle(float seconds)
    {
        if (rattling != null)
        {
            StopCoroutine(rattling);
        }
        rattling = StartCoroutine(RattleRoutine(seconds));
        ProceduralSounds.PlayAt(ProceduralSounds.GrateRattle, transform.position, 1f, 25f);
        Bubble(4);
    }

    // A few muddy bubbles rising out of the grate.
    public void Bubble(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bubble.GetComponent<Collider>());
            bubble.name = "Drain bubble";
            bubble.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.05f, Random.Range(-0.3f, 0.3f));
            bubble.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
            Renderer renderer = bubble.GetComponent<Renderer>();
            renderer.material.color = new Color(0.25f, 0.2f, 0.12f);
            StartCoroutine(RiseAndPop(bubble.transform, Random.Range(0.4f, 0.9f)));
        }
    }

    private IEnumerator RiseAndPop(Transform bubble, float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end && bubble != null)
        {
            bubble.position += Vector3.up * 0.4f * Time.deltaTime;
            yield return null;
        }
        if (bubble != null)
        {
            Destroy(bubble.gameObject);
        }
    }

    private IEnumerator RattleRoutine(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            if (plate != null)
            {
                plate.localPosition = plateRest + new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(0f, 0.04f), Random.Range(-0.02f, 0.02f));
                plate.localRotation = Quaternion.Euler(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            }
            yield return new WaitForSeconds(0.04f);
        }
        if (plate != null)
        {
            plate.localPosition = plateRest;
            plate.localRotation = Quaternion.identity;
        }
        rattling = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.3f, 0.1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 0.05f, 0.9f));
    }
}
