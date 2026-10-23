using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// A circle of salt some monsters cannot cross (plan 4.5; blocks the Chudail).
// It also cuts a hole in the navigation mesh so walking monsters path around it.
[RequireComponent(typeof(NavMeshObstacle))]
public class SaltWard : NetworkBehaviour
{
    private static readonly List<SaltWard> active = new List<SaltWard>();

    [SerializeField] private float radius = 2.5f;

    private float pendingSeconds = 60f;
    private float timeLeft;

    public static bool Contains(Vector3 position)
    {
        foreach (SaltWard ward in active)
        {
            Vector3 offset = position - ward.transform.position;
            offset.y = 0f;
            if (offset.magnitude <= ward.radius && Mathf.Abs(position.y - ward.transform.position.y) < 3f)
            {
                return true;
            }
        }
        return false;
    }

    public void ServerInit(float seconds)
    {
        pendingSeconds = seconds;
    }

    public override void OnNetworkSpawn()
    {
        active.Add(this);
        timeLeft = pendingSeconds;

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Capsule;
        obstacle.radius = radius;
        obstacle.height = 2f;
        obstacle.carving = true;

        BuildRing();
    }

    public override void OnNetworkDespawn()
    {
        active.Remove(this);
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
        {
            return;
        }
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            NetworkObject.Despawn(true);
        }
    }

    // A ring of small white grains so players can see the ward.
    private void BuildRing()
    {
        const int grains = 24;
        for (int i = 0; i < grains; i++)
        {
            float angle = i * Mathf.PI * 2f / grains;
            GameObject grain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(grain.GetComponent<Collider>());
            grain.transform.SetParent(transform, false);
            grain.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);
            grain.transform.localScale = new Vector3(0.25f, 0.05f, 0.25f);
        }
    }
}
