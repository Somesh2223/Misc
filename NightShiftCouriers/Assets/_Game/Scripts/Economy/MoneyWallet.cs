using System;
using UnityEngine;

// The crew's money for the shift. (Server-owned once networking arrives in Milestone 2.)
public class MoneyWallet : MonoBehaviour
{
    private int balance;

    public int Balance => balance;
    public event Action<int> BalanceChanged;

    public void Add(int amount)
    {
        balance += amount;
        BalanceChanged?.Invoke(balance);
    }
}
