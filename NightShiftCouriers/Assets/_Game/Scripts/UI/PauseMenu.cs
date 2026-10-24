using UnityEngine;
using UnityEngine.InputSystem;

// Esc menu (plan M5): Resume, Settings (audio, graphics, controls with key rebinding), Leave game, Quit.
// The game does NOT pause - it is online co-op, the monsters keep coming.
// Also handles the screenshot hotkeys: F1 hides the HUD, F10 toggles the retro camera effect.
public class PauseMenu : MonoBehaviour
{
    private enum Page { Main, Audio, Graphics, Controls }

    [SerializeField, Tooltip("Width of the menu panel in pixels.")]
    private float panelWidth = 460f;

    private bool open;
    private Page page = Page.Main;
    private string rebindingId;
    private int rebindStartFrame;
    private Vector2 scroll;
    private float fpsSmoothed;

    public bool IsOpen => open;

    private void Update()
    {
        fpsSmoothed = Mathf.Lerp(fpsSmoothed, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (rebindingId != null)
        {
            UpdateRebind(keyboard);
            return;
        }

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            UIState.HideHud = !UIState.HideHud;
        }
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            GameSettings.RetroEffect = !GameSettings.RetroEffect;
            GameSettings.Save();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (open)
            {
                if (page != Page.Main)
                {
                    page = Page.Main;
                }
                else
                {
                    Close();
                }
            }
            // Another menu (shop, journal) uses Esc to close itself: do not open on that same press.
            else if (!UIState.MenuOpen && UIState.ClosedFrame < Time.frameCount - 1)
            {
                Open();
            }
        }
    }

    private void UpdateRebind(Keyboard keyboard)
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            rebindingId = null; // cancelled
            return;
        }
        if (Time.frameCount <= rebindStartFrame + 1)
        {
            return; // ignore the click that started the rebind
        }
        string path = KeyBindings.PressedPathThisFrame();
        if (path != null)
        {
            KeyBindings.SetPath(rebindingId, path);
            rebindingId = null;
        }
    }

    public void Open()
    {
        open = true;
        page = Page.Main;
        UIState.MenuOpen = true;
    }

    public void Close()
    {
        open = false;
        rebindingId = null;
        GameSettings.Save();
        UIState.MenuOpen = false;
    }

    private void OnDisable()
    {
        if (open)
        {
            Close();
        }
    }

    // ---------- Drawing ----------

    private void OnGUI()
    {
        UITheme.Apply();

        if (GameSettings.ShowFps && !UIState.HideHud)
        {
            GUI.Label(new Rect(Screen.width - 90, Screen.height - 28, 80, 22), Mathf.RoundToInt(fpsSmoothed) + " FPS");
        }

        if (!open)
        {
            return;
        }

        float height = Mathf.Min(Screen.height - 80f, 560f);
        Rect area = new Rect((Screen.width - panelWidth) / 2f, (Screen.height - height) / 2f, panelWidth, height);
        GUILayout.BeginArea(area, GUI.skin.box);
        switch (page)
        {
            case Page.Main: DrawMain(); break;
            case Page.Audio: DrawAudio(); break;
            case Page.Graphics: DrawGraphics(); break;
            case Page.Controls: DrawControls(); break;
        }
        GUILayout.EndArea();
    }

    private void DrawMain()
    {
        GUILayout.Label("NIGHT SHIFT COURIERS", UITheme.Title);
        if (BuildInfo.IsDemo)
        {
            GUILayout.Label("Demo build " + BuildInfo.Version);
        }
        GUILayout.Space(12);

        if (GUILayout.Button("Resume", GUILayout.Height(34))) Close();
        GUILayout.Space(6);
        if (GUILayout.Button("Audio", GUILayout.Height(30))) page = Page.Audio;
        if (GUILayout.Button("Graphics", GUILayout.Height(30))) page = Page.Graphics;
        if (GUILayout.Button("Controls", GUILayout.Height(30))) page = Page.Controls;
        GUILayout.Space(6);

        Unity.Netcode.NetworkManager network = Unity.Netcode.NetworkManager.Singleton;
        if (network != null && network.IsListening)
        {
            string leave = network.IsHost ? "End game for everyone" : "Leave game";
            if (GUILayout.Button(leave, GUILayout.Height(30)))
            {
                Close();
                NetworkBootstrap bootstrap = FindAnyObjectByType<NetworkBootstrap>();
                if (bootstrap != null)
                {
                    bootstrap.Disconnect();
                }
                else
                {
                    network.Shutdown();
                }
            }
        }
        if (GUILayout.Button("Quit to desktop", GUILayout.Height(30)))
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Esc: back    F1: hide HUD    F9: free camera    F10: retro look");
    }

    private void DrawAudio()
    {
        GUILayout.Label("AUDIO", UITheme.Title);
        GameSettings.MasterVolume = Slider("Master volume", GameSettings.MasterVolume, 0f, 1f, true);
        GameSettings.SfxVolume = Slider("Sound effects", GameSettings.SfxVolume, 0f, 1f, true);
        GameSettings.AmbienceVolume = Slider("Ambience (rain, city)", GameSettings.AmbienceVolume, 0f, 1f, true);
        GameSettings.VoiceVolume = Slider("Crew voices", GameSettings.VoiceVolume, 0f, 2f, true);

        VoiceChatService voice = VoiceChatService.Instance;
        if (voice != null && voice.IsConnected)
        {
            GUILayout.Space(6);
            if (GUILayout.Button("Microphone: " + voice.Mode))
            {
                voice.SetMode((VoiceMode)(((int)voice.Mode + 1) % 3));
            }
        }
        DrawFooter();
    }

    private void DrawGraphics()
    {
        GUILayout.Label("GRAPHICS", UITheme.Title);

        string[] qualities = QualitySettings.names;
        if (qualities.Length > 0)
        {
            GUILayout.Label("Quality: " + qualities[Mathf.Clamp(GameSettings.QualityLevel, 0, qualities.Length - 1)]);
            int picked = GUILayout.SelectionGrid(GameSettings.QualityLevel, qualities, Mathf.Min(3, qualities.Length));
            if (picked != GameSettings.QualityLevel)
            {
                GameSettings.QualityLevel = picked;
            }
        }

        GameSettings.FieldOfView = Mathf.Round(Slider("Field of view", GameSettings.FieldOfView, 55f, 100f, false));
        GameSettings.Fullscreen = Toggle("Fullscreen", GameSettings.Fullscreen);
        GameSettings.VSync = Toggle("V-Sync", GameSettings.VSync);
        GameSettings.RetroEffect = Toggle("Retro body-cam look (F10)", GameSettings.RetroEffect);
        GameSettings.ShowFps = Toggle("Show FPS", GameSettings.ShowFps);
        DrawFooter();
    }

    private void DrawControls()
    {
        GUILayout.Label("CONTROLS", UITheme.Title);
        GameSettings.MouseSensitivity = Slider("Mouse sensitivity", GameSettings.MouseSensitivity, 0.1f, 4f, false);
        GameSettings.InvertY = Toggle("Invert mouse Y", GameSettings.InvertY);

        GUILayout.Space(6);
        GUILayout.Label(rebindingId != null ? "Press a key or mouse button... (Esc cancels)" : "Click a key to change it:");
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (KeyBindings.Entry entry in KeyBindings.Entries)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.label, GUILayout.Width(220));
            string shown = rebindingId == entry.id ? "..." : KeyBindings.DisplayName(entry.id);
            GUI.enabled = rebindingId == null;
            if (GUILayout.Button(shown, GUILayout.Width(160)))
            {
                rebindingId = entry.id;
                rebindStartFrame = Time.frameCount;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Reset keys to default"))
        {
            KeyBindings.ResetAll();
        }
        GUILayout.Label("Number keys 1-4 pick an inventory slot. Gamepads use their own fixed layout.");
        DrawFooter();
    }

    private void DrawFooter()
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back")) page = Page.Main;
        if (page != Page.Controls && GUILayout.Button("Reset all settings")) GameSettings.ResetToDefaults();
        GUILayout.EndHorizontal();
    }

    // ---------- Small helpers ----------

    // A labelled slider. Only writes the setting when the slider actually moved.
    private static float Slider(string label, float value, float min, float max, bool asPercent)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(190));
        float changed = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(170));
        GUILayout.Label(asPercent ? Mathf.RoundToInt(changed * 100f) + "%" : changed.ToString("0.0"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        return Mathf.Approximately(changed, value) ? value : changed;
    }

    private static bool Toggle(string label, bool value)
    {
        return GUILayout.Toggle(value, "  " + label);
    }
}
