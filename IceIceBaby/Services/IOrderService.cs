using IceIceBaby.Models;

namespace IceIceBaby.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Order order, IEnumerable<(int productId, int qty)> lines, CancellationToken ct = default);
    Task<bool> ConfirmAsync(int orderId, byte[]? rowVersion, CancellationToken ct = default);
    Task<bool> CancelAsync(int orderId, byte[]? rowVersion, CancellationToken ct = default);
    Task<bool> MarkReadyForPickupAsync(int orderId, CancellationToken ct = default);
    Task<bool> MarkCollectedAsync(int orderId, CancellationToken ct = default);
    Task<Order?> GetAsync(int id, CancellationToken ct = default);
    Task<List<Order>> ListAsync(CancellationToken ct = default);
}
