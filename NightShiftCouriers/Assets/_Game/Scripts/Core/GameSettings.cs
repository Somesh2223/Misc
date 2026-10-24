using System;
using UnityEngine;

// The player's settings (plan M5): audio, controls and graphics. Saved in PlayerPrefs on this computer.
// Read the values from anywhere (e.g. GameSettings.SfxVolume). Setting one applies it straight away
// and fires Changed. The pause menu calls Save() when it closes.
public static class GameSettings
{
    private const string Prefix = "NightShift.";

    private static bool loaded;
    private static float masterVolume;
    private static float sfxVolume;
    private static float ambienceVolume;
    private static float voiceVolume;
    private static float mouseSensitivity;
    private static bool invertY;
    private static float fieldOfView;
    private static bool retroEffect;
    private static int qualityLevel;
    private static bool fullscreen;
    private static bool vSync;
    private static bool showFps;

    public static event Action Changed;

    // Setting a value to what it already is does nothing, so menus can write every frame.

    // ---------- Audio ----------

    public static float MasterVolume { get { Load(); return masterVolume; } set { Load(); if (masterVolume == Mathf.Clamp01(value)) return; masterVolume = Mathf.Clamp01(value); Store("MasterVolume", masterVolume); } }
    // Sound effects: monsters, doors, items.
    public static float SfxVolume { get { Load(); return sfxVolume; } set { Load(); if (sfxVolume == Mathf.Clamp01(value)) return; sfxVolume = Mathf.Clamp01(value); Store("SfxVolume", sfxVolume); } }
    // Rain, thunder, sirens, distant city sounds.
    public static float AmbienceVolume { get { Load(); return ambienceVolume; } set { Load(); if (ambienceVolume == Mathf.Clamp01(value)) return; ambienceVolume = Mathf.Clamp01(value); Store("AmbienceVolume", ambienceVolume); } }
    // Other players' voices (0 = muted, 1 = normal, 2 = loud).
    public static float VoiceVolume { get { Load(); return voiceVolume; } set { Load(); if (voiceVolume == Mathf.Clamp(value, 0f, 2f)) return; voiceVolume = Mathf.Clamp(value, 0f, 2f); Store("VoiceVolume", voiceVolume); } }

    // ---------- Controls ----------

    // Multiplies the normal mouse look speed.
    public static float MouseSensitivity { get { Load(); return mouseSensitivity; } set { Load(); if (mouseSensitivity == Mathf.Clamp(value, 0.1f, 4f)) return; mouseSensitivity = Mathf.Clamp(value, 0.1f, 4f); Store("MouseSensitivity", mouseSensitivity); } }
    public static bool InvertY { get { Load(); return invertY; } set { Load(); if (invertY == value) return; invertY = value; Store("InvertY", value ? 1 : 0); } }

    // ---------- Graphics ----------

    public static float FieldOfView { get { Load(); return fieldOfView; } set { Load(); if (fieldOfView == Mathf.Clamp(value, 55f, 100f)) return; fieldOfView = Mathf.Clamp(value, 55f, 100f); Store("FieldOfView", fieldOfView); } }
    // The VHS / body cam look (F10).
    public static bool RetroEffect { get { Load(); return retroEffect; } set { Load(); if (retroEffect == value) return; retroEffect = value; Store("RetroEffect", value ? 1 : 0); } }
    public static int QualityLevel { get { Load(); return qualityLevel; } set { Load(); if (qualityLevel == Mathf.Clamp(value, 0, QualitySettings.names.Length - 1)) return; qualityLevel = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1); Store("QualityLevel", qualityLevel); } }
    public static bool Fullscreen { get { Load(); return fullscreen; } set { Load(); if (fullscreen == value) return; fullscreen = value; Store("Fullscreen", value ? 1 : 0); } }
    public static bool VSync { get { Load(); return vSync; } set { Load(); if (vSync == value) return; vSync = value; Store("VSync", value ? 1 : 0); } }
    public static bool ShowFps { get { Load(); return showFps; } set { Load(); if (showFps == value) return; showFps = value; Store("ShowFps", value ? 1 : 0); } }

    // ---------- Load / save ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyAtStartup()
    {
        Load();
        Apply();
    }

    private static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        masterVolume = PlayerPrefs.GetFloat(Prefix + "MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat(Prefix + "SfxVolume", 1f);
        ambienceVolume = PlayerPrefs.GetFloat(Prefix + "AmbienceVolume", 0.8f);
        voiceVolume = PlayerPrefs.GetFloat(Prefix + "VoiceVolume", 1f);
        mouseSensitivity = PlayerPrefs.GetFloat(Prefix + "MouseSensitivity", 1f);
        invertY = PlayerPrefs.GetInt(Prefix + "InvertY", 0) == 1;
        fieldOfView = PlayerPrefs.GetFloat(Prefix + "FieldOfView", 70f);
        retroEffect = PlayerPrefs.GetInt(Prefix + "RetroEffect", 1) == 1;
        qualityLevel = PlayerPrefs.GetInt(Prefix + "QualityLevel", QualitySettings.GetQualityLevel());
        fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        vSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1;
        showFps = PlayerPrefs.GetInt(Prefix + "ShowFps", 0) == 1;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        foreach (string key in new[] { "MasterVolume", "SfxVolume", "AmbienceVolume", "VoiceVolume", "MouseSensitivity", "InvertY",
            "FieldOfView", "RetroEffect", "QualityLevel", "Fullscreen", "VSync", "ShowFps" })
        {
            PlayerPrefs.DeleteKey(Prefix + key);
        }
        loaded = false;
        Load();
        Apply();
        Changed?.Invoke();
    }

    private static void Store(string key, float value)
    {
        PlayerPrefs.SetFloat(Prefix + key, value);
        Apply();
        Changed?.Invoke();
    }

    private static void Store(string key, int value)
    {
        PlayerPrefs.SetInt(Prefix + key, value);
        Apply();
        Changed?.Invoke();
    }

    // Pushes the settings that live in Unity itself. Others (FOV, mouse, voice) are read by their scripts.
    private static void Apply()
    {
        AudioListener.volume = masterVolume;
        if (QualitySettings.GetQualityLevel() != qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        if (Screen.fullScreen != fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }
    }
}
