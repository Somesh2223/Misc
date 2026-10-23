using System.Collections.Generic;
using UnityEngine;

// SERVER memory of each player's last few voice clips, for the Chudail to copy (plan 4.12).
// Kept only in memory for this hosted session; never saved or uploaded.
public static class VoiceClipStore
{
    private const int ClipsPerPlayer = 3;

    private static readonly Dictionary<ulong, List<byte[]>> clips = new Dictionary<ulong, List<byte[]>>();

    public static void Add(ulong clientId, byte[] clip)
    {
        if (!clips.TryGetValue(clientId, out List<byte[]> list))
        {
            list = new List<byte[]>();
            clips[clientId] = list;
        }
        list.Add(clip);
        if (list.Count > ClipsPerPlayer)
        {
            list.RemoveAt(0);
        }
    }

    public static bool TryGetRandom(ulong clientId, out byte[] clip)
    {
        clip = null;
        if (!clips.TryGetValue(clientId, out List<byte[]> list) || list.Count == 0)
        {
            return false;
        }
        clip = list[Random.Range(0, list.Count)];
        return true;
    }

    public static void Forget(ulong clientId)
    {
        clips.Remove(clientId);
    }
}
