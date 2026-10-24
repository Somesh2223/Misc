using Unity.Netcode;
using UnityEngine;

// Server-authoritative health (plan 4.10). At 0 the player becomes a Signal Ghost:
// they drop their courier ID badge, and stay a ghost until a teammate carries the badge
// to a revive shrine, or the crew returns to the depot.
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Tooltip("Seconds without damage before health regenerates.")]
    private float regenDelay = 8f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField, Tooltip("Health after being revived at a shrine (fraction of max).")]
    private float reviveHealthFraction = 0.5f;

    private readonly NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> dead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float timeSinceDamage;

    public bool IsDead => dead.Value;
    public float Percent => health.Value / maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            dead.Value = false;
        }
        health.OnValueChanged += OnHealthChanged;
        dead.OnValueChanged += OnDeadChanged;
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        dead.OnValueChanged -= OnDeadChanged;
    }

    // Body cam highlights on the victim's own machine (plan 4.11).
    private void OnHealthChanged(float previous, float current)
    {
        if (IsOwner && current < previous - 10f && current > 0f)
        {
            HighlightMoments.Flag("Attacked!", 60);
        }
    }

    private void OnDeadChanged(bool previous, bool current)
    {
        if (IsOwner && current)
        {
            HighlightMoments.Flag("Died", 100);
            MessageFeed.Show("You are a Signal Ghost now. A teammate can carry your badge to a shrine to revive you.", 8f);
        }
    }

    // Server only. Anything that hurts a player calls this.
    public void TakeDamage(float amount)
    {
        if (!IsServer || dead.Value)
        {
            return;
        }

        health.Value = Mathf.Max(0f, health.Value - amount);
        timeSinceDamage = 0f;
        GameEvents.RaisePlayerDamaged(amount);

        if (health.Value <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        dead.Value = true;

        PlayerGrabber grabber = GetComponent<PlayerGrabber>();
        if (grabber != null)
        {
            grabber.ServerDrop();
        }
        GetComponent<PlayerInventory>().ServerSetFlashlight(false);

        // Drop the courier ID badge where we fell, so a teammate can bring us back.
        NetworkPlayer player = GetComponent<NetworkPlayer>();
        if (ShiftSpawner.Instance != null)
        {
            ShiftSpawner.Instance.ServerDropBadge(player.Position + Vector3.up * 0.3f, OwnerClientId);
        }
        GameEvents.RaisePlayerDied();
    }

    // Server only: full heal (used when the crew returns to the depot).
    public void ServerRevive()
    {
        if (!IsServer)
        {
            return;
        }
        health.Value = maxHealth;
        dead.Value = false;
    }

    // Server only: back from the dead at a shrine, weakened.
    public void ServerReviveAt(Vector3 position)
    {
        if (!IsServer || !dead.Value)
        {
            return;
        }
        health.Value = maxHealth * reviveHealthFraction;
        dead.Value = false;
        GetComponent<NetworkPlayer>().ServerTeleport(position, 0f);
        RevivedClientRpc(OwnerOnly());
    }

    [ClientRpc]
    private void RevivedClientRpc(ClientRpcParams rpcParams = default)
    {
        // Revived "at reduced stamina" (plan 4.10).
        PlayerStamina stamina = GetComponent<PlayerStamina>();
        stamina.Spend(stamina.Current * 0.7f);
        MessageFeed.Show("You're back! Weak and out of breath, but alive.");
    }

    private ClientRpcParams OwnerOnly()
    {
        ClientRpcParams rpcParams = default;
        rpcParams.Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } };
        return rpcParams;
    }

    private void Update()
    {
        if (!IsServer || dead.Value)
        {
            return;
        }

        timeSinceDamage += Time.deltaTime;
        if (timeSinceDamage > regenDelay && health.Value < maxHealth)
        {
            health.Value = Mathf.Min(maxHealth, health.Value + regenPerSecond * Time.deltaTime);
        }
    }

    private void OnGUI()
    {
        if (UIState.HideHud)
        {
            return; // F1: screenshot mode
        }
        if (!IsSpawned || !IsOwner)
        {
            return;
        }

        // Health bar above the stamina bar
        Rect back = new Rect(20, Screen.height - 62, 220, 14);
        GUI.color = Color.black;
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(back.x, back.y, back.width * Percent, back.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
