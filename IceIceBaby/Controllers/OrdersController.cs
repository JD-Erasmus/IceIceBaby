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
