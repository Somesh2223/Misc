using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Tracks the night's orders: pick up -> deliver -> get paid (Milestone 1 delivery loop).
// Orders are built from every Package in the scene that has a destination address.
public class DeliveryManager : MonoBehaviour
{
    [Header("Payout")]
    [SerializeField, Tooltip("Money needed by the end of the shift to meet the quota.")]
    private int quota = 400;
    [SerializeField, Tooltip("Deliver faster than this many seconds for a speed bonus.")]
    private float parSeconds = 240f;
    [SerializeField, Tooltip("Largest speed bonus (0.25 = +25%).")]
    private float maxSpeedBonus = 0.25f;

    [Header("Cleanup")]
    [SerializeField, Tooltip("Packages that fall below this height are counted as lost.")]
    private float killHeight = -20f;

    private readonly List<DeliveryOrder> orders = new List<DeliveryOrder>();
    private MoneyWallet wallet;
    private string lastMessage = "";
    private float messageTimer;
    private bool shiftOver;

    public IReadOnlyList<DeliveryOrder> Orders => orders;
    public int Quota => quota;
    public int Money => wallet.Balance;
    public bool ShiftOver => shiftOver;
    public bool QuotaMet => wallet.Balance >= quota;
    public string Message => messageTimer > 0f ? lastMessage : "";

    private void Awake()
    {
        wallet = GetComponent<MoneyWallet>();
        if (wallet == null)
        {
            wallet = gameObject.AddComponent<MoneyWallet>();
        }
    }

    private void Start()
    {
        Package[] packages = FindObjectsByType<Package>(FindObjectsSortMode.None);
        foreach (Package package in packages)
        {
            if (string.IsNullOrEmpty(package.DestinationAddress))
            {
                continue;
            }

            DeliveryOrder order = new DeliveryOrder();
            order.package = package;
            order.address = package.DestinationAddress;
            order.packageName = package.Data.displayName;
            orders.Add(order);
        }
    }

    private void Update()
    {
        messageTimer -= Time.deltaTime;

        CheckForLostPackages();
        CheckShiftOver();

        if (shiftOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Called by a DeliveryPoint when a loose package sits inside it.
    public void TryDeliver(Package package, DeliveryPoint point)
    {
        DeliveryOrder order = FindOrder(package);
        if (order == null || order.status != OrderStatus.Pending)
        {
            return;
        }

        if (order.address != point.Address)
        {
            ShowMessage("Wrong address! This one goes to " + order.address + ".");
            return;
        }

        int payout = CalculatePayout(package);
        order.status = OrderStatus.Delivered;
        order.payout = payout;
        package.MarkDelivered();
        wallet.Add(payout);
        GameEvents.RaisePackageDelivered(package, payout);

        ShowMessage("Delivered to " + order.address + "!  +" + payout);
        Destroy(package.gameObject, 0.3f);
    }

    // Money = base value x condition x speed bonus (plan section 4.15).
    public int CalculatePayout(Package package)
    {
        float value = package.Data.baseValue * package.Condition;

        float timeLeftFraction = Mathf.Clamp01((parSeconds - package.AgeSeconds) / parSeconds);
        float speedBonus = 1f + maxSpeedBonus * timeLeftFraction;

        return Mathf.RoundToInt(value * speedBonus);
    }

    // The address of the held package, if any (used by the HUD).
    public string FindAddressFor(Package package)
    {
        DeliveryOrder order = FindOrder(package);
        return order == null ? "" : order.address;
    }

    private DeliveryOrder FindOrder(Package package)
    {
        foreach (DeliveryOrder order in orders)
        {
            if (order.package == package)
            {
                return order;
            }
        }
        return null;
    }

    private void CheckForLostPackages()
    {
        foreach (DeliveryOrder order in orders)
        {
            if (order.status != OrderStatus.Pending)
            {
                continue;
            }

            bool gone = order.package == null;
            bool fellOut = !gone && order.package.transform.position.y < killHeight;
            if (gone || fellOut)
            {
                order.status = OrderStatus.Lost;
                if (fellOut)
                {
                    Destroy(order.package.gameObject);
                }
                ShowMessage("A package was lost!");
            }
        }
    }

    private void CheckShiftOver()
    {
        if (shiftOver || orders.Count == 0)
        {
            return;
        }

        foreach (DeliveryOrder order in orders)
        {
            if (order.status == OrderStatus.Pending)
            {
                return;
            }
        }
        shiftOver = true;
    }

    private void ShowMessage(string text)
    {
        lastMessage = text;
        messageTimer = 4f;
    }
}
