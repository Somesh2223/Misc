using UnityEngine;

// Debug HUD for the flood. Per pillar 2 the real game has no on-screen timer;
// set showTimer to false later and only the warning text stays.
public class FloodHUD : MonoBehaviour
{
    [SerializeField] private bool showTimer = true;
    [SerializeField, Tooltip("Seconds the warning message stays on screen.")]
    private float warningDisplaySeconds = 8f;

    private float warningTimer;
    private string warningText = "";

    private void OnEnable()
    {
        GameEvents.FloodWarning += OnFloodWarning;
        GameEvents.FloodStageChanged += OnStageChanged;
    }

    private void OnDisable()
    {
        GameEvents.FloodWarning -= OnFloodWarning;
        GameEvents.FloodStageChanged -= OnStageChanged;
    }

    private void OnFloodWarning(int nextStage, float secondsLeft)
    {
        warningText = "SIREN! The water is about to rise...";
        warningTimer = warningDisplaySeconds;
    }

    private void OnStageChanged(int stage)
    {
        if (stage > 0)
        {
            warningText = "The water is rising!";
            warningTimer = warningDisplaySeconds;
        }
    }

    private void Update()
    {
        warningTimer -= Time.deltaTime;
    }

    private void OnGUI()
    {
        FloodManager flood = FloodManager.Instance;
        if (flood == null)
        {
            return;
        }

        if (showTimer)
        {
            string text = "Flood stage " + (flood.StageIndex + 1) + "/" + flood.StageCount + ": " + flood.StageName
                          + "\nWater height: " + flood.CurrentWaterHeight.ToString("0.00") + " m";
            if (flood.IsDawn)
            {
                text += "\nDAWN";
            }
            else if (flood.IsLastStage)
            {
                text += "\nDawn in: " + Mathf.CeilToInt(flood.SecondsUntilNextRise) + " s";
            }
            else
            {
                text += "\nNext rise in: " + Mathf.CeilToInt(flood.SecondsUntilNextRise) + " s   (host: N = skip)";
            }
            GUI.Label(new Rect(20, 20, 400, 80), text);
        }

        if (warningTimer > 0f)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 26;
            style.alignment = TextAnchor.UpperCenter;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, 60, Screen.width, 60), warningText, style);
        }
    }
}
