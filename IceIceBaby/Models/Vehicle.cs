using System.ComponentModel.DataAnnotations;

namespace IceIceBaby.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty; // e.g., Truck 1 or plate number

    [MaxLength(20)]
    public string? Plate { get; set; }
}
