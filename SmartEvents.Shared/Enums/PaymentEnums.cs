namespace SmartEvents.Shared.Enums;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    Stripe,
    AirtelMoney,
    MTNMoMo,
    Free
}
