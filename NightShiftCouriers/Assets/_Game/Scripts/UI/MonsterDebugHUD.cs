using UnityEngine;

// Debug overlay: shows each monster's current AI state at the top right.
public class MonsterDebugHUD : MonoBehaviour
{
    private MonsterBase[] monsters;
    private float refreshTimer;

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
            refreshTimer = 2f;
        }
    }

    private void OnGUI()
    {
        if (monsters == null)
        {
            return;
        }

        string text = "";
        foreach (MonsterBase monster in monsters)
        {
            if (monster != null)
            {
                text += monster.name + ": " + monster.State + "\n";
            }
        }
        GUI.Label(new Rect(Screen.width - 260, 20, 240, 100), text);
    }
}
