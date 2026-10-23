using UnityEngine;
using UnityEngine.InputSystem;

// Connects the local player to the crew's voice channel (Vivox) and keeps their 3D position up to date.
// Only the owner runs this (NetworkPlayer enables it). Hold V to talk in push-to-talk mode.
[RequireComponent(typeof(NetworkPlayer))]
public class PlayerVoice : MonoBehaviour
{
    [SerializeField, Tooltip("Seconds between connection attempts if voice fails.")]
    private float retrySeconds = 10f;

    private NetworkPlayer networkPlayer;
    private bool joining;
    private float retryTimer;

    private void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    private async void Update()
    {
        VoiceChatService voice = VoiceChatService.Instance;
        if (voice == null || !voice.IsAvailable)
        {
            return;
        }

        if (!voice.IsConnected)
        {
            retryTimer -= Time.deltaTime;
            string channel = networkPlayer.VoiceChannel;
            if (!joining && retryTimer <= 0f && channel != "")
            {
                joining = true;
                bool ok = await voice.JoinAsync(channel, "Courier " + networkPlayer.OwnerClientId);
                joining = false;
                if (!ok)
                {
                    retryTimer = retrySeconds;
                }
            }
            return;
        }

        voice.PushToTalkHeld = Keyboard.current != null && Keyboard.current.vKey.isPressed;
        voice.UpdatePosition(gameObject);
    }

    private void OnDisable()
    {
        if (VoiceChatService.Instance != null)
        {
            VoiceChatService.Instance.LeaveAsync();
        }
    }
}
