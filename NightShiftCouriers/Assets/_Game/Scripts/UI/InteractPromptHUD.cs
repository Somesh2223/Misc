using UnityEngine;

// A small crosshair and "[E] Open door" style hints for whatever you are looking at (plan M5 UI polish).
// Uses the same ray as PlayerGrabber, so the hint matches what E will actually do.
// Key names follow the player's own key bindings.
public class InteractPromptHUD : MonoBehaviour
{
    [SerializeField, Tooltip("Must match PlayerGrabber's grab range.")]
    private float range = 3f;
    [SerializeField] private bool showCrosshair = true;

    private string prompt = "";
    private string warning = "";

    private void Update()
    {
        prompt = "";
        warning = "";
        NetworkPlayer local = NetworkPlayer.Local;
        if (local == null || local.IsDead || UIState.InputBlocked)
        {
            return;
        }

        if (local.IsHeldDown)
        {
            warning = "Something has you by the ankles! A teammate must press " + Key("interact") + " on you.";
            return;
        }

        PlayerGrabber grabber = local.GetComponent<PlayerGrabber>();
        if (grabber != null && grabber.IsHolding)
        {
            prompt = HoldingPrompt(grabber.HeldPackage);
            return;
        }

        Camera view = local.PlayerCamera;
        if (view == null)
        {
            return;
        }
        Transform eye = view.transform;
        if (Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            string action = DescribeTarget(hit.collider, local);
            if (action != null)
            {
                prompt = "[" + Key("interact") + "] " + action;
            }
        }
    }

    private static string HoldingPrompt(Package package)
    {
        string text = "[" + Key("interact") + "] Drop";
        if (package != null && package.CanBeThrown)
        {
            text += "    [" + Key("throw") + "] Throw";
        }
        if (package != null && package.IsCursed && !package.IsScreaming)
        {
            text += "    [" + Key("open") + "] Open it (cursed!)";
        }
        return text;
    }

    // What pressing E on this would do, or null if nothing.
    private static string DescribeTarget(Collider collider, NetworkPlayer local)
    {
        if (collider.GetComponentInParent<ShopTerminal>() != null) return "Open the shop";

        Package package = collider.GetComponentInParent<Package>();
        if (package != null)
        {
            string packageName = package.Data != null ? package.Data.displayName : "package";
            bool heavy = package.Data != null && package.Data.holdersRequired > 1;
            return "Grab " + packageName + (heavy ? " (needs two couriers)" : "");
        }

        NetworkPlayer teammate = collider.GetComponentInParent<NetworkPlayer>();
        if (teammate != null && teammate != local)
        {
            if (teammate.IsHeldDown) return "Pull them free!";
            if (teammate.IsClimbing) return "Pull them up";
            return null;
        }

        HouseDoor door = collider.GetComponentInParent<HouseDoor>();
        if (door != null) return door.IsClosed ? "Open door" : "Close door";

        WorldItem item = collider.GetComponentInParent<WorldItem>();
        if (item != null) return "Pick up " + (item.Item != null ? item.Item.displayName : "item");

        if (collider.GetComponentInParent<DeliveryDoor>() != null) return "Knock";
        if (collider.GetComponentInParent<ReviveShrine>() != null) return "Revive a teammate (bring their badge)";
        if (collider.GetComponentInParent<RaftSpot>() != null) return "Build the raft (two couriers)";
        return null;
    }

    private static string Key(string id)
    {
        return KeyBindings.DisplayName(id);
    }

    private void OnGUI()
    {
        if (UIState.HideHud || UIState.MenuOpen || UIState.FreeCam)
        {
            return;
        }
        UITheme.Apply();

        if (showCrosshair && NetworkPlayer.Local != null && !NetworkPlayer.Local.IsDead)
        {
            GUI.color = new Color(1f, 1f, 1f, prompt != "" ? 0.9f : 0.4f);
            GUI.Label(new Rect(Screen.width / 2f - 5f, Screen.height / 2f - 11f, 20f, 22f), "+");
            GUI.color = Color.white;
        }

        if (prompt != "")
        {
            Vector2 size = UITheme.Prompt.CalcSize(new GUIContent(prompt));
            GUI.Label(new Rect((Screen.width - size.x) / 2f, Screen.height / 2f + 40f, size.x, size.y), prompt, UITheme.Prompt);
        }
        if (warning != "")
        {
            GUIStyle style = new GUIStyle(UITheme.Prompt);
            style.fontSize = 20;
            style.normal.textColor = UITheme.Danger;
            Vector2 size = style.CalcSize(new GUIContent(warning));
            GUI.Label(new Rect((Screen.width - size.x) / 2f, Screen.height * 0.3f, size.x, size.y), warning, style);
        }
    }
}
