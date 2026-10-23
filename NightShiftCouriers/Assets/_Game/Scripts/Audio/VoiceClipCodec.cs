using UnityEngine;

// Turns short voice clips into small byte arrays (8-bit, 8 kHz) and back, to send them over the network.
public static class VoiceClipCodec
{
    public const int SampleRate = 8000;
    public const int ChunkSize = 1000; // bytes per network message

    public static byte[] Encode(float[] samples)
    {
        byte[] bytes = new byte[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float value = Mathf.Clamp(samples[i] * 2f, -1f, 1f); // a little gain, mic levels are low
            bytes[i] = (byte)Mathf.RoundToInt((value * 0.5f + 0.5f) * 255f);
        }
        return bytes;
    }

    public static AudioClip Decode(byte[] bytes, string name)
    {
        float[] samples = new float[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            samples[i] = (bytes[i] / 255f - 0.5f) * 2f;
        }
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Splits a clip into network-sized pieces.
    public static byte[] Chunk(byte[] all, int index)
    {
        int start = index * ChunkSize;
        int length = Mathf.Min(ChunkSize, all.Length - start);
        byte[] chunk = new byte[length];
        System.Array.Copy(all, start, chunk, 0, length);
        return chunk;
    }

    public static int ChunkCount(int byteCount)
    {
        return (byteCount + ChunkSize - 1) / ChunkSize;
    }
}
