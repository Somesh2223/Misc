using UnityEngine;

// Sound effects generated in code, so the project needs no audio files yet (plan 4.17 audio pass comes later).
// Each clip is built once, the first time it is used.
public static class ProceduralSounds
{
    private const int SampleRate = 22050;

    private static AudioClip bell;
    private static AudioClip bang;
    private static AudioClip scream;
    private static AudioClip laugh;
    private static AudioClip whisper;
    private static AudioClip splash;

    // Ghungroo (anklet bell): a short bright ring. The Chudail's tell.
    public static AudioClip Bell
    {
        get
        {
            if (bell == null)
            {
                bell = Build("Bell", 0.5f, (t, random) =>
                    Sine(2700f, t) * Mathf.Exp(-7f * t) + 0.5f * Sine(4100f, t) * Mathf.Exp(-10f * t));
            }
            return bell;
        }
    }

    // Firecracker decoy.
    public static AudioClip Bang
    {
        get
        {
            if (bang == null)
            {
                bang = Build("Bang", 0.6f, (t, random) => Noise(random) * Mathf.Exp(-9f * t));
            }
            return bang;
        }
    }

    // A rising wail (attacks, a possessed package).
    public static AudioClip Scream
    {
        get
        {
            if (scream == null)
            {
                scream = Build("Scream", 1.2f, (t, random) =>
                {
                    float pitch = Mathf.Lerp(700f, 1300f, t / 1.2f) + 40f * Mathf.Sin(t * 40f);
                    float envelope = Mathf.Sin(Mathf.PI * t / 1.2f);
                    return (Mathf.Sin(2f * Mathf.PI * pitch * t) * 0.7f + Noise(random) * 0.3f) * envelope;
                });
            }
            return scream;
        }
    }

    // "Ha-ha-ha-ha" from above: the Vetala's tell.
    public static AudioClip Laugh
    {
        get
        {
            if (laugh == null)
            {
                laugh = Build("Laugh", 1.0f, (t, random) =>
                {
                    float burst = t % 0.25f;               // one "ha" every quarter second
                    float envelope = burst < 0.15f ? Mathf.Sin(Mathf.PI * burst / 0.15f) : 0f;
                    float voice = Sine(230f, t) + 0.5f * Sine(460f, t) + 0.25f * Sine(690f, t);
                    return (voice * 0.6f + Noise(random) * 0.25f) * envelope;
                });
            }
            return laugh;
        }
    }

    // Breathy noise, used when the Chudail has no voice to copy.
    public static AudioClip Whisper
    {
        get
        {
            if (whisper == null)
            {
                whisper = Build("Whisper", 1.5f, (t, random) =>
                    Noise(random) * 0.4f * (0.5f + 0.5f * Mathf.Sin(t * 9f)) * Mathf.Sin(Mathf.PI * t / 1.5f));
            }
            return whisper;
        }
    }

    // A wet slap / splash (water monster, raft paddling).
    public static AudioClip Splash
    {
        get
        {
            if (splash == null)
            {
                splash = Build("Splash", 0.4f, (t, random) => Noise(random) * Mathf.Exp(-12f * t) * 0.8f);
            }
            return splash;
        }
    }

    // Plays a clip in 3D at a position, then cleans up.
    public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float maxDistance = 30f)
    {
        GameObject soundObject = new GameObject("Sound: " + clip.name);
        soundObject.transform.position = position;
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = maxDistance;
        source.Play();
        Object.Destroy(soundObject, clip.length + 0.1f);
    }

    // ---------- Helpers ----------

    private delegate float Wave(float time, System.Random random);

    private static AudioClip Build(string name, float seconds, Wave wave)
    {
        int count = Mathf.CeilToInt(SampleRate * seconds);
        float[] samples = new float[count];
        System.Random random = new System.Random(name.GetHashCode());
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            samples[i] = Mathf.Clamp(wave(t, random) * 0.6f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Sine(float frequency, float t)
    {
        return Mathf.Sin(2f * Mathf.PI * frequency * t);
    }

    private static float Noise(System.Random random)
    {
        return (float)(random.NextDouble() * 2.0 - 1.0);
    }
}
