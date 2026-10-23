using UnityEngine;

// Turns how loudly the player talks into monster-audible noise (plan section 4.12: voice loudness counts as noise).
// This only MEASURES the microphone. Sending voice to other players needs a voice chat plugin (see README).
// Only the owner runs this (NetworkPlayer enables it).
[RequireComponent(typeof(NetworkPlayer))]
public class MicNoiseReporter : MonoBehaviour
{
    [SerializeField, Tooltip("Start with the microphone on. The in-game panel can toggle it.")]
    private bool useMicrophone = false;
    [SerializeField, Tooltip("Microphone level (0-1) below this counts as silence.")]
    private float silenceLevel = 0.02f;
    [SerializeField, Tooltip("Noise loudness when talking at level 0.1 (a normal speaking voice).")]
    private float loudnessAtNormalVoice = 1.5f;
    [SerializeField, Tooltip("Highest loudness a shout can reach.")]
    private float maxLoudness = 5f;
    [SerializeField, Tooltip("Seconds between noise reports while talking.")]
    private float reportInterval = 0.4f;

    private const int SampleWindow = 512;
    private const int SampleRate = 16000;

    private NetworkPlayer networkPlayer;
    private AudioClip micClip;
    private string deviceName;
    private float timer;
    private readonly float[] samples = new float[SampleWindow];

    // 0 = silent, around 0.1 = speaking, 0.3+ = shouting. Other systems (e.g. UI) may read this.
    public float CurrentLevel { get; private set; }

    private void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    // Turn microphone noise on or off while playing.
    public bool UseMicrophone
    {
        get => useMicrophone;
        set
        {
            useMicrophone = value;
            if (isActiveAndEnabled)
            {
                if (useMicrophone) StartMic(); else StopMic();
            }
        }
    }

    private void OnEnable()
    {
        if (useMicrophone)
        {
            StartMic();
        }
    }

    private void OnDisable()
    {
        StopMic();
    }

    private void StartMic()
    {
        if (micClip != null || Microphone.devices.Length == 0)
        {
            return;
        }
        deviceName = Microphone.devices[0];
        micClip = Microphone.Start(deviceName, true, 1, SampleRate);
    }

    private void StopMic()
    {
        if (micClip != null)
        {
            Microphone.End(deviceName);
            micClip = null;
            CurrentLevel = 0f;
        }
    }

    private void Update()
    {
        if (micClip == null)
        {
            return;
        }

        CurrentLevel = MeasureLevel();

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

    private float MeasureLevel()
    {
        int position = Microphone.GetPosition(deviceName) - SampleWindow;
        if (position < 0)
        {
            return 0f;
        }

        micClip.GetData(samples, position);
        float sum = 0f;
        for (int i = 0; i < SampleWindow; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / SampleWindow); // root-mean-square level
    }
}
