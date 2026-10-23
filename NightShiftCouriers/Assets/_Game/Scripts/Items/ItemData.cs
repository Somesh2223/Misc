using UnityEngine;

// Tweakable item stats (plan rule 4). Create via Assets > Create > Night Shift > Item Data.
[CreateAssetMenu(fileName = "NewItem", menuName = "Night Shift/Item Data")]
public class ItemData : ScriptableObject
{
    public string displayName = "Item";
    public ItemType type;
    [Tooltip("Shop price.")]
    public int price = 50;
    [TextArea] public string description = "";
    [Tooltip("Uses before the item is used up. Flashlight: seconds of battery.")]
    public int uses = 1;
    [Tooltip("Salt ward / charm: how long the effect lasts (seconds).")]
    public float duration = 60f;
    [Tooltip("Charm only: the monster type that cannot see you.")]
    public MonsterKind charmAgainst = MonsterKind.Vetala;
    public Color color = Color.white;
    [Tooltip("Shown in the depot shop. Off for things you can only find (courier badges).")]
    public bool inShop = true;
}
