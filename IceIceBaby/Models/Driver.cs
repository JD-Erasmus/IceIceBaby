using System.ComponentModel.DataAnnotations;

namespace IceIceBaby.Models;

public class Driver
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }
}
