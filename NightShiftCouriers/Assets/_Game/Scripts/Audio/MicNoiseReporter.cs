using UnityEngine;

// Turns how loudly the player talks into monster-audible noise (plan section 4.12: voice loudness counts as noise).
// Uses Vivox's speech level while voice chat is connected, otherwise (if enabled) the raw microphone.
// Only the owner runs this (NetworkPlayer enables it).
[RequireComponent(typeof(NetworkPlayer))]
[RequireComponent(typeof(LocalMicrophone))]
public class MicNoiseReporter : MonoBehaviour
{
    [SerializeField, Tooltip("Vivox reports speech energy on a different scale than the raw microphone. Tune this so normal speech lands around 0.1.")]
    private float vivoxLevelScale = 0.5f;
    [SerializeField, Tooltip("Microphone level (0-1) below this counts as silence.")]
    private float silenceLevel = 0.02f;
    [SerializeField, Tooltip("Noise loudness when talking at level 0.1 (a normal speaking voice).")]
    private float loudnessAtNormalVoice = 1.5f;
    [SerializeField, Tooltip("Highest loudness a shout can reach.")]
    private float maxLoudness = 5f;
    [SerializeField, Tooltip("Seconds between noise reports while talking.")]
    private float reportInterval = 0.4f;

    private NetworkPlayer networkPlayer;
    private LocalMicrophone microphone;
    private float timer;

    // 0 = silent, around 0.1 = speaking, 0.3+ = shouting.
    public float CurrentLevel { get; private set; }

    // True while the level comes from Vivox (the voice chat is connected).
    public bool UsingVoiceChat { get; private set; }

    // Raw-microphone noise, used only when Vivox is not connected.
    public bool UseMicrophone
    {
        get => LocalMicrophone.MicNoiseEnabled;
        set => LocalMicrophone.MicNoiseEnabled = value;
    }

    private void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
        microphone = GetComponent<LocalMicrophone>();
    }

    private void Update()
    {
        VoiceChatService voice = VoiceChatService.Instance;
        UsingVoiceChat = voice != null && voice.IsConnected;
        if (UsingVoiceChat)
        {
            CurrentLevel = voice.LocalLevel * vivoxLevelScale;
        }
        else if (LocalMicrophone.MicNoiseEnabled && microphone.Running)
        {
            CurrentLevel = microphone.Level;
        }
        else
        {
            CurrentLevel = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f || CurrentLevel < silenceLevel)
        {
            return;
        }
        timer = reportInterval;

        float loudness = Mathf.Clamp(CurrentLevel / 0.1f * loudnessAtNormalVoice, 0f, maxLoudness);
        // Voices are not "water noise": a water monster hears them only if the speaker also splashes.
        networkPlayer.ReportNoise(loudness, false);
    }
}
