using UnityEngine;

// Sound effects generated in code, so the project needs no audio files yet (plan 4.17).
// Each clip is built once, the first time it is used. Clips marked "loop" can be played with AudioSource.loop.
// Swap any of these for a real recording later: only this file needs to change.
public static class ProceduralSounds
{
    private const int SampleRate = 22050;

    private static AudioClip bell;
    private static AudioClip bang;
    private static AudioClip scream;
    private static AudioClip laugh;
    private static AudioClip whisper;
    private static AudioClip splash;
    private static AudioClip rainLoop;
    private static AudioClip thunder;
    private static AudioClip siren;
    private static AudioClip templeBell;
    private static AudioClip dogBark;
    private static AudioClip generatorHum;
    private static AudioClip chant;
    private static AudioClip gurgle;
    private static AudioClip doorCreak;
    private static AudioClip doorSlam;
    private static AudioClip grateRattle;
    private static AudioClip stinger;
    private static AudioClip heartbeat;
    private static AudioClip roar;

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

    // ---------- Ambience (plan 4.17) ----------

    // loop: steady monsoon rain (filtered noise with the odd heavy drop).
    public static AudioClip RainLoop
    {
        get
        {
            if (rainLoop == null)
            {
                float smooth = 0f;
                rainLoop = Build("RainLoop", 4f, (t, random) =>
                {
                    smooth += 0.35f * (Noise(random) - smooth);
                    float drop = random.NextDouble() < 0.0015 ? Noise(random) * 0.8f : 0f;
                    return smooth * 0.9f + drop;
                });
            }
            return rainLoop;
        }
    }

    // A long low rumble with a crack at the start.
    public static AudioClip Thunder
    {
        get
        {
            if (thunder == null)
            {
                float rumble = 0f;
                thunder = Build("Thunder", 4f, (t, random) =>
                {
                    rumble += 0.03f * (Noise(random) - rumble);
                    float crack = Noise(random) * Mathf.Exp(-14f * t) * 0.6f;
                    float envelope = Mathf.Min(1f, t * 8f) * Mathf.Exp(-0.9f * t);
                    return rumble * 7f * envelope + crack;
                });
            }
            return thunder;
        }
    }

    // A distant air-raid / flood siren rising and falling.
    public static AudioClip Siren
    {
        get
        {
            if (siren == null)
            {
                float phase = 0f;
                siren = Build("Siren", 6f, (t, random) =>
                {
                    float frequency = 320f + 330f * Mathf.Sin(Mathf.PI * t / 6f);
                    phase += 2f * Mathf.PI * frequency / SampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * t / 6f);
                    return (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * 0.6f * envelope;
                });
            }
            return siren;
        }
    }

    // A big temple bell: a low strike with long, slightly out-of-tune overtones.
    public static AudioClip TempleBell
    {
        get
        {
            if (templeBell == null)
            {
                templeBell = Build("TempleBell", 4f, (t, random) =>
                    (Sine(196f, t) * 0.6f + Sine(392f * 1.19f, t) * 0.35f + Sine(588f * 1.33f, t) * 0.2f) * Mathf.Exp(-0.9f * t)
                    + Noise(random) * Mathf.Exp(-40f * t) * 0.3f);
            }
            return templeBell;
        }
    }

    // Two barks from a stray dog.
    public static AudioClip DogBark
    {
        get
        {
            if (dogBark == null)
            {
                dogBark = Build("DogBark", 0.9f, (t, random) =>
                {
                    float local = t < 0.35f ? t : t - 0.35f;
                    if (local > 0.14f)
                    {
                        return 0f;
                    }
                    float pitch = Mathf.Lerp(520f, 330f, local / 0.14f);
                    float envelope = Mathf.Sin(Mathf.PI * local / 0.14f);
                    float tone = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * pitch * local)) * 0.5f;
                    return (tone + Noise(random) * 0.5f) * envelope;
                });
            }
            return dogBark;
        }
    }

    // loop: a diesel generator humming behind a shutter.
    public static AudioClip GeneratorHum
    {
        get
        {
            if (generatorHum == null)
            {
                generatorHum = Build("GeneratorHum", 2f, (t, random) =>
                    (Sine(50f, t) * 0.5f + Sine(100f, t) * 0.35f + Sine(150f, t) * 0.2f) * (0.85f + 0.15f * Sine(8f, t))
                    + Noise(random) * 0.05f);
            }
            return generatorHum;
        }
    }

    // ---------- Monsters ----------

    // loop: the Brahmarakshas chanting, a deep "om" pulse. Its tell.
    public static AudioClip Chant
    {
        get
        {
            if (chant == null)
            {
                chant = Build("Chant", 4f, (t, random) =>
                {
                    float voice = Sine(110f, t) * 0.6f + Sine(220f, t) * 0.3f + Sine(330f, t) * 0.2f + Sine(550f, t) * 0.12f;
                    float syllables = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Mathf.PI * t)); // one "om" per second
                    return voice * syllables * 0.8f + Noise(random) * 0.03f;
                });
            }
            return chant;
        }
    }

    // A deep growl when the Brahmarakshas strikes.
    public static AudioClip Roar
    {
        get
        {
            if (roar == null)
            {
                float smooth = 0f;
                roar = Build("Roar", 1.6f, (t, random) =>
                {
                    smooth += 0.08f * (Noise(random) - smooth);
                    float growl = 0.6f + 0.4f * Sine(28f, t);
                    float envelope = Mathf.Min(1f, t * 6f) * Mathf.Exp(-1.6f * t);
                    return (smooth * 5f * growl + Sine(75f, t) * 0.4f) * envelope;
                });
            }
            return roar;
        }
    }

    // Bubbling drain water: the Mud Crawler moving below.
    public static AudioClip Gurgle
    {
        get
        {
            if (gurgle == null)
            {
                float bubbleStart = 0f;
                float bubblePitch = 400f;
                gurgle = Build("Gurgle", 1.2f, (t, random) =>
                {
                    if (t - bubbleStart > 0.07f + (float)random.NextDouble() * 0.05f)
                    {
                        bubbleStart = t;
                        bubblePitch = 250f + (float)random.NextDouble() * 600f;
                    }
                    float local = t - bubbleStart;
                    float pitch = bubblePitch * (1f + local * 6f); // bubbles rise in pitch as they pop
                    return Mathf.Sin(2f * Mathf.PI * pitch * local) * Mathf.Exp(-30f * local) * Mathf.Sin(Mathf.PI * t / 1.2f);
                });
            }
            return gurgle;
        }
    }

    // A metal grate shaking in its frame.
    public static AudioClip GrateRattle
    {
        get
        {
            if (grateRattle == null)
            {
                float lastHit = -1f;
                grateRattle = Build("GrateRattle", 0.7f, (t, random) =>
                {
                    if (random.NextDouble() < 0.0025)
                    {
                        lastHit = t;
                    }
                    float local = lastHit < 0f ? 1f : t - lastHit;
                    float clank = (Sine(1650f, local) + 0.6f * Sine(2730f, local) + 0.4f * Sine(980f, local)) * Mathf.Exp(-45f * local);
                    return clank * 0.7f + Noise(random) * 0.05f * Mathf.Exp(-3f * t);
                });
            }
            return grateRattle;
        }
    }

    // ---------- Doors ----------

    public static AudioClip DoorCreak
    {
        get
        {
            if (doorCreak == null)
            {
                float phase = 0f;
                doorCreak = Build("DoorCreak", 0.9f, (t, random) =>
                {
                    float frequency = 170f + 70f * Mathf.Sin(t * 7f) + (float)random.NextDouble() * 25f;
                    phase += 2f * Mathf.PI * frequency / SampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * t / 0.9f);
                    return Mathf.Sign(Mathf.Sin(phase)) * 0.25f * envelope;
                });
            }
            return doorCreak;
        }
    }

    public static AudioClip DoorSlam
    {
        get
        {
            if (doorSlam == null)
            {
                doorSlam = Build("DoorSlam", 0.6f, (t, random) =>
                    Sine(70f, t) * Mathf.Exp(-10f * t) + Noise(random) * Mathf.Exp(-35f * t) * 0.8f);
            }
            return doorSlam;
        }
    }

    // ---------- Music cues ----------

    // A sharp dissonant swell when something starts hunting you.
    public static AudioClip Stinger
    {
        get
        {
            if (stinger == null)
            {
                stinger = Build("Stinger", 2.2f, (t, random) =>
                {
                    float chord = Sine(220f, t) + Sine(233f, t) + Sine(311f, t) * 0.7f + Sine(466f, t) * 0.4f;
                    float envelope = Mathf.Min(1f, t * 12f) * Mathf.Exp(-1.4f * t);
                    return (chord * 0.3f + Noise(random) * 0.15f) * envelope;
                });
            }
            return stinger;
        }
    }

    // loop: lub-dub, once a second.
    public static AudioClip Heartbeat
    {
        get
        {
            if (heartbeat == null)
            {
                heartbeat = Build("Heartbeat", 1f, (t, random) =>
                {
                    float lub = Sine(55f, t) * Mathf.Exp(-25f * t);
                    float dubTime = Mathf.Max(0f, t - 0.25f);
                    float dub = t >= 0.25f ? Sine(48f, dubTime) * Mathf.Exp(-30f * dubTime) * 0.7f : 0f;
                    return (lub + dub) * 1.4f;
                });
            }
            return heartbeat;
        }
    }

    // The clip for a sound the server sent to everyone.
    public static AudioClip Get(WorldSound sound)
    {
        switch (sound)
        {
            case WorldSound.Bell: return Bell;
            case WorldSound.Bang: return Bang;
            case WorldSound.Scream: return Scream;
            case WorldSound.Laugh: return Laugh;
            case WorldSound.Whisper: return Whisper;
            case WorldSound.Splash: return Splash;
            case WorldSound.TempleBell: return TempleBell;
            case WorldSound.DogBark: return DogBark;
            case WorldSound.Siren: return Siren;
            case WorldSound.DoorSlam: return DoorSlam;
            case WorldSound.DoorCreak: return DoorCreak;
            case WorldSound.Gurgle: return Gurgle;
            case WorldSound.GrateRattle: return GrateRattle;
            case WorldSound.Roar: return Roar;
            case WorldSound.Thunder: return Thunder;
        }
        return Bang;
    }

    // Plays a clip in 3D at a position, then cleans up. The volume is scaled by the player's SFX setting.
    public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float maxDistance = 30f)
    {
        GameObject soundObject = new GameObject("Sound: " + clip.name);
        soundObject.transform.position = position;
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * GameSettings.SfxVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = maxDistance;
        source.Play();
        Object.Destroy(soundObject, clip.length + 0.1f);
    }

    // A looping 3D sound attached to an object (a monster's chant, a generator). Returns the source to adjust later.
    public static AudioSource AddLoop(GameObject owner, AudioClip clip, float volume, float maxDistance)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume * GameSettings.SfxVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f;
        source.maxDistance = maxDistance;
        source.time = Random.Range(0f, clip.length); // so two of them do not play in step
        source.Play();
        return source;
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
