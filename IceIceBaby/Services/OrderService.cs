using IceIceBaby.Data;
using IceIceBaby.Models;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;

    public OrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Order> CreateAsync(Order order, IEnumerable<(int productId, int qty)> lines, CancellationToken ct = default)
    {
        // Generate OrderNo: DDMMYY-### per day
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var todayStr = today.ToString("ddMMyy");
        var seq = await _db.Orders.CountAsync(o => o.OrderNo.StartsWith(todayStr + "-"), ct) + 1;
        order.OrderNo = $"{todayStr}-{seq:000}";

        var products = await _db.Products.Where(p => lines.Select(l => l.productId).Contains(p.Id)).ToListAsync(ct);
        foreach (var line in lines)
        {
            var product = products.First(p => p.Id == line.productId);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Qty = line.qty,
                UnitPriceSnapshot = product.UnitPrice,
                LineTotal = product.UnitPrice * line.qty
            });
        }
        order.Subtotal = order.Items.Sum(i => i.LineTotal);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<bool> ConfirmAsync(int orderId, byte[]? rowVersion, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null) return false;
        if (order.Status != OrderStatus.New && order.Status != OrderStatus.Confirmed) return false;
        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CancelAsync(int orderId, byte[]? rowVersion, CancellationToken ct = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null) return false;
        if (order.Status == OrderStatus.Delivered) return false;
        order.Status = OrderStatus.Canceled;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<Order?> GetAsync(int id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Customer).Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<List<Order>> ListAsync(CancellationToken ct = default)
        => _db.Orders.Include(o => o.Customer).OrderByDescending(o => o.Id).ToListAsync(ct);
}
