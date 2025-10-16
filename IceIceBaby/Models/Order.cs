using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceIceBaby.Models;

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string OrderNo { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DeliveryType DeliveryType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    public DateTimeOffset? PromisedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsPaid { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, 100000)]
    public int Qty { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPriceSnapshot { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }
}
