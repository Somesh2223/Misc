using UnityEngine;

// Debug overlay: shows each monster's current AI state at the bottom right,
// plus what the AI Director is thinking (host only). F3 hides it.
public class MonsterDebugHUD : MonoBehaviour
{
    [SerializeField, Tooltip("Start with the overlay visible.")]
    private bool visible = true;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
        {
            visible = !visible;
        }
    }

    private void OnGUI()
    {
        if (!visible || UIState.HideHud)
        {
            return;
        }

        string text = "";
        foreach (MonsterBase monster in MonsterBase.All)
        {
            if (monster != null)
            {
                text += monster.DisplayName + ": " + monster.State + "\n";
            }
        }

        AIDirector director = AIDirector.Instance;
        if (director != null && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            text += "\nDirector: " + director.CurrentPhase
                    + "\nStress: " + Mathf.RoundToInt(director.Stress)
                    + "   Budget: " + director.Spent + "/" + director.Budget
                    + "\nQuiet for: " + Mathf.RoundToInt(director.SecondsSinceSomethingHappened) + " s";
        }

        GUI.Label(new Rect(Screen.width - 260, Screen.height - 240, 240, 200), text); // bottom right, under the crew panel
    }
}
