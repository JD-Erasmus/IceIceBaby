namespace IceIceBaby.Models;

public enum OrderStatus { New, Confirmed, OutForDelivery, Delivered, Canceled }
public enum DeliveryType { Delivery, Pickup }
public enum PaymentMethod { Cash, EFT }
public enum DeliveryRunStatus { New, InProgress, Completed }
