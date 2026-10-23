// One package that must reach one address.
public enum OrderStatus
{
    Pending,
    Delivered,
    Lost
}

public class DeliveryOrder
{
    public Package package;
    public string address;
    public OrderStatus status = OrderStatus.Pending;
    public int payout;
    public string packageName;
}
