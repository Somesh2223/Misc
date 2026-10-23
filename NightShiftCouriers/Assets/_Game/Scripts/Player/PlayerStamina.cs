using UnityEngine;

// Shared stamina pool (section 4.1). Sprinting and swimming spend it,
// and later climbing and heavy carrying will too.
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField, Tooltip("Maximum stamina.")]
    private float maxStamina = 100f;

    [SerializeField, Tooltip("Stamina regained per second while resting.")]
    private float regenPerSecond = 15f;

    [SerializeField, Tooltip("Seconds to wait after spending stamina before regen starts.")]
    private float regenDelay = 1.0f;

    [SerializeField, Tooltip("Stamina needed after hitting zero before sprinting is allowed again.")]
    private float exhaustedRecoverAmount = 25f;

    private float current;
    private float regenTimer;
    private bool exhausted;

    public float Current => current;
    public float Max => maxStamina;
    public float Percent => current / maxStamina;
    public bool IsExhausted => exhausted;

    private void Awake()
    {
        current = maxStamina;
    }

    // Spend stamina continuously. Call with Time.deltaTime-scaled amounts.
    public void Spend(float amount)
    {
        current -= amount;
        regenTimer = regenDelay;

        if (current <= 0f)
        {
            current = 0f;
            exhausted = true;
        }
    }

    // Spend a lump sum (e.g. a jump). Returns false if there is not enough.
    public bool TrySpend(float amount)
    {
        if (current < amount)
        {
            return false;
        }
        Spend(amount);
        return true;
    }

    // Call once per frame. Pass true while stamina is being spent this frame.
    public void Tick(bool spendingThisFrame)
    {
        if (spendingThisFrame)
        {
            return;
        }

        if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
            return;
        }

        current = Mathf.Min(maxStamina, current + regenPerSecond * Time.deltaTime);

        if (exhausted && current >= exhaustedRecoverAmount)
        {
            exhausted = false;
        }
    }
}
