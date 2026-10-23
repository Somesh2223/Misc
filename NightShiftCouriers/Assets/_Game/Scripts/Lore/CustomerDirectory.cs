using UnityEngine;

// Every customer, so any machine can look up the note for an address.
[CreateAssetMenu(fileName = "CustomerDirectory", menuName = "Night Shift/Customer Directory")]
public class CustomerDirectory : ScriptableObject
{
    public CustomerData[] customers;

    public CustomerData Find(string address)
    {
        if (customers == null)
        {
            return null;
        }
        foreach (CustomerData customer in customers)
        {
            if (customer != null && customer.address == address)
            {
                return customer;
            }
        }
        return null;
    }
}
