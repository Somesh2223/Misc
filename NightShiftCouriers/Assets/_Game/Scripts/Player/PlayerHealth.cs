using Unity.Netcode;
using UnityEngine;

// Server-authoritative health with respawn at the spawn point. Real death rules (Signal Ghost) come in Milestone 4.
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Tooltip("Seconds without damage before health regenerates.")]
    private float regenDelay = 8f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField, Tooltip("Seconds the 'you died' message shows before respawning.")]
    private float respawnDelay = 2.5f;

    private readonly NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> dead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float timeSinceDamage;
    private float respawnTimer;

    public bool IsDead => dead.Value;
    public float Percent => health.Value / maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            dead.Value = false;
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
            dead.Value = true;
            respawnTimer = respawnDelay;

            PlayerGrabber grabber = GetComponent<PlayerGrabber>();
            if (grabber != null)
            {
                grabber.ServerDrop();
            }
            GameEvents.RaisePlayerDied();
        }
    }

    // Server only: full heal (used when a shift restarts).
    public void ServerRevive()
    {
        if (!IsServer)
        {
            return;
        }
        health.Value = maxHealth;
        dead.Value = false;
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (dead.Value)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                health.Value = maxHealth;
                dead.Value = false;
                GetComponent<NetworkPlayer>().ServerRespawn();
            }
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

        if (dead.Value)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 48;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "YOU DIED", style);
        }
    }
}
