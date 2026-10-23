using System;
using UnityEngine;

// The depot journal's entries (plan 4.8). They unlock over nights through deliveries and cursed packages.
[CreateAssetMenu(fileName = "LoreBook", menuName = "Night Shift/Lore Book")]
public class LoreBook : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string title = "";
        [TextArea(3, 10)] public string text = "";
    }

    public Entry[] entries;

    public Entry Get(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length)
        {
            return null;
        }
        return entries[index];
    }
}
