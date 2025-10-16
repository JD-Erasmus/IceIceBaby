using IceIceBaby.Data;
using IceIceBaby.Models;
using IceIceBaby.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Clerk,Manager")]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;

    public PaymentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Payments
    public async Task<IActionResult> Index()
    {
        var recentPayments = await _db.Payments
            .Include(p => p.Order)!.ThenInclude(o => o!.Customer)
            .OrderByDescending(p => p.PaidAt)
            .Take(25)
            .AsNoTracking()
            .ToListAsync();

        var outstandingOrders = await _db.Orders
            .Include(o => o.Customer)
            .Where(o => !o.IsPaid && o.Status != OrderStatus.Canceled)
            .OrderBy(o => o.PromisedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(o => o.OrderNo)
            .Take(25)
            .AsNoTracking()
            .ToListAsync();

        var vm = new PaymentsIndexViewModel
        {
            RecentPayments = recentPayments,
            OutstandingOrders = outstandingOrders
        };

        return View(vm);
    }

    // GET: /Payments/Create
    public async Task<IActionResult> Create(int? orderId)
    {
        var dto = new RecordPaymentDto();
        if (orderId.HasValue)
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId.Value);
            if (order != null && !order.IsPaid && order.Status != OrderStatus.Canceled)
            {
                dto.OrderId = order.Id;
                dto.Amount = order.Subtotal;
            }
        }

        await LoadOutstandingOrdersAsync(dto.OrderId);
        return View(dto);
    }

    // POST: /Payments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecordPaymentDto dto)
    {
        await LoadOutstandingOrdersAsync(dto.OrderId);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var order = await _db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == dto.OrderId!.Value);
        if (order == null)
        {
            ModelState.AddModelError(nameof(dto.OrderId), "Order not found.");
            return View(dto);
        }

        if (order.IsPaid)
        {
            ModelState.AddModelError(nameof(dto.OrderId), "Order is already marked as paid.");
            return View(dto);
        }

        if (order.Status == OrderStatus.Canceled)
        {
            ModelState.AddModelError(nameof(dto.OrderId), "Cannot record payment for a canceled order.");
            return View(dto);
        }

        if (dto.Amount < order.Subtotal)
        {
            ModelState.AddModelError(nameof(dto.Amount), "Amount is less than the order subtotal.");
            return View(dto);
        }

        DateTimeOffset paidAt;
        if (dto.PaidAt == default)
        {
            paidAt = DateTimeOffset.Now;
        }
        else
        {
            var localDateTime = DateTime.SpecifyKind(dto.PaidAt, DateTimeKind.Local);
            paidAt = new DateTimeOffset(localDateTime);
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = dto.Amount,
            Method = dto.Method,
            PaidAt = paidAt,
            RecordedBy = User?.Identity?.Name ?? "System"
        };

        order.IsPaid = true;
        order.PaymentMethod = dto.Method;
        order.PaidAt = paidAt;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Payment recorded for order {order.OrderNo}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOutstandingOrdersAsync(int? selectedOrderId = null)
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Where(o => !o.IsPaid && o.Status != OrderStatus.Canceled)
            .OrderBy(o => o.OrderNo)
            .Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.OrderNo} - {o.Customer!.Name} ({o.Subtotal:C})",
                Selected = selectedOrderId.HasValue && o.Id == selectedOrderId.Value
            })
            .ToListAsync();

        ViewBag.OutstandingOrders = orders;
    }
}
