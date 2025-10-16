using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceIceBaby.Models;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public DateTimeOffset PaidAt { get; set; }

    [MaxLength(120)]
    public string? RecordedBy { get; set; }
}
