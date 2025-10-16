using System.ComponentModel.DataAnnotations;

namespace IceIceBaby.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 9999999)]
    [DataType(DataType.Currency)]
    public decimal UnitPrice { get; set; }
}
