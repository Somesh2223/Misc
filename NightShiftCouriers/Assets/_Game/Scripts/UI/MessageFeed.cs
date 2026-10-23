using System.Collections.Generic;
using UnityEngine;

// Short messages in the middle of the screen ("Delivered!", "The raft broke apart!").
// Anything can call MessageFeed.Show on a client; a ClientRpc usually does it.
public static class MessageFeed
{
    private class Entry
    {
        public string text;
        public float hideTime;
    }

    private static readonly List<Entry> entries = new List<Entry>();

    public static void Show(string text, float seconds = 4f)
    {
        entries.Add(new Entry { text = text, hideTime = Time.time + seconds });
        if (entries.Count > 4)
        {
            entries.RemoveAt(0);
        }
    }

    // The messages that should be visible right now.
    public static List<string> Current()
    {
        List<string> visible = new List<string>();
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (Time.time > entries[i].hideTime)
            {
                entries.RemoveAt(i);
            }
        }
        foreach (Entry entry in entries)
        {
            visible.Add(entry.text);
        }
        return visible;
    }
}
