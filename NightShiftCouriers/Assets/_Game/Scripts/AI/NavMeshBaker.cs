using Unity.AI.Navigation;
using UnityEngine;

// Builds the walking map for ground monsters (the Chudail) on the server when a shift starts.
// The scene has one NavMeshSurface; this just rebuilds it so it matches the current level.
public static class NavMeshBaker
{
    public static void ServerBake()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            return; // scenes without walking monsters do not need one
        }
        surface.BuildNavMesh();
    }
}
