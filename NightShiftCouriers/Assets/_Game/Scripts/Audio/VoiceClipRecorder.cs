using Unity.Netcode;
using UnityEngine;

// Owner side: when you talk, now and then send a short clip of your voice to the host
// so the Chudail can mimic you (plan 4.6). Server side: collects the pieces into VoiceClipStore.
// Turn it off with the "Chudail voice mimic" toggle in the side panel.
[RequireComponent(typeof(LocalMicrophone))]
public class VoiceClipRecorder : NetworkBehaviour
{
    [SerializeField, Tooltip("Microphone level that counts as talking.")]
    private float speakingLevel = 0.04f;
    [SerializeField, Tooltip("Seconds of talking before a clip is taken.")]
    private float speakSecondsNeeded = 0.8f;
    [SerializeField] private float clipSeconds = 1.5f;
    [SerializeField, Tooltip("Seconds between clips.")]
    private float cooldownSeconds = 20f;

    private LocalMicrophone microphone;
    private float speakingTime;
    private float cooldown;
    private int nextClipId;

    // Server: pieces of clips still arriving.
    private byte[] incoming;
    private int incomingClipId = -1;
    private int incomingReceived;

    private void Awake()
    {
        microphone = GetComponent<LocalMicrophone>();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            VoiceClipStore.Forget(OwnerClientId);
        }
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || !LocalMicrophone.ShareVoiceWithChudail || !microphone.Running)
        {
            return;
        }

        cooldown -= Time.deltaTime;
        speakingTime = microphone.Level >= speakingLevel ? speakingTime + Time.deltaTime : 0f;

        if (cooldown <= 0f && speakingTime >= speakSecondsNeeded)
        {
            cooldown = cooldownSeconds;
            speakingTime = 0f;
            SendClip();
        }
    }

    private void SendClip()
    {
        float[] samples = microphone.CopyRecent(clipSeconds, VoiceClipCodec.SampleRate);
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        byte[] bytes = VoiceClipCodec.Encode(samples);
        int clipId = nextClipId++;
        int chunks = VoiceClipCodec.ChunkCount(bytes.Length);
        for (int i = 0; i < chunks; i++)
        {
            SubmitChunkServerRpc(clipId, i, bytes.Length, VoiceClipCodec.Chunk(bytes, i));
        }
    }

    [ServerRpc]
    private void SubmitChunkServerRpc(int clipId, int index, int totalBytes, byte[] data)
    {
        if (totalBytes <= 0 || totalBytes > 32000)
        {
            return; // ignore anything unreasonably large
        }

        if (clipId != incomingClipId)
        {
            incomingClipId = clipId;
            incoming = new byte[totalBytes];
            incomingReceived = 0;
        }

        int start = index * VoiceClipCodec.ChunkSize;
        if (start + data.Length > incoming.Length)
        {
            return;
        }
        System.Array.Copy(data, 0, incoming, start, data.Length);
        incomingReceived += data.Length;

        if (incomingReceived >= incoming.Length)
        {
            VoiceClipStore.Add(OwnerClientId, incoming);
            incomingClipId = -1;
        }
    }
}
