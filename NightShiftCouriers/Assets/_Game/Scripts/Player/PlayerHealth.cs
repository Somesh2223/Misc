using UnityEngine;

// Simple health with respawn at the start point. Real death rules (Signal Ghost) come in Milestone 4.
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField, Tooltip("Seconds without damage before health regenerates.")]
    private float regenDelay = 8f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField, Tooltip("Seconds the 'you died' message shows before respawning.")]
    private float respawnDelay = 2.5f;

    private float health;
    private float timeSinceDamage;
    private float respawnTimer;
    private bool isDead;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private CharacterController controller;

    public bool IsDead => isDead;
    public float Percent => health / maxHealth;

    private void Awake()
    {
        health = maxHealth;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        controller = GetComponent<CharacterController>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
        {
            return;
        }

        health -= amount;
        timeSinceDamage = 0f;
        GameEvents.RaisePlayerDamaged(amount);

        if (health <= 0f)
        {
            health = 0f;
            isDead = true;
            respawnTimer = respawnDelay;
            GameEvents.RaisePlayerDied();
        }
    }

    private void Update()
    {
        if (isDead)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
            return;
        }

        timeSinceDamage += Time.deltaTime;
        if (timeSinceDamage > regenDelay && health < maxHealth)
        {
            health = Mathf.Min(maxHealth, health + regenPerSecond * Time.deltaTime);
        }
    }

    private void Respawn()
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controller.enabled = true;
        health = maxHealth;
        isDead = false;
    }

    private void OnGUI()
    {
        // Health bar above the stamina bar
        Rect back = new Rect(20, Screen.height - 62, 220, 14);
        GUI.color = Color.black;
        GUI.DrawTexture(back, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(back.x, back.y, back.width * Percent, back.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (isDead)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 48;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "YOU DIED", style);
        }
    }
}
