using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// The AI Director (plan M5). Runs on the SERVER, on the same object as ShiftSpawner.
// It keeps a "stress" number for the crew and moves through three phases:
//   Build Up - spend the monster budget at hidden spawn points, play scares if it gets too quiet
//   Peak     - the crew is in trouble: stop adding monsters
//   Relax    - a breather after a peak, then back to Build Up
// The budget grows with every flood rise. Only spawn points marked "director only" are used.
public class AIDirector : MonoBehaviour
{
    public enum Phase { Off, BuildUp, Peak, Relax }

    [SerializeField] private DirectorSettings settings;

    private static readonly WorldSound[] ScareSounds =
    {
        WorldSound.Whisper, WorldSound.Laugh, WorldSound.DogBark, WorldSound.TempleBell,
        WorldSound.DoorSlam, WorldSound.Bell, WorldSound.Gurgle, WorldSound.Siren
    };

    private readonly Dictionary<MonsterSpawnPoint, NetworkObject> spawnedAt = new Dictionary<MonsterSpawnPoint, NetworkObject>();
    private Phase phase = Phase.Off;
    private float stress;
    private float phaseTimer;
    private float spawnTimer;
    private float checkTimer;
    private float quietTimer;
    private float shiftTime;

    public static AIDirector Instance { get; private set; }
    public Phase CurrentPhase => phase;
    public float Stress => stress;
    public int Budget => settings == null ? 0 : settings.baseBudget + settings.budgetPerFloodStage * FloodStage;
    public int Spent { get; private set; }
    public float SecondsSinceSomethingHappened => quietTimer;

    private static int FloodStage => FloodManager.Instance != null ? FloodManager.Instance.StageIndex : 0;
    private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.NoiseMade += OnNoise;
        GameEvents.PlayerDamaged += OnDamage;
        GameEvents.MonsterStateChanged += OnMonsterState;
    }

    private void OnDisable()
    {
        GameEvents.NoiseMade -= OnNoise;
        GameEvents.PlayerDamaged -= OnDamage;
        GameEvents.MonsterStateChanged -= OnMonsterState;
    }

    // Called by ShiftSpawner once the shift's own monsters are out.
    public void ServerBeginShift()
    {
        spawnedAt.Clear();
        stress = 0f;
        quietTimer = 0f;
        shiftTime = 0f;
        spawnTimer = settings != null ? settings.graceSeconds : 0f;
        phase = settings != null ? Phase.BuildUp : Phase.Off;
    }

    // ---------- Listening (server) ----------

    private void OnNoise(Vector3 position, float loudness, bool fromWater)
    {
        if (phase == Phase.Off || !IsServer || DistanceToNearestCourier(position) > settings.noiseRadius)
        {
            return;
        }
        stress += loudness * settings.stressPerLoudness * 0.1f; // footsteps are constant; only loud noises add up
    }

    private void OnDamage(float amount)
    {
        if (phase == Phase.Off || !IsServer)
        {
            return;
        }
        stress += amount * settings.stressPerDamage;
        quietTimer = 0f;
    }

    private void OnMonsterState(MonsterBase monster, MonsterState state)
    {
        if (phase != Phase.Off && (state == MonsterState.Hunt || state == MonsterState.Attack))
        {
            quietTimer = 0f;
        }
    }

    // ---------- Thinking (server) ----------

    private void Update()
    {
        if (!IsServer || phase == Phase.Off || settings == null)
        {
            return;
        }
        if (CampaignManager.Instance == null || CampaignManager.Instance.Phase != ShiftPhase.Shift)
        {
            phase = Phase.Off; // the night is over; ServerBeginShift starts it again
            return;
        }

        float dt = Time.deltaTime;
        shiftTime += dt;
        quietTimer += dt;
        phaseTimer -= dt;
        spawnTimer -= dt;

        if (AnyMonsterHunting())
        {
            stress += settings.stressPerHuntSecond * dt;
        }
        else
        {
            stress -= settings.stressDecayPerSecond * dt;
        }
        stress = Mathf.Clamp(stress, 0f, 100f);

        UpdatePhase();

        checkTimer -= dt;
        if (checkTimer > 0f)
        {
            return;
        }
        checkTimer = settings.checkInterval;

        ForgetDeadMonsters();

        if (phase == Phase.BuildUp && spawnTimer <= 0f)
        {
            spawnTimer = settings.spawnInterval;
            TrySpawn();
        }

        float scareAfter = phase == Phase.Relax ? settings.quietSecondsBeforeScare * 2f : settings.quietSecondsBeforeScare;
        if (phase != Phase.Peak && quietTimer >= scareAfter)
        {
            PlayScare();
        }
    }

    private void UpdatePhase()
    {
        switch (phase)
        {
            case Phase.BuildUp:
                if (stress >= settings.peakStress)
                {
                    phase = Phase.Peak;
                }
                break;
            case Phase.Peak:
                if (stress < settings.peakStress * 0.5f)
                {
                    phase = Phase.Relax;
                    phaseTimer = settings.relaxSeconds;
                }
                break;
            case Phase.Relax:
                if (phaseTimer <= 0f)
                {
                    phase = Phase.BuildUp;
                    spawnTimer = Mathf.Min(spawnTimer, settings.spawnInterval * 0.5f);
                }
                break;
        }
    }

    private void TrySpawn()
    {
        if (ShiftSpawner.Instance == null || spawnedAt.Count >= settings.maxDirectorMonsters)
        {
            return;
        }
        int left = Budget - Spent;
        if (left <= 0)
        {
            return;
        }

        List<MonsterSpawnPoint> choices = new List<MonsterSpawnPoint>();
        foreach (MonsterSpawnPoint point in FindObjectsByType<MonsterSpawnPoint>(FindObjectsSortMode.None))
        {
            int cost = CostOf(point.kind);
            if (!point.directorOnly || spawnedAt.ContainsKey(point) || cost <= 0 || cost > left)
            {
                continue;
            }
            if (point.kind == MonsterKind.JalPishach && point.homeWater == null)
            {
                continue;
            }
            if (IsHiddenFromCrew(point.transform.position))
            {
                choices.Add(point);
            }
        }
        if (choices.Count == 0)
        {
            return;
        }

        MonsterSpawnPoint chosen = choices[Random.Range(0, choices.Count)];
        NetworkObject spawned = ShiftSpawner.Instance.ServerSpawnMonsterAt(chosen);
        if (spawned != null)
        {
            spawnedAt[chosen] = spawned;
            Debug.Log("AI Director sent a " + chosen.kind + " (stress " + Mathf.RoundToInt(stress) + ", budget " + Budget + ").");
        }
    }

    // Spent points come back when a director monster is gone (despawned at the depot, or killed one day).
    private void ForgetDeadMonsters()
    {
        List<MonsterSpawnPoint> gone = new List<MonsterSpawnPoint>();
        foreach (KeyValuePair<MonsterSpawnPoint, NetworkObject> pair in spawnedAt)
        {
            if (pair.Key == null || pair.Value == null || !pair.Value.IsSpawned)
            {
                gone.Add(pair.Key);
            }
        }
        foreach (MonsterSpawnPoint point in gone)
        {
            spawnedAt.Remove(point);
        }

        int spent = 0;
        foreach (MonsterSpawnPoint point in spawnedAt.Keys)
        {
            spent += CostOf(point.kind);
        }
        Spent = spent;
    }

    private int CostOf(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Chudail: return settings.chudailCost;
            case MonsterKind.Vetala: return settings.vetalaCost;
            case MonsterKind.MudCrawler: return settings.mudCrawlerCost;
            case MonsterKind.JalPishach: return settings.jalPishachCost;
            case MonsterKind.Brahmarakshas: return settings.brahmarakshasCost;
        }
        return 0;
    }

    // A sound from somewhere off to the side of a random courier, to keep them on edge.
    private void PlayScare()
    {
        quietTimer = 0f;
        if (WorldSync.Instance == null)
        {
            return;
        }
        List<NetworkPlayer> living = new List<NetworkPlayer>();
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead)
            {
                living.Add(player);
            }
        }
        if (living.Count == 0)
        {
            return;
        }

        NetworkPlayer chosen = living[Random.Range(0, living.Count)];
        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(settings.scareDistance.x, settings.scareDistance.y);
        Vector3 where = chosen.Position + new Vector3(offset.x, 1.5f, offset.y);
        WorldSound sound = ScareSounds[Random.Range(0, ScareSounds.Length)];
        WorldSync.Instance.ServerPlaySound(sound, where, 0.8f, settings.scareDistance.y + 15f);
    }

    // ---------- Helpers ----------

    // Hidden = far from every courier, and not where any of them is looking with a clear view.
    private bool IsHiddenFromCrew(Vector3 point)
    {
        Vector3 pointChest = point + Vector3.up * 1.2f;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player == null || player.IsDead)
            {
                continue;
            }
            Vector3 eyes = player.Position + Vector3.up * 1.6f;
            if (Vector3.Distance(player.Position, point) < settings.minSpawnDistance)
            {
                return false;
            }
            Vector3 toPoint = pointChest - eyes;
            bool inCone = Vector3.Angle(player.Forward, toPoint) <= settings.viewConeHalfAngle;
            bool clearLine = !Physics.Linecast(eyes, pointChest, ~(1 << 2), QueryTriggerInteraction.Ignore);
            if (inCone && clearLine)
            {
                return false;
            }
        }
        return true;
    }

    private static float DistanceToNearestCourier(Vector3 position)
    {
        float best = float.MaxValue;
        foreach (NetworkPlayer player in NetworkPlayer.All)
        {
            if (player != null && !player.IsDead)
            {
                best = Mathf.Min(best, Vector3.Distance(player.Position, position));
            }
        }
        return best;
    }

    private static bool AnyMonsterHunting()
    {
        foreach (MonsterBase monster in MonsterBase.All)
        {
            if (monster != null && (monster.State == MonsterState.Hunt || monster.State == MonsterState.Attack))
            {
                return true;
            }
        }
        return false;
    }
}
