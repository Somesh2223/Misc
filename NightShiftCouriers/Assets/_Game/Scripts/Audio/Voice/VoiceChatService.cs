using System;
using System.Threading.Tasks;
using UnityEngine;
#if VIVOX_PRESENT
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
#endif

// Proximity voice chat with Vivox (plan section 4.12). Everything Vivox-specific lives in this one file.
// One positional channel per crew: your voice gets quieter with distance from the speaker.
//
// Setup (once): link the Unity project to a Unity Cloud project (Edit > Project Settings > Services)
// and turn Vivox on in the Unity Cloud dashboard. No keys are stored in this repo.
//
// If the Vivox package is missing, every call here safely reports "not available".
public class VoiceChatService : MonoBehaviour
{
    public static VoiceChatService Instance { get; private set; }

    [Header("3D voice")]
    [SerializeField, Tooltip("Beyond this distance (metres) you cannot hear a player at all.")]
    private int audibleDistance = 32;
    [SerializeField, Tooltip("Within this distance (metres) voices play at full volume.")]
    private int conversationalDistance = 2;
    [SerializeField, Tooltip("How quickly voices fade out between the two distances (1 = normal).")]
    private float fadeIntensity = 1f;
    [SerializeField, Tooltip("Seconds between telling Vivox where you are.")]
    private float positionInterval = 0.3f;

    public string Status { get; private set; } = "Voice: not connected";
    public bool IsConnected { get; private set; }
    // How loudly the LOCAL player is talking right now (0 = silent). Used to make noise for monsters.
    public float LocalLevel { get; private set; }
    public VoiceMode Mode { get; private set; } = VoiceMode.OpenMic;
    // Set every frame by the player's voice script: is the push-to-talk key down?
    public bool PushToTalkHeld { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    // True while this player is also on the long-range radio channel (has a radio item).
    public bool OnRadio { get; private set; }
    // True while this player's voice goes ONLY to the radio channel (they are a Signal Ghost).
    public bool RadioOnly { get; private set; }

    public void SetMode(VoiceMode mode)
    {
        Mode = mode;
        ApplyTransmit();
    }

#if VIVOX_PRESENT
    private string channelName = "";
    private string radioChannelName = "";
    private bool busy;
    private float positionTimer;
    private bool transmitting = true;

    public bool IsAvailable => true;

    // ---------- Join / leave ----------

    public async Task<bool> JoinAsync(string channel, string displayName)
    {
        if (busy)
        {
            return false;
        }
        if (IsConnected && channelName == channel)
        {
            return true;
        }

        busy = true;
        Status = "Voice: connecting...";
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await VivoxService.Instance.InitializeAsync();

            LoginOptions login = new LoginOptions();
            login.DisplayName = displayName;
            await VivoxService.Instance.LoginAsync(login);

            Channel3DProperties properties = new Channel3DProperties(
                audibleDistance, conversationalDistance, fadeIntensity, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channel, ChatCapability.AudioOnly, properties);

            channelName = channel;
            IsConnected = true;
            Status = "Voice: connected";
            ApplyTransmit();
            return true;
        }
        catch (Exception e)
        {
            // Most common cause: the project is not linked to a Unity Cloud project, or Vivox is not enabled there.
            Status = "Voice failed: " + e.Message;
            Debug.LogWarning("Vivox could not start: " + e);
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    // The radio item (plan 4.5): a second, non-positional channel everyone with a radio hears at any range.
    public async Task<bool> JoinRadioAsync(string radioChannel)
    {
        if (!IsConnected || OnRadio || busy)
        {
            return OnRadio;
        }
        busy = true;
        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(radioChannel, ChatCapability.AudioOnly);
            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);
            radioChannelName = radioChannel;
            OnRadio = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not join the radio channel: " + e.Message);
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    // Signal Ghosts (plan 4.10) speak only through radios: transmit on the radio channel alone.
    public async Task SetRadioOnlyAsync(bool radioOnly)
    {
        if (!OnRadio || radioOnly == RadioOnly)
        {
            return;
        }
        try
        {
            if (radioOnly)
            {
                await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, radioChannelName);
            }
            else
            {
                await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);
            }
            RadioOnly = radioOnly;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not change voice transmission: " + e.Message);
        }
    }

    public async Task LeaveRadioAsync()
    {
        if (!OnRadio || busy)
        {
            return;
        }
        busy = true;
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(radioChannelName);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not leave the radio channel: " + e.Message);
        }
        finally
        {
            OnRadio = false;
            RadioOnly = false;
            busy = false;
        }
    }

    public async void LeaveAsync()
    {
        if (!IsConnected)
        {
            return;
        }

        IsConnected = false;
        OnRadio = false;
        LocalLevel = 0f;
        Status = "Voice: not connected";
        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
            await VivoxService.Instance.LogoutAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Vivox leave failed: " + e.Message);
        }
    }

    // ---------- Per-frame ----------

    private void Update()
    {
        if (!IsConnected)
        {
            return;
        }

        ApplyTransmit();
        LocalLevel = transmitting ? ReadOwnLevel() : 0f;
    }

    // Tells Vivox where the speaker/listener is, so voices are positioned in 3D.
    public void UpdatePosition(GameObject speakerAndListener)
    {
        if (!IsConnected)
        {
            return;
        }

        positionTimer -= Time.deltaTime;
        if (positionTimer > 0f)
        {
            return;
        }
        positionTimer = positionInterval;
        VivoxService.Instance.Set3DPosition(speakerAndListener, channelName);
    }

    private void ApplyTransmit()
    {
        bool wantTransmit = Mode == VoiceMode.OpenMic || (Mode == VoiceMode.PushToTalk && PushToTalkHeld);
        if (!IsConnected || wantTransmit == transmitting)
        {
            return;
        }

        transmitting = wantTransmit;
        if (transmitting)
        {
            VivoxService.Instance.UnmuteInputDevice();
        }
        else
        {
            VivoxService.Instance.MuteInputDevice();
        }
    }

    private float ReadOwnLevel()
    {
        if (!VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out var participants))
        {
            return 0f;
        }

        foreach (VivoxParticipant participant in participants)
        {
            if (participant.IsSelf)
            {
                return (float)participant.AudioEnergy;
            }
        }
        return 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        LeaveAsync();
    }

#else
    // Vivox package not installed: stubs so the rest of the game still compiles.
    public bool IsAvailable => false;

    public Task<bool> JoinAsync(string channel, string displayName)
    {
        Status = "Voice: Vivox package not installed";
        return Task.FromResult(false);
    }

    public void LeaveAsync() { }
    public void UpdatePosition(GameObject speakerAndListener) { }
    public Task<bool> JoinRadioAsync(string radioChannel) { return Task.FromResult(false); }
    public Task LeaveRadioAsync() { return Task.CompletedTask; }
    public Task SetRadioOnlyAsync(bool radioOnly) { return Task.CompletedTask; }
    private void ApplyTransmit() { }
#endif
}
