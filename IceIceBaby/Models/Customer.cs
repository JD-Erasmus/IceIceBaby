using System.ComponentModel.DataAnnotations;

namespace IceIceBaby.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }
}
