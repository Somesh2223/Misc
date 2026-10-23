using System;
using UnityEngine;

// Weighted outcomes for opening a cursed package (plan 4.9). Tweak the weights in the Inspector.
[CreateAssetMenu(fileName = "CurseTable", menuName = "Night Shift/Curse Table")]
public class CurseTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public CurseOutcome outcome;
        [Tooltip("Relative chance. Higher = more likely.")]
        public float weight = 1f;
        [Tooltip("What everyone is told when this happens.")]
        public string message = "";
    }

    public Entry[] entries;
    [Tooltip("Money for the BigMoney outcome.")]
    public int bigMoneyAmount = 150;

    public Entry Roll()
    {
        float total = 0f;
        foreach (Entry entry in entries)
        {
            total += entry.weight;
        }

        float roll = UnityEngine.Random.value * total;
        foreach (Entry entry in entries)
        {
            roll -= entry.weight;
            if (roll <= 0f)
            {
                return entry;
            }
        }
        return entries[entries.Length - 1];
    }
}
