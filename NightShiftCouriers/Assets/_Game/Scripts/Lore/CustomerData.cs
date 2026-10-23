using UnityEngine;

// A customer and the note on their order (plan 4.8). Create via Assets > Create > Night Shift > Customer.
[CreateAssetMenu(fileName = "NewCustomer", menuName = "Night Shift/Customer")]
public class CustomerData : ScriptableObject
{
    public string customerName = "Customer";
    [Tooltip("Must match the DeliveryPoint address exactly.")]
    public string address = "";
    [TextArea] public string note = "";
    public CustomerRule rule = CustomerRule.None;
    [Tooltip("Payout multiplier when the rule is followed.")]
    public float ruleBonus = 1.5f;
    [Tooltip("Journal entry unlocked by delivering here (-1 = none). Index into the LoreBook.")]
    public int loreEntry = -1;
}
