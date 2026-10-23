using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// The flood clock. Moves the water up through stages and raises events for the HUD, audio and monsters.
// Single-player for now; in Milestone 2 only the server will run this.
public class FloodManager : MonoBehaviour
{
    [SerializeField] private FloodSettings settings;

    [SerializeField, Tooltip("Water volumes that rise with the flood.")]
    private List<WaterVolume> floodWaters = new List<WaterVolume>();

    [Header("Debug")]
    [SerializeField, Tooltip("Press N to jump to the next stage while testing.")]
    private bool allowDebugKeys = true;

    private int stageIndex;
    private float stageTimer;
    private bool warned;
    private bool rising;

    public int StageIndex => stageIndex;
    public int StageCount => settings.stages.Length;
    public string StageName => settings.stages[stageIndex].name;
    public bool IsLastStage => stageIndex >= settings.stages.Length - 1;
    public bool IsRising => rising;

    // Seconds until the next rise starts (or 0 on the last stage).
    public float SecondsUntilNextRise
    {
        get
        {
            if (IsLastStage)
            {
                return 0f;
            }
            return Mathf.Max(0f, stageTimer);
        }
    }

    public float CurrentWaterHeight
    {
        get
        {
            if (floodWaters.Count == 0)
            {
                return 0f;
            }
            return floodWaters[0].SurfaceHeight;
        }
    }

    private void Start()
    {
        stageIndex = 0;
        stageTimer = settings.stages[0].duration;
        SetWaterHeightInstantly(settings.stages[0].waterHeight);
        GameEvents.RaiseFloodStageChanged(stageIndex);
    }

    private void Update()
    {
        if (allowDebugKeys && Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            StartNextStage();
        }

        MoveWaterTowardTarget();

        if (IsLastStage)
        {
            return;
        }

        stageTimer -= Time.deltaTime;

        if (!warned && stageTimer <= settings.warningSeconds)
        {
            warned = true;
            GameEvents.RaiseFloodWarning(stageIndex + 1, Mathf.Max(0f, stageTimer));
        }

        if (stageTimer <= 0f)
        {
            StartNextStage();
        }
    }

    private void StartNextStage()
    {
        if (IsLastStage)
        {
            return;
        }

        stageIndex++;
        stageTimer = settings.stages[stageIndex].duration;
        warned = false;
        GameEvents.RaiseFloodStageChanged(stageIndex);
    }

    private void MoveWaterTowardTarget()
    {
        float target = settings.stages[stageIndex].waterHeight;
        rising = false;

        foreach (WaterVolume water in floodWaters)
        {
            Vector3 position = water.transform.position;
            if (!Mathf.Approximately(position.y, target))
            {
                rising = true;
                position.y = Mathf.MoveTowards(position.y, target, settings.riseSpeed * Time.deltaTime);
                water.transform.position = position;
            }
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
