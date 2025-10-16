using IceIceBaby.Data;
using IceIceBaby.Models;
using IceIceBaby.Models.DTOs;
using IceIceBaby.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Clerk,Manager")]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;
    private readonly ApplicationDbContext _db;

    public OrdersController(IOrderService orders, ApplicationDbContext db)
    {
        _orders = orders;
        _db = db;
    }

    // GET: /Orders
    public async Task<IActionResult> Index()
    {
        var list = await _orders.ListAsync();
        return View(list);
    }

    // GET: /Orders/History
    public async Task<IActionResult> History(string? order, string? customer, string? status, DateOnly? from, DateOnly? to)
    {
        var filter = new OrderHistoryFilter
        {
            Order = order,
            Customer = customer,
            Status = status,
            From = from?.ToString("yyyy-MM-dd"),
            To = to?.ToString("yyyy-MM-dd")
        };

        var query = _db.Orders
            .Include(o => o.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(order))
        {
            var term = order.Trim();
            query = query.Where(o => o.OrderNo.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(customer))
        {
            var term = customer.Trim();
            query = query.Where(o => o.Customer != null && o.Customer.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        if (from.HasValue)
        {
            var fromDateTime = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
            var fromValue = new DateTimeOffset(fromDateTime);
            query = query.Where(o => (o.PromisedAt ?? o.PaidAt ?? DateTimeOffset.MinValue) >= fromValue);
        }

        if (to.HasValue)
        {
            var toDateTime = DateTime.SpecifyKind(to.Value.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Local);
            var toValue = new DateTimeOffset(toDateTime);
            query = query.Where(o => (o.PromisedAt ?? o.PaidAt ?? DateTimeOffset.MaxValue) <= toValue);
        }

        var totalMatches = await query.CountAsync();

        var results = await query
            .OrderByDescending(o => o.PromisedAt ?? o.PaidAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(o => o.Id)
            .Take(100)
            .ToListAsync();

        var customers = await _db.Customers
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Name,
                Text = c.Name,
                Selected = !string.IsNullOrWhiteSpace(customer) && string.Equals(c.Name, customer, StringComparison.OrdinalIgnoreCase)
            })
            .ToListAsync();

        var vm = new OrderHistoryViewModel
        {
            Filter = filter,
            Results = results,
            TotalMatches = totalMatches,
            Customers = customers
        };

        return View(vm);
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.Customers = await _db.Customers
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync();
        ViewBag.Products = await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name + " (" + p.UnitPrice.ToString("C") + ")" })
            .ToListAsync();
    }

    // GET: /Orders/Create
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        var vm = new CreateOrderDto
        {
            Lines = new List<CreateOrderLineDto> { new CreateOrderLineDto { Qty = 1 } }
        };
        return View(vm);
    }

    // POST: /Orders/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            ModelState.AddModelError("Lines", "Add at least one item.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(dto);
        }

        var order = new Order
        {
            CustomerId = dto.CustomerId,
            DeliveryType = dto.DeliveryType,
            PromisedAt = dto.PromisedAt,
            Notes = dto.Notes
        };
        var created = await _orders.CreateAsync(order, dto.Lines.Select(l => (l.ProductId, l.Qty)));
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    // GET: /Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    // GET: /Orders/Invoice/5
    public async Task<IActionResult> Invoice(int id)
    {
        var order = await _orders.GetAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    // POST: /Orders/Confirm/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, byte[]? rowVersion)
    {
        var ok = await _orders.ConfirmAsync(id, rowVersion);
        if (!ok) TempData["Error"] = "Unable to confirm order.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Orders/ReadyForPickup/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadyForPickup(int id, string? returnUrl)
    {
        var ok = await _orders.MarkReadyForPickupAsync(id);
        TempData[ok ? "Message" : "Error"] = ok
            ? "Order marked as ready for pickup."
            : "Unable to mark order as ready for pickup.";
        var redirectUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Details), new { id })!;
        return Redirect(redirectUrl);
    }

    // POST: /Orders/MarkCollected/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCollected(int id, string? returnUrl)
    {
        var ok = await _orders.MarkCollectedAsync(id);
        TempData[ok ? "Message" : "Error"] = ok
            ? "Order marked as collected."
            : "Unable to mark order as collected.";
        var redirectUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Details), new { id })!;
        return Redirect(redirectUrl);
    }

    // POST: /Orders/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, byte[]? rowVersion)
    {
        var ok = await _orders.CancelAsync(id, rowVersion);
        if (!ok) TempData["Error"] = "Unable to cancel order.";
        return RedirectToAction(nameof(Index));
    }
}
