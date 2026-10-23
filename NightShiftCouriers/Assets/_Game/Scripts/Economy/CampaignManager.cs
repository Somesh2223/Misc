using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// The run across many nights (plan sections 3 and 4.15): the bank, the quota every 3 nights,
// and which phase the crew is in. SERVER-authoritative; spawned once when hosting starts and kept between shifts.
public class CampaignManager : NetworkBehaviour
{
    public static CampaignManager Instance { get; private set; }

    [Header("Run settings")]
    [SerializeField, Tooltip("Money the crew starts a run with (to buy a few items).")]
    private int startingMoney = 150;
    [SerializeField] private int startingQuota = 300;
    [SerializeField, Tooltip("Each met quota grows by this factor...")]
    private float quotaGrowth = 1.5f;
    [SerializeField, Tooltip("...plus this flat amount.")]
    private int quotaIncrease = 100;
    [SerializeField] private int nightsPerQuota = 3;

    private readonly NetworkVariable<int> bank = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> quotaProgress = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> quota = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> night = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> nightsLeft = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> phase = new NetworkVariable<int>(
        (int)ShiftPhase.Depot, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString512Bytes> resultText = new NetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int Bank => bank.Value;
    public int QuotaProgress => quotaProgress.Value;
    public int Quota => quota.Value;
    public int Night => night.Value;
    public int NightsLeft => nightsLeft.Value;
    public ShiftPhase Phase => (ShiftPhase)phase.Value;
    public bool ShopOpen => Phase == ShiftPhase.Depot;
    public string ResultText => resultText.Value.ToString();

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer)
        {
            ResetRun();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---------- Server ----------

    private void ResetRun()
    {
        bank.Value = startingMoney;
        quotaProgress.Value = 0;
        quota.Value = startingQuota;
        night.Value = 1;
        nightsLeft.Value = nightsPerQuota;
        phase.Value = (int)ShiftPhase.Depot;
    }

    public void ServerSetPhase(ShiftPhase newPhase)
    {
        phase.Value = (int)newPhase;
    }

    // Called by the ShiftSpawner when a shift ends. Pays the crew and runs the quota check.
    public void ServerEndNight(int earnings, string reason)
    {
        bank.Value += earnings;
        quotaProgress.Value += earnings;
        nightsLeft.Value -= 1;

        string text = reason + "\nEarned tonight: " + earnings + "   Bank: " + bank.Value;

        if (nightsLeft.Value <= 0)
        {
            if (quotaProgress.Value >= quota.Value)
            {
                text += "\n\nQUOTA MET (" + quotaProgress.Value + " / " + quota.Value + ")";
                quota.Value = Mathf.RoundToInt(quota.Value * quotaGrowth) + quotaIncrease;
                quotaProgress.Value = 0;
                nightsLeft.Value = nightsPerQuota;
                text += "\nNext quota: " + quota.Value + " in " + nightsPerQuota + " nights.";
                night.Value += 1;
            }
            else
            {
                text += "\n\nQUOTA MISSED (" + quotaProgress.Value + " / " + quota.Value + ")"
                        + "\nThe company lets you go. The run starts over.";
                ResetRun();
            }
        }
        else
        {
            text += "\nQuota: " + quotaProgress.Value + " / " + quota.Value + "   Nights left: " + nightsLeft.Value;
            night.Value += 1;
        }

        resultText.Value = new FixedString512Bytes(text);
        phase.Value = (int)ShiftPhase.Results;
    }

    public bool ServerTrySpend(int amount)
    {
        if (bank.Value < amount)
        {
            return false;
        }
        bank.Value -= amount;
        return true;
    }

    // A message for everyone.
    public void ServerMessage(string text)
    {
        MessageClientRpc(new FixedString128Bytes(Shorten(text)));
    }

    // A message for one player only.
    public void ServerMessage(string text, ulong clientId)
    {
        ClientRpcParams rpcParams = default;
        rpcParams.Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } };
        MessageClientRpc(new FixedString128Bytes(Shorten(text)), rpcParams);
    }

    private static string Shorten(string text)
    {
        return text.Length > 120 ? text.Substring(0, 120) : text;
    }

    [ClientRpc]
    private void MessageClientRpc(FixedString128Bytes text, ClientRpcParams rpcParams = default)
    {
        MessageFeed.Show(text.ToString());
    }
}
