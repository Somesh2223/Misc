using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// The Chudail, the backwards-feet woman (plan section 4.6). Walks streets and rooftops.
// Lures players away from the group by copying their voices, then attacks people who are alone.
// Tells: footprints that point the wrong way, faint anklet bells, copied voices with an echo.
// Counters: stay grouped, use radio code words, and stand in a salt ward (she cannot cross it).
[RequireComponent(typeof(NavMeshAgent))]
public class Chudail : MonsterBase
{
    [Header("Luring")]
    [SerializeField, Tooltip("A player with no teammate within this distance counts as alone.")]
    private float isolationRadius = 9f;
    [SerializeField, Tooltip("While her target is with the group, she waits about this far away and calls out.")]
    private float lureDistance = 12f;
    [SerializeField, Tooltip("Seconds between copied voices.")]
    private float mimicCooldown = 12f;

    [Header("Tells")]
    [SerializeField, Tooltip("Metres between backwards footprints.")]
    private float footprintSpacing = 1.1f;
    [SerializeField] private float footprintLifetime = 40f;
    [SerializeField, Tooltip("Seconds between anklet bell rings while she moves.")]
    private float bellInterval = 0.7f;
    [SerializeField] private Material visualMaterial;

    private NavMeshAgent agent;
    private AudioSource voiceSource;
    private float mimicTimer;
    private int nextMimicId;

    // Every machine: tells
    private Vector3 lastPrintPosition;
    private bool leftFoot;
    private float bellTimer;
    private Vector3 lastPosition;

    // Every machine: copied voice pieces arriving from the server
    private readonly Dictionary<int, byte[]> incomingMimics = new Dictionary<int, byte[]>();
    private readonly Dictionary<int, int> incomingReceived = new Dictionary<int, int>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        BuildBody();
        BuildVoice();
        lastPrintPosition = transform.position;
        lastPosition = transform.position;

        if (IsServer)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
        else
        {
            agent.enabled = false; // clients just follow the synced position
        }

        base.OnNetworkSpawn();
    }

    // ---------- MonsterBase rules ----------

    protected override bool IsActive()
    {
        return agent.enabled ? agent.isOnNavMesh : true;
    }

    protected override bool CanHearNoise(Vector3 position, float loudness, bool fromWater)
    {
        return true; // she hears everything: footsteps, voices, decoys
    }

    protected override bool CanSeeTarget()
    {
        float range = data.sightRange;
        if (target.Inventory != null && target.Inventory.FlashlightOn)
        {
            range *= 1.5f; // a light draws her in (plan 4.5)
        }
        return HorizontalDistanceToTarget() <= range && HasLineOfSight(target.Position);
    }

    protected override bool CanTrackTarget()
    {
        return HorizontalDistanceToTarget() < data.forgetDistance;
    }

    protected override bool CanAttackTarget()
    {
        if (target.IsDead || SaltWard.Contains(target.Position))
        {
            return false;
        }
        return IsAlone(target) && Vector3.Distance(transform.position, target.Position) <= data.attackRange;
    }

    protected override void PerformAttack()
    {
        Vector3 push = target.Position - transform.position;
        push.y = 0f;
        target.ServerAddImpulse(push.normalized * 4f + Vector3.up * 2f);
        target.Health.TakeDamage(data.attackDamage);
        ScreamClientRpc();
    }

    protected override Vector3 GetWanderPoint()
    {
        Vector3 random = transform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
        if (NavMesh.SamplePosition(random, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;
    }

    protected override void MoveToward(Vector3 point, float speed)
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        // Her target is with the group: do not rush in, wait at a distance and call out instead.
        if (State == MonsterState.Hunt && target != null && !IsAlone(target))
        {
            Vector3 away = transform.position - target.Position;
            away.y = 0f;
            point = target.Position + away.normalized * lureDistance;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                point = hit.position;
            }
        }

        agent.speed = speed;
        if ((agent.destination - point).sqrMagnitude > 0.25f)
        {
            agent.SetDestination(point);
        }
    }

    protected override bool HasReached(Vector3 point)
    {
        return HorizontalDistance(transform.position, point) < 1.2f;
    }

    // ---------- Per frame ----------

    protected override void Update()
    {
        base.Update();
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            UpdateMimic();
        }
        UpdateTells();
    }

    private bool IsAlone(NetworkPlayer player)
    {
        foreach (NetworkPlayer other in NetworkPlayer.All)
        {
            if (other != player && other != null && !other.IsDead
                && Vector3.Distance(other.Position, player.Position) <= isolationRadius)
            {
                return false;
            }
        }
        return true;
    }

    private bool HasLineOfSight(Vector3 targetFeet)
    {
        Vector3 eyes = transform.position + Vector3.up * 1.6f;
        Vector3 targetChest = targetFeet + Vector3.up * 1.2f;
        int mask = ~(1 << 2); // ignore players and trigger zones
        return !Physics.Linecast(eyes, targetChest, mask, QueryTriggerInteraction.Ignore);
    }

    // ---------- Voice mimic (server decides, everyone hears) ----------

    private void UpdateMimic()
    {
        mimicTimer -= Time.deltaTime;
        if (mimicTimer > 0f || target == null || State != MonsterState.Hunt)
        {
            return;
        }
        if (HorizontalDistanceToTarget() > 30f)
        {
            return;
        }
        mimicTimer = mimicCooldown;

        // Everyone except the copied player hears it (they know what they did not say).
        List<ulong> listeners = new List<ulong>();
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId != target.OwnerClientId)
            {
                listeners.Add(clientId);
            }
        }
        if (listeners.Count == 0)
        {
            WhisperClientRpc(); // playing alone: a creepy whisper instead
            return;
        }

        ClientRpcParams rpcParams = default;
        rpcParams.Send = new ClientRpcSendParams { TargetClientIds = listeners.ToArray() };

        if (!VoiceClipStore.TryGetRandom(target.OwnerClientId, out byte[] clip))
        {
            WhisperClientRpc(rpcParams);
            return;
        }

        int mimicId = nextMimicId++;
        int chunks = VoiceClipCodec.ChunkCount(clip.Length);
        for (int i = 0; i < chunks; i++)
        {
            MimicChunkClientRpc(mimicId, i, clip.Length, VoiceClipCodec.Chunk(clip, i), rpcParams);
        }
    }

    [ClientRpc]
    private void MimicChunkClientRpc(int mimicId, int index, int totalBytes, byte[] data, ClientRpcParams rpcParams = default)
    {
        if (!incomingMimics.TryGetValue(mimicId, out byte[] buffer))
        {
            buffer = new byte[totalBytes];
            incomingMimics[mimicId] = buffer;
            incomingReceived[mimicId] = 0;
        }

        int start = index * VoiceClipCodec.ChunkSize;
        if (start + data.Length > buffer.Length)
        {
            return;
        }
        System.Array.Copy(data, 0, buffer, start, data.Length);
        incomingReceived[mimicId] += data.Length;

        if (incomingReceived[mimicId] >= buffer.Length)
        {
            incomingMimics.Remove(mimicId);
            incomingReceived.Remove(mimicId);
            voiceSource.PlayOneShot(VoiceClipCodec.Decode(buffer, "Mimic"));
        }
    }

    [ClientRpc]
    private void WhisperClientRpc(ClientRpcParams rpcParams = default)
    {
        voiceSource.PlayOneShot(ProceduralSounds.Whisper);
    }

    [ClientRpc]
    private void ScreamClientRpc()
    {
        ProceduralSounds.PlayAt(ProceduralSounds.Scream, transform.position, 1f, 40f);
        HighlightMoments.Flag("Chudail attack", 70, transform.position);
    }

    // ---------- Tells (every machine) ----------

    private void UpdateTells()
    {
        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        bool moving = moved.magnitude > 0.2f * Time.deltaTime;
        lastPosition = transform.position;

        if (moving)
        {
            bellTimer -= Time.deltaTime;
            if (bellTimer <= 0f)
            {
                bellTimer = bellInterval;
                voiceSource.PlayOneShot(ProceduralSounds.Bell, 0.35f);
            }
        }

        Vector3 sinceLastPrint = transform.position - lastPrintPosition;
        sinceLastPrint.y = 0f;
        if (sinceLastPrint.magnitude >= footprintSpacing)
        {
            LeaveFootprint(sinceLastPrint.normalized);
            lastPrintPosition = transform.position;
        }
    }

    // Footprints point BACK the way she came: the wrong way.
    private void LeaveFootprint(Vector3 walkDirection)
    {
        Vector3 side = Vector3.Cross(Vector3.up, walkDirection) * (leftFoot ? -0.15f : 0.15f);
        leftFoot = !leftFoot;
        Vector3 origin = transform.position + side + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 2f, ~(1 << 2), QueryTriggerInteraction.Ignore))
        {
            return;
        }

        GameObject print = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(print.GetComponent<Collider>());
        print.name = "Chudail footprint";
        print.transform.position = ground.point + Vector3.up * 0.01f;
        print.transform.rotation = Quaternion.LookRotation(-walkDirection);
        print.transform.localScale = new Vector3(0.12f, 0.01f, 0.26f);
        print.GetComponent<Renderer>().material = MakeMaterial(new Color(0.15f, 0.08f, 0.05f));

        // A toe mark at the "front" of the print, which faces backwards.
        GameObject toes = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(toes.GetComponent<Collider>());
        toes.transform.SetParent(print.transform, false);
        toes.transform.localPosition = new Vector3(0f, 0f, 0.6f);
        toes.transform.localScale = new Vector3(1.3f, 1f, 0.3f);
        toes.GetComponent<Renderer>().material = print.GetComponent<Renderer>().material;

        Destroy(print, footprintLifetime);
    }

    // ---------- Look ----------

    private void BuildBody()
    {
        Color sari = new Color(0.45f, 0.05f, 0.08f);
        Color skin = new Color(0.55f, 0.5f, 0.45f);
        Color hair = new Color(0.03f, 0.03f, 0.03f);

        MakePart(PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.5f, 0.85f, 0.4f), sari);
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0f), new Vector3(0.3f, 0.32f, 0.3f), skin);
        MakePart(PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.18f), new Vector3(0.36f, 1.1f, 0.08f), hair);
        // Feet on backwards: toes point behind her.
        MakePart(PrimitiveType.Cube, new Vector3(-0.12f, 0.05f, -0.12f), new Vector3(0.12f, 0.08f, 0.3f), skin);
        MakePart(PrimitiveType.Cube, new Vector3(0.12f, 0.05f, -0.12f), new Vector3(0.12f, 0.08f, 0.3f), skin);
    }

    private void MakePart(PrimitiveType shape, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = MakeMaterial(color);
    }

    private void BuildVoice()
    {
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.spatialBlend = 1f;
        voiceSource.rolloffMode = AudioRolloffMode.Linear;
        voiceSource.minDistance = 2f;
        voiceSource.maxDistance = 35f;

        // The tell for a copied voice: a slight echo.
        AudioEchoFilter echo = gameObject.AddComponent<AudioEchoFilter>();
        echo.delay = 140f;
        echo.decayRatio = 0.35f;
        echo.wetMix = 0.6f;
        echo.dryMix = 1f;
    }

    private Material MakeMaterial(Color color)
    {
        Material material;
        if (visualMaterial != null)
        {
            material = new Material(visualMaterial);
        }
        else
        {
            bool usingUrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = usingUrp ? Shader.Find("Universal Render Pipeline/Lit") : null;
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
        }
        material.color = color;
        return material;
    }
}
