using System.Collections.Generic;
using UnityEngine;

// A flat body of water. The surface height is this object's Y position
// (the Flood system will move it up and down later).
// The Collider only defines how far the water spreads in X and Z.
[RequireComponent(typeof(Collider))]
public class WaterVolume : MonoBehaviour
{
    private static readonly List<WaterVolume> allWater = new List<WaterVolume>();

    private Collider area;

    public float SurfaceHeight => transform.position.y;

    private void Awake()
    {
        area = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        allWater.Add(this);
    }

    private void OnDisable()
    {
        allWater.Remove(this);
    }

    // How deep the water is at a world position's X/Z, measured up from groundY.
    // Returns 0 if there is no water there.
    public static float GetDepthAt(Vector3 position, float groundY)
    {
        float deepest = 0f;

        foreach (WaterVolume water in allWater)
        {
            Bounds bounds = water.area.bounds;
            bool insideX = position.x >= bounds.min.x && position.x <= bounds.max.x;
            bool insideZ = position.z >= bounds.min.z && position.z <= bounds.max.z;

            if (insideX && insideZ)
            {
                float depth = water.SurfaceHeight - groundY;
                if (depth > deepest)
                {
                    deepest = depth;
                }
            }
        }

        return deepest;
    }
}
