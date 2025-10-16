using IceIceBaby.Data;
using IceIceBaby.Models;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Services;

public class RunService : IRunService
{
    private readonly ApplicationDbContext _db;

    public RunService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DeliveryRun> CreateRunAsync(DateOnly runDate, string driverName, string? vehicle, IEnumerable<int> orderIds, CancellationToken ct = default)
    {
        var orders = await _db.Orders.Where(o => orderIds.Contains(o.Id)).ToListAsync(ct);
        if (!orders.Any() || orders.Any(o => o.Status != OrderStatus.Confirmed))
            throw new InvalidOperationException("Run must include only confirmed orders");

        var run = new DeliveryRun
        {
            RunDate = runDate,
            DriverName = driverName,
            Vehicle = vehicle,
            Status = DeliveryRunStatus.New
        };
        _db.DeliveryRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        int seq = 1;
        foreach (var o in orders)
        {
            _db.DeliveryStops.Add(new DeliveryStop
            {
                DeliveryRunId = run.Id,
                OrderId = o.Id,
                Seq = seq++
            });
            o.Status = OrderStatus.OutForDelivery;
        }
        await _db.SaveChangesAsync(ct);
        return run;
    }

    public Task<DeliveryRun?> GetRunAsync(int id, CancellationToken ct = default)
        => _db.DeliveryRuns.Include(r => r.Stops).ThenInclude(s => s.Order).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<DeliveryRun>> ListRunsAsync(CancellationToken ct = default)
        => _db.DeliveryRuns.OrderByDescending(r => r.RunDate).ToListAsync(ct);

    public async Task<bool> MarkDeliveredAsync(int runId, int orderId, DateTimeOffset when, string? podNote, string? podPhotoPath, CancellationToken ct = default)
    {
        var stop = await _db.DeliveryStops.Include(s => s.Order).Include(s => s.Run)
            .FirstOrDefaultAsync(s => s.DeliveryRunId == runId && s.OrderId == orderId, ct);
        if (stop == null) return false;
        if (stop.DeliveredAt != null) return true; // already delivered
        stop.DeliveredAt = when;
        stop.PodNote = podNote;
        stop.PodPhotoPath = podPhotoPath;
        if (stop.Order != null)
        {
            stop.Order.Status = OrderStatus.Delivered;
        }

        await _db.SaveChangesAsync(ct);

        // Auto-complete run if all delivered
        var allDelivered = await _db.DeliveryStops.Where(s => s.DeliveryRunId == runId).AllAsync(s => s.DeliveredAt != null, ct);
        if (allDelivered)
        {
            var run = await _db.DeliveryRuns.FirstAsync(r => r.Id == runId, ct);
            run.Status = DeliveryRunStatus.Completed;
            await _db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<bool> AddOrderAsync(int runId, int orderId, CancellationToken ct = default)
    {
        var run = await _db.DeliveryRuns.Include(r => r.Stops).FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run == null) return false;

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null || order.Status != OrderStatus.Confirmed) return false;

        var alreadyLinked = await _db.DeliveryStops.AnyAsync(s => s.OrderId == orderId, ct);
        if (alreadyLinked) return false;

        var nextSeq = (run.Stops.Any() ? run.Stops.Max(s => s.Seq) : 0) + 1;
        _db.DeliveryStops.Add(new DeliveryStop
        {
            DeliveryRunId = runId,
            OrderId = orderId,
            Seq = nextSeq
        });
        order.Status = OrderStatus.OutForDelivery;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
