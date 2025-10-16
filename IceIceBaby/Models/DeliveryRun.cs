using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IceIceBaby.Models;

public class DeliveryRun
{
    public int Id { get; set; }

    public DateOnly RunDate { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    [MaxLength(60)]
    public string? Vehicle { get; set; }

    public DeliveryRunStatus Status { get; set; } = DeliveryRunStatus.New; // New, InProgress, Completed

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<DeliveryStop> Stops { get; set; } = new List<DeliveryStop>();
}

public class DeliveryStop
{
    public int Id { get; set; }

    [Required]
    public int DeliveryRunId { get; set; }
    public DeliveryRun? Run { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int Seq { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    [MaxLength(300)]
    public string? PodNote { get; set; }

    [MaxLength(300)]
    public string? PodPhotoPath { get; set; }
}
