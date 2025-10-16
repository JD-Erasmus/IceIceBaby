namespace IceIceBaby.Models;

public enum OrderStatus
{
    New = 0,
    Confirmed = 1,
    OutForDelivery = 2,
    Delivered = 3,
    Canceled = 4,
    ReadyForPickup = 5,
    PickedUp = 6
}
public enum DeliveryType { Delivery, Pickup }
public enum PaymentMethod { Cash, EFT }
public enum DeliveryRunStatus { New, InProgress, Completed }
