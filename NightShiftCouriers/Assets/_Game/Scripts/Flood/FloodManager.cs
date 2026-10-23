using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// The flood clock (section 4.3). SERVER-authoritative: only the server runs the timers.
// Clients receive the stage and the water height and move their local water to match.
// When the last stage runs out it is dawn: the shift ends and players outside the depot extraction are lost.
public class FloodManager : NetworkBehaviour
{
    public static FloodManager Instance { get; private set; }

    private FloodSettings settings => GameNetworkConfig.Instance.FloodSettings;

    [Header("Debug")]
    [SerializeField, Tooltip("Host only: press N to jump to the next stage while testing.")]
    private bool allowDebugKeys = true;

    [SerializeField, Tooltip("How quickly clients catch up to the server's water height.")]
    private float clientFollowSpeed = 8f;

    private readonly NetworkVariable<int> stage = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> waterHeight = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> dawn = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> secondsUntilRise = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<WaterVolume> floodWaters = new List<WaterVolume>();
    private float stageTimer;
    private bool warned;

    public int StageIndex => stage.Value;
    public int StageCount => settings.stages.Length;
    public string StageName => settings.stages[Mathf.Clamp(stage.Value, 0, settings.stages.Length - 1)].name;
    public bool IsLastStage => stage.Value >= settings.stages.Length - 1;
    // Seconds until the next rise (or, on the last stage, until dawn).
    public float SecondsUntilNextRise => secondsUntilRise.Value;
    public bool IsDawn => dawn.Value;
    public float CurrentWaterHeight => waterHeight.Value;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        foreach (WaterVolume water in WaterVolume.All)
        {
            if (water.FollowsFlood)
            {
                floodWaters.Add(water);
            }
        }

        if (IsServer)
        {
            stage.Value = 0;
            stageTimer = settings.stages[0].duration;
            waterHeight.Value = settings.stages[0].waterHeight;
            secondsUntilRise.Value = Mathf.CeilToInt(stageTimer);
            SetWaterHeightInstantly(waterHeight.Value);
        }
        else
        {
            SetWaterHeightInstantly(waterHeight.Value);
        }

        stage.OnValueChanged += OnStageChanged;
        GameEvents.RaiseFloodStageChanged(stage.Value);
    }

    public override void OnNetworkDespawn()
    {
        stage.OnValueChanged -= OnStageChanged;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnStageChanged(int previous, int current)
    {
        GameEvents.RaiseFloodStageChanged(current);
    }

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            ServerUpdate();
        }
        else
        {
            FollowServerHeight();
        }
    }

    // ---------- Server ----------

    private void ServerUpdate()
    {
        if (allowDebugKeys && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            StartNextStage();
        }

        MoveWaterTowardTarget();

        if (dawn.Value)
        {
            return;
        }

        stageTimer -= Time.deltaTime;
        secondsUntilRise.Value = Mathf.Max(0, Mathf.CeilToInt(stageTimer));

        if (!warned && stageTimer <= settings.warningSeconds)
        {
            warned = true;
            if (IsLastStage)
            {
                DawnWarningClientRpc();
            }
            else
            {
                WarningClientRpc(stage.Value + 1, Mathf.Max(0f, stageTimer));
            }
        }

        if (stageTimer <= 0f)
        {
            if (IsLastStage)
            {
                dawn.Value = true;
            }
            else
            {
                StartNextStage();
            }
        }
    }

    // A cursed package can make the water rise early (plan 4.9).
    public void ServerAdvanceStage()
    {
        if (IsServer)
        {
            WarningClientRpc(stage.Value + 1, 0f);
            StartNextStage();
        }
    }

    private void StartNextStage()
    {
        if (IsLastStage)
        {
            return;
        }

        stage.Value++;
        stageTimer = settings.stages[stage.Value].duration;
        warned = false;
    }

    private void MoveWaterTowardTarget()
    {
        float target = settings.stages[stage.Value].waterHeight;
        float current = waterHeight.Value;
        if (Mathf.Approximately(current, target))
        {
            return;
        }

        float next = Mathf.MoveTowards(current, target, settings.riseSpeed * Time.deltaTime);
        waterHeight.Value = next;
        SetWaterHeightInstantly(next);
    }

    [ClientRpc]
    private void WarningClientRpc(int nextStage, float secondsLeft)
    {
        GameEvents.RaiseFloodWarning(nextStage, secondsLeft);
    }

    [ClientRpc]
    private void DawnWarningClientRpc()
    {
        MessageFeed.Show("Dawn is coming! Get back to the depot extraction or be left behind.", 8f);
    }

    // ---------- Clients ----------

    private void FollowServerHeight()
    {
        foreach (WaterVolume water in floodWaters)
        {
            Vector3 position = water.transform.position;
            position.y = Mathf.Lerp(position.y, waterHeight.Value, clientFollowSpeed * Time.deltaTime);
            water.transform.position = position;
        }
    }

    private void SetWaterHeightInstantly(float height)
    {
        foreach (WaterVolume water in floodWaters)
        {
            Vector3 position = water.transform.position;
            position.y = height;
            water.transform.position = position;
        }
    }
}
