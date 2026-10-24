using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Rebindable keyboard/mouse controls (plan M5 settings). Saved in PlayerPrefs.
// Each entry is one keyboard/mouse binding of one PlayerInputReader action; gamepad bindings stay fixed.
public static class KeyBindings
{
    public class Entry
    {
        public string id;          // saved under this name
        public string label;       // shown in the settings menu
        public string action;      // PlayerInputReader action name
        public int bindingIndex;   // which binding of that action
        public string defaultPath; // e.g. "<Keyboard>/e"

        public Entry(string id, string label, string action, int bindingIndex, string defaultPath)
        {
            this.id = id;
            this.label = label;
            this.action = action;
            this.bindingIndex = bindingIndex;
            this.defaultPath = defaultPath;
        }
    }

    private const string Prefix = "NightShift.Key.";

    // In the order the settings menu lists them.
    public static readonly Entry[] Entries =
    {
        new Entry("forward", "Move forward", "Move", 1, "<Keyboard>/w"),
        new Entry("back", "Move back", "Move", 2, "<Keyboard>/s"),
        new Entry("left", "Move left", "Move", 3, "<Keyboard>/a"),
        new Entry("right", "Move right", "Move", 4, "<Keyboard>/d"),
        new Entry("sprint", "Sprint", "Sprint", 0, "<Keyboard>/leftShift"),
        new Entry("crouch", "Crouch", "Crouch", 0, "<Keyboard>/leftCtrl"),
        new Entry("jump", "Jump / climb", "Jump", 0, "<Keyboard>/space"),
        new Entry("interact", "Grab / use / open door", "Interact", 0, "<Keyboard>/e"),
        new Entry("throw", "Throw", "Throw", 0, "<Mouse>/leftButton"),
        new Entry("use", "Use item", "Use", 0, "<Mouse>/rightButton"),
        new Entry("open", "Open package", "OpenPackage", 0, "<Keyboard>/f"),
        new Entry("journal", "Journal", "Journal", 0, "<Keyboard>/j"),
        new Entry("talk", "Push to talk", "PushToTalk", 0, "<Keyboard>/v"),
    };

    // Fired when a key changes, so every PlayerInputReader can update its actions.
    public static event Action Changed;

    public static string GetPath(string id)
    {
        Entry entry = Find(id);
        if (entry == null)
        {
            return "";
        }
        return PlayerPrefs.GetString(Prefix + id, entry.defaultPath);
    }

    public static void SetPath(string id, string path)
    {
        PlayerPrefs.SetString(Prefix + id, path);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void ResetAll()
    {
        foreach (Entry entry in Entries)
        {
            PlayerPrefs.DeleteKey(Prefix + entry.id);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // A short name for prompts, e.g. "E" or "Left Button".
    public static string DisplayName(string id)
    {
        string path = GetPath(id);
        if (path == "")
        {
            return "?";
        }
        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    // Sets every rebindable binding on these actions to the saved keys.
    public static void ApplyTo(Dictionary<string, InputAction> actionsByName)
    {
        foreach (Entry entry in Entries)
        {
            if (!actionsByName.TryGetValue(entry.action, out InputAction action) || entry.bindingIndex >= action.bindings.Count)
            {
                continue;
            }
            string path = GetPath(entry.id);
            if (path == entry.defaultPath)
            {
                action.RemoveBindingOverride(entry.bindingIndex);
            }
            else
            {
                action.ApplyBindingOverride(entry.bindingIndex, path);
            }
        }
    }

    private static Entry Find(string id)
    {
        foreach (Entry entry in Entries)
        {
            if (entry.id == id)
            {
                return entry;
            }
        }
        return null;
    }

    // ---------- Listening for a new key (settings menu) ----------

    // The key or mouse button pressed this frame as a binding path, or "" if none.
    // Escape is not returned (it cancels a rebind).
    public static string PressedPathThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (UnityEngine.InputSystem.Controls.KeyControl key in keyboard.allKeys)
            {
                if (key != null && key.wasPressedThisFrame && key != keyboard.escapeKey)
                {
                    return "<Keyboard>/" + key.name;
                }
            }
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
            if (mouse.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
            if (mouse.middleButton.wasPressedThisFrame) return "<Mouse>/middleButton";
            if (mouse.forwardButton.wasPressedThisFrame) return "<Mouse>/forwardButton";
            if (mouse.backButton.wasPressedThisFrame) return "<Mouse>/backButton";
        }
        return "";
    }
}
