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
