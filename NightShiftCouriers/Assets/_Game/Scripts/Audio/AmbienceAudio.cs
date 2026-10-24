using UnityEngine;

// The sound of the flooded city at night (plan M5 audio pass). Runs locally on every machine;
// nothing here is networked, so each player hears their own random mix.
//  - Rain that gets heavier with each flood stage, muffled when you are under a roof
//  - Far-off sirens, temple bells, dogs and generators in random directions
//  - Thunder with a lightning flash
//  - Everything sounds muffled while your head is underwater
//  - A sting and a heartbeat when a monster close to you starts hunting
// Uses the Ambience volume slider (the stinger and heartbeat use SFX).
public class AmbienceAudio : MonoBehaviour
{
    [Header("Rain")]
    [SerializeField, Tooltip("Rain volume at flood stage 1, and how much louder each stage makes it.")]
    private float rainBaseVolume = 0.25f;
    [SerializeField] private float rainPerStage = 0.12f;
    [SerializeField, Tooltip("Rain volume multiplier while something is above your head.")]
    private float indoorRainMultiplier = 0.35f;

    [Header("Distant sounds")]
    [SerializeField, Tooltip("Seconds between random distant sounds (min, max).")]
    private Vector2 distantInterval = new Vector2(8f, 20f);
    [SerializeField, Tooltip("How far away distant sounds play (min, max metres).")]
    private Vector2 distantRange = new Vector2(35f, 70f);

    [Header("Thunder")]
    [SerializeField] private Vector2 thunderInterval = new Vector2(25f, 70f);
    [SerializeField, Tooltip("How bright the lightning flash makes the ambient light.")]
    private float flashStrength = 0.6f;

    [Header("Monster sting")]
    [SerializeField, Tooltip("A monster that starts hunting within this distance plays the sting.")]
    private float stingRange = 25f;
    [SerializeField, Tooltip("No second sting for this many seconds.")]
    private float stingCooldown = 12f;
    [SerializeField, Tooltip("The heartbeat plays while a hunting monster is within this distance.")]
    private float heartbeatRange = 15f;

    private AudioSource rain;
    private AudioLowPassFilter rainFilter;
    private AudioSource heartbeat;
    private AudioSource stinger;
    private AudioListener listener;
    private AudioLowPassFilter underwaterFilter;

    private float distantTimer;
    private float thunderTimer;
    private float stingTimer;
    private float listenerSearchTimer;
    private float flash;
    private Color baseAmbient;
    private bool ambientSaved;

    private void Start()
    {
        rain = MakeSource("Rain", ProceduralSounds.RainLoop, true);
        rainFilter = rain.gameObject.AddComponent<AudioLowPassFilter>();
        rainFilter.cutoffFrequency = 22000f;
        heartbeat = MakeSource("Heartbeat", ProceduralSounds.Heartbeat, true);
        heartbeat.volume = 0f;
        stinger = MakeSource("Stinger", ProceduralSounds.Stinger, false);

        distantTimer = Random.Range(distantInterval.x, distantInterval.y);
        thunderTimer = Random.Range(thunderInterval.x * 0.3f, thunderInterval.y * 0.5f);
    }

    private void OnEnable()
    {
        GameEvents.MonsterStateChanged += OnMonsterStateChanged;
        GameEvents.FloodWarning += OnFloodWarning;
    }

    private void OnDisable()
    {
        GameEvents.MonsterStateChanged -= OnMonsterStateChanged;
        GameEvents.FloodWarning -= OnFloodWarning;
        RestoreAmbient();
    }

    // A non-3D source on a child object, so its filter only changes this one sound.
    private AudioSource MakeSource(string sourceName, AudioClip clip, bool loop)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        if (loop)
        {
            source.Play();
        }
        return source;
    }

    private void Update()
    {
        Vector3 ears = ListenerPosition();
        bool inShift = CampaignManager.Instance == null || CampaignManager.Instance.Phase == ShiftPhase.Shift;

        UpdateRain(ears);
        UpdateUnderwater(ears);
        UpdateHeartbeat(ears);
        UpdateFlash();

        stingTimer -= Time.deltaTime;
        if (!inShift)
        {
            return; // the depot is calm: rain only
        }

        distantTimer -= Time.deltaTime;
        if (distantTimer <= 0f)
        {
            distantTimer = Random.Range(distantInterval.x, distantInterval.y);
            PlayDistant(ears);
        }

        thunderTimer -= Time.deltaTime;
        if (thunderTimer <= 0f)
        {
            thunderTimer = Random.Range(thunderInterval.x, thunderInterval.y);
            flash = 1f;
            // Light travels faster than sound: the rumble arrives a moment after the flash.
            Invoke(nameof(PlayThunder), Random.Range(0.3f, 2f));
        }
    }

    private void UpdateRain(Vector3 ears)
    {
        int stage = FloodManager.Instance != null ? FloodManager.Instance.StageIndex : 0;
        bool indoors = Physics.Raycast(ears, Vector3.up, 25f, ~(1 << 2), QueryTriggerInteraction.Ignore);
        float target = (rainBaseVolume + rainPerStage * stage) * (indoors ? indoorRainMultiplier : 1f);
        rain.volume = Mathf.MoveTowards(rain.volume, target * GameSettings.AmbienceVolume, Time.deltaTime * 0.5f);
        rainFilter.cutoffFrequency = Mathf.MoveTowards(rainFilter.cutoffFrequency, indoors ? 1200f : 22000f, Time.deltaTime * 40000f);
    }

    // Puts a low-pass filter on the listener, so every sound is dull while your head is underwater.
    private void UpdateUnderwater(Vector3 ears)
    {
        if (listener == null)
        {
            return;
        }
        if (underwaterFilter == null || underwaterFilter.gameObject != listener.gameObject)
        {
            underwaterFilter = listener.GetComponent<AudioLowPassFilter>();
            if (underwaterFilter == null)
            {
                underwaterFilter = listener.gameObject.AddComponent<AudioLowPassFilter>();
            }
        }
        bool underwater = IsUnderwater(ears);
        underwaterFilter.cutoffFrequency = underwater ? 600f : 22000f;
        underwaterFilter.lowpassResonanceQ = 1f;
    }

    private static bool IsUnderwater(Vector3 point)
    {
        foreach (WaterVolume water in WaterVolume.All)
        {
            if (water == null)
            {
                continue;
            }
            Bounds area = water.Area;
            if (point.y < water.SurfaceHeight && point.x > area.min.x && point.x < area.max.x
                && point.z > area.min.z && point.z < area.max.z)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateHeartbeat(Vector3 ears)
    {
        bool close = false;
        foreach (MonsterBase monster in MonsterBase.All)
        {
            if (monster != null && (monster.State == MonsterState.Hunt || monster.State == MonsterState.Attack)
                && Vector3.Distance(monster.transform.position, ears) < heartbeatRange)
            {
                close = true;
                break;
            }
        }
        float target = close ? 0.7f * GameSettings.SfxVolume : 0f;
        heartbeat.volume = Mathf.MoveTowards(heartbeat.volume, target, Time.deltaTime * 0.6f);
        heartbeat.pitch = close ? 1.15f : 1f;
    }

    private void OnMonsterStateChanged(MonsterBase monster, MonsterState state)
    {
        if (state != MonsterState.Hunt || stingTimer > 0f || monster == null)
        {
            return;
        }
        if (Vector3.Distance(monster.transform.position, ListenerPosition()) > stingRange)
        {
            return;
        }
        stingTimer = stingCooldown;
        stinger.volume = 0.8f * GameSettings.SfxVolume;
        stinger.Play();
    }

    // The flood siren is loud and close: the city's warning system.
    private void OnFloodWarning(int nextStage, float secondsLeft)
    {
        Vector3 ears = ListenerPosition();
        Vector2 offset = Random.insideUnitCircle.normalized * 25f;
        PlayAmbient(ProceduralSounds.Siren, ears + new Vector3(offset.x, 10f, offset.y), 1f, 150f);
    }

    private void PlayDistant(Vector3 ears)
    {
        AudioClip clip;
        float roll = Random.value;
        if (roll < 0.3f) clip = ProceduralSounds.DogBark;
        else if (roll < 0.55f) clip = ProceduralSounds.TempleBell;
        else if (roll < 0.75f) clip = ProceduralSounds.Siren;
        else clip = ProceduralSounds.GeneratorHum;

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(distantRange.x, distantRange.y);
        Vector3 where = ears + new Vector3(offset.x, 3f, offset.y);
        PlayAmbient(clip, where, Random.Range(0.5f, 0.9f), distantRange.y + 40f);
    }

    private void PlayThunder()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * 60f;
        PlayAmbient(ProceduralSounds.Thunder, ListenerPosition() + new Vector3(offset.x, 40f, offset.y), 1f, 200f);
    }

    // Like ProceduralSounds.PlayAt, but on the Ambience slider.
    private static void PlayAmbient(AudioClip clip, Vector3 position, float volume, float maxDistance)
    {
        GameObject soundObject = new GameObject("Ambience: " + clip.name);
        soundObject.transform.position = position;
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * GameSettings.AmbienceVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = maxDistance * 0.5f; // far sounds stay audible
        source.maxDistance = maxDistance;
        source.Play();
        Destroy(soundObject, clip.length + 0.1f);
    }

    // ---------- Lightning ----------

    private void UpdateFlash()
    {
        if (flash <= 0f)
        {
            return;
        }
        if (!ambientSaved)
        {
            baseAmbient = RenderSettings.ambientLight;
            ambientSaved = true;
        }
        flash = Mathf.Max(0f, flash - Time.deltaTime * 3f);
        // Two quick flickers then a fade.
        float flicker = flash > 0.6f && flash < 0.75f ? 0.2f : flash;
        RenderSettings.ambientLight = baseAmbient + new Color(0.6f, 0.65f, 0.8f) * flicker * flashStrength;
        if (flash <= 0f)
        {
            RestoreAmbient();
        }
    }

    private void RestoreAmbient()
    {
        if (ambientSaved)
        {
            RenderSettings.ambientLight = baseAmbient;
            ambientSaved = false;
        }
    }

    // ---------- Helpers ----------

    private Vector3 ListenerPosition()
    {
        listenerSearchTimer -= Time.deltaTime;
        if ((listener == null || !listener.isActiveAndEnabled) && listenerSearchTimer <= 0f)
        {
            listenerSearchTimer = 1f;
            listener = FindAnyObjectByType<AudioListener>();
        }
        return listener != null ? listener.transform.position : transform.position;
    }
}
