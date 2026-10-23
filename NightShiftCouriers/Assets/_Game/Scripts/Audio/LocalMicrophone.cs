using UnityEngine;

// The player's own microphone, read with Unity's Microphone API (owner only).
// Used for: monster noise when Vivox is not running, and short clips for the Chudail's voice mimic.
// Clips never leave the hosted session and are never saved (plan 4.12).
public class LocalMicrophone : MonoBehaviour
{
    private const int MicSampleRate = 16000;
    private const int LoopSeconds = 2;
    private const int LevelWindow = 512;

    // Player choices (the in-game panel toggles these).
    public static bool MicNoiseEnabled = false;          // raw mic level makes monster noise (when Vivox is off)
    public static bool ShareVoiceWithChudail = true;     // let the Chudail copy short clips of your voice

    private AudioClip loop;
    private string deviceName;
    private readonly float[] levelSamples = new float[LevelWindow];

    // 0 = silent, ~0.1 = speaking, 0.3+ = shouting.
    public float Level { get; private set; }
    public bool Running => loop != null;

    private void Update()
    {
        bool wanted = MicNoiseEnabled || ShareVoiceWithChudail;
        if (wanted && loop == null)
        {
            StartMic();
        }
        else if (!wanted && loop != null)
        {
            StopMic();
        }

        Level = loop != null ? MeasureLevel() : 0f;
    }

    private void OnDisable()
    {
        StopMic();
    }

    private void StartMic()
    {
        if (Microphone.devices.Length == 0)
        {
            return;
        }
        deviceName = Microphone.devices[0];
        loop = Microphone.Start(deviceName, true, LoopSeconds, MicSampleRate);
    }

    private void StopMic()
    {
        if (loop != null)
        {
            Microphone.End(deviceName);
            loop = null;
            Level = 0f;
        }
    }

    private float MeasureLevel()
    {
        int position = Microphone.GetPosition(deviceName) - LevelWindow;
        if (position < 0)
        {
            return 0f;
        }
        loop.GetData(levelSamples, position);
        float sum = 0f;
        foreach (float sample in levelSamples)
        {
            sum += sample * sample;
        }
        return Mathf.Sqrt(sum / LevelWindow);
    }

    // The last few seconds of microphone audio, resampled to targetRate. Returns null if not recording.
    public float[] CopyRecent(float seconds, int targetRate)
    {
        if (loop == null)
        {
            return null;
        }

        int total = loop.samples;
        int needed = Mathf.Min(total - 1, Mathf.RoundToInt(seconds * loop.frequency));
        int end = Microphone.GetPosition(deviceName);
        int start = end - needed;
        if (start < 0)
        {
            start += total;
        }

        // The microphone writes into a loop, so the recent audio may wrap around the end of the clip.
        float[] raw = new float[needed];
        int firstPart = Mathf.Min(needed, total - start);
        float[] part = new float[firstPart];
        loop.GetData(part, start);
        part.CopyTo(raw, 0);
        if (firstPart < needed)
        {
            float[] rest = new float[needed - firstPart];
            loop.GetData(rest, 0);
            rest.CopyTo(raw, firstPart);
        }

        // Simple resample by averaging.
        int step = Mathf.Max(1, loop.frequency / targetRate);
        float[] result = new float[needed / step];
        for (int i = 0; i < result.Length; i++)
        {
            float sum = 0f;
            for (int j = 0; j < step; j++)
            {
                sum += raw[i * step + j];
            }
            result[i] = sum / step;
        }
        return result;
    }
}
