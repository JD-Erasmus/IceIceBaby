using System.ComponentModel.DataAnnotations;
using IceIceBaby.Models;

namespace IceIceBaby.Models.DTOs;

public class CreateOrderDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public DeliveryType DeliveryType { get; set; }

    public DateTimeOffset? PromisedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<CreateOrderLineDto> Lines { get; set; } = new();
}

public class CreateOrderLineDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100000)]
    public int Qty { get; set; }
}

public class CreateRunDto
{
    [Required]
    public DateOnly RunDate { get; set; }

    [Required, MaxLength(100)]
    public string DriverName { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Vehicle { get; set; }

    [MinLength(1, ErrorMessage = "Select at least one confirmed order")]
    public List<int> OrderIds { get; set; } = new();
}

public class RecordPaymentDto
{
    [Required]
    [Display(Name = "Order")]
    [Range(1, int.MaxValue, ErrorMessage = "Select an order to continue.")]
    public int? OrderId { get; set; }

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [Display(Name = "Paid At")]
    public DateTime PaidAt { get; set; } = DateTime.Now;
}

public class OrderHistoryFilter
{
    public string? Order { get; set; }
    public string? Customer { get; set; }
    public string? Status { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}

