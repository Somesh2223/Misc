using UnityEngine;

// On-screen labels in the world:
//   - Delivery addresses within range (Navigator role sees them from much further: plan 4.7)
//   - Monsters near a Lookout (plan 4.7), or every monster for a Signal Ghost (plan 4.10)
//   - The Signal Ghost's controls and cooldowns
public class WorldMarkersHUD : MonoBehaviour
{
    [SerializeField] private float deliveryRange = 25f;
    [SerializeField] private float navigatorDeliveryRange = 150f;
    [SerializeField] private float lookoutMonsterRange = 35f;

    private MonsterBase[] monsters = new MonsterBase[0];
    private float refreshTimer;

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 1f;
            monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
        }
    }

    private void OnGUI()
    {
        if (UIState.HideHud)
        {
            return; // F1: screenshot mode
        }
        NetworkPlayer local = NetworkPlayer.Local;
        Camera view = local != null ? local.PlayerCamera : null;
        if (view == null || CampaignManager.Instance == null || CampaignManager.Instance.Phase != ShiftPhase.Shift)
        {
            return;
        }

        bool ghost = local.IsDead;
        RolePassive passive = local.Passive;

        float range = passive == RolePassive.Navigator ? navigatorDeliveryRange : deliveryRange;
        foreach (DeliveryPoint point in DeliveryPoint.All)
        {
            DrawLabel(view, point.transform.position + Vector3.up * 2f, point.Address, range, Color.green);
        }

        if (ghost || passive == RolePassive.Lookout)
        {
            float monsterRange = ghost ? 1000f : lookoutMonsterRange;
            foreach (MonsterBase monster in monsters)
            {
                if (monster != null)
                {
                    DrawLabel(view, monster.transform.position + Vector3.up * 2.2f, "! " + monster.name.Replace("(Clone)", ""),
                        monsterRange, Color.red);
                }
            }
        }

        if (ghost)
        {
            SignalGhost signal = local.GetComponent<SignalGhost>();
            string flicker = signal.FlickerCooldownLeft > 0f ? Mathf.CeilToInt(signal.FlickerCooldownLeft) + " s" : "ready";
            string bell = signal.BellCooldownLeft > 0f ? Mathf.CeilToInt(signal.BellCooldownLeft) + " s" : "ready";
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 16;
            GUI.Box(new Rect(Screen.width / 2f - 260f, 60f, 520f, 70f),
                "SIGNAL GHOST - fly with WASD, Space/Ctrl up/down\n"
                + "E: flicker a streetlight (" + flicker + ")    Right mouse: ring a bell (" + bell + ")\n"
                + "Speak through radios. Help your crew... or don't.", style);
        }
    }

    private static void DrawLabel(Camera view, Vector3 worldPosition, string text, float range, Color color)
    {
        float distance = Vector3.Distance(view.transform.position, worldPosition);
        if (distance > range)
        {
            return;
        }

        Vector3 screen = view.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f)
        {
            return; // behind the camera
        }

        GUI.color = color;
        GUI.Label(new Rect(screen.x - 80f, Screen.height - screen.y - 10f, 160f, 22f),
            text + "  " + Mathf.RoundToInt(distance) + " m");
        GUI.color = Color.white;
    }
}
