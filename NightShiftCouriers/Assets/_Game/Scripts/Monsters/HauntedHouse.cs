using System.Collections.Generic;
using UnityEngine;

// The inside of a haunted house (plan M5): the Brahmarakshas lives here and never leaves.
// Scene object. The house is a box (centre + size, in this object's space); no collider needed.
// Candles inside are lit while the Brahmarakshas is home, and flicker while it hunts: a tell from outside.
public class HauntedHouse : MonoBehaviour
{
    private static readonly List<HauntedHouse> all = new List<HauntedHouse>();
    public static IReadOnlyList<HauntedHouse> All => all;

    [SerializeField, Tooltip("Shown in messages, e.g. \"The Old Haveli\".")]
    private string houseName = "The Old Haveli";
    [SerializeField, Tooltip("Centre of the inside, relative to this object.")]
    private Vector3 center = new Vector3(0f, 2f, 0f);
    [SerializeField, Tooltip("Size of the inside (metres).")]
    private Vector3 size = new Vector3(20f, 4f, 20f);
    [SerializeField, Tooltip("Candle lights. Leave empty to use every Light under this object.")]
    private Light[] candles;

    private float flickerTimer;

    public string HouseName => houseName;

    private void OnEnable()
    {
        all.Add(this);
        if (candles == null || candles.Length == 0)
        {
            candles = GetComponentsInChildren<Light>(true);
        }
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    // ---------- Shape ----------

    public bool Contains(Vector3 position, float margin = 0f)
    {
        Vector3 local = transform.InverseTransformPoint(position) - center;
        Vector3 half = size * 0.5f + Vector3.one * margin;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    // The same point, moved inside the walls if it was outside (height is left alone).
    public Vector3 ClampInside(Vector3 position, float margin = 0.8f)
    {
        Vector3 local = transform.InverseTransformPoint(position) - center;
        Vector3 half = size * 0.5f - Vector3.one * margin;
        local.x = Mathf.Clamp(local.x, -half.x, half.x);
        local.z = Mathf.Clamp(local.z, -half.z, half.z);
        Vector3 clamped = transform.TransformPoint(local + center);
        clamped.y = position.y;
        return clamped;
    }

    public Vector3 RandomPointInside(float margin = 1f)
    {
        Vector3 half = size * 0.5f - Vector3.one * margin;
        Vector3 local = new Vector3(Random.Range(-half.x, half.x), -size.y * 0.5f, Random.Range(-half.z, half.z));
        return transform.TransformPoint(local + center);
    }

    public static HauntedHouse FindContaining(Vector3 position, float margin = 0f)
    {
        foreach (HauntedHouse house in all)
        {
            if (house.Contains(position, margin))
            {
                return house;
            }
        }
        return null;
    }

    public static HauntedHouse Nearest(Vector3 position)
    {
        HauntedHouse best = null;
        float bestDistance = float.MaxValue;
        foreach (HauntedHouse house in all)
        {
            float distance = Vector3.Distance(house.transform.TransformPoint(house.center), position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = house;
            }
        }
        return best;
    }

    // ---------- Candles (every machine) ----------

    private void Update()
    {
        MonsterState? ownerState = null;
        foreach (MonsterBase monster in MonsterBase.All)
        {
            if (monster != null && monster.Kind == MonsterKind.Brahmarakshas && Contains(monster.transform.position, 1f))
            {
                ownerState = monster.State;
                break;
            }
        }

        bool lit = ownerState.HasValue;
        bool hunting = lit && (ownerState.Value == MonsterState.Hunt || ownerState.Value == MonsterState.Attack);
        flickerTimer -= Time.deltaTime;
        foreach (Light candle in candles)
        {
            if (candle == null)
            {
                continue;
            }
            if (!lit)
            {
                candle.enabled = false;
            }
            else if (hunting)
            {
                if (flickerTimer <= 0f)
                {
                    candle.enabled = Random.value > 0.35f;
                }
            }
            else
            {
                candle.enabled = true;
                candle.intensity = Mathf.Lerp(candle.intensity, Random.Range(0.8f, 1.2f), Time.deltaTime * 8f);
            }
        }
        if (flickerTimer <= 0f)
        {
            flickerTimer = Random.Range(0.05f, 0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.9f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(center, size);
    }
}
