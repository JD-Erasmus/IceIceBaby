using System.Collections.Generic;
using IceIceBaby.Models.DTOs;

namespace IceIceBaby.Models;

public class PaymentsIndexViewModel
{
    public List<Payment> RecentPayments { get; set; } = new();
    public List<Order> OutstandingOrders { get; set; } = new();
}

public class OrderHistoryViewModel
{
    public OrderHistoryFilter Filter { get; set; } = new();
    public List<Order> Results { get; set; } = new();
    public int TotalMatches { get; set; }
}

