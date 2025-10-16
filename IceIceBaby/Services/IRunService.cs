using IceIceBaby.Models;

namespace IceIceBaby.Services;

public interface IRunService
{
    Task<DeliveryRun> CreateRunAsync(DateOnly runDate, string driverName, string? vehicle, IEnumerable<int> orderIds, CancellationToken ct = default);
    Task<DeliveryRun?> GetRunAsync(int id, CancellationToken ct = default);
    Task<List<DeliveryRun>> ListRunsAsync(CancellationToken ct = default);
    Task<bool> MarkDeliveredAsync(int runId, int orderId, DateTimeOffset when, string? podNote, string? podPhotoPath, CancellationToken ct = default);
    Task<bool> AddOrderAsync(int runId, int orderId, CancellationToken ct = default);
    Task<bool> SetStatusAsync(int runId, DeliveryRunStatus status, CancellationToken ct = default);
}
