using IceIceBaby.Data;
using IceIceBaby.Models;
using IceIceBaby.Models.DTOs;
using IceIceBaby.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Driver,Manager")]
public class DeliveryRunsController : Controller
{
    private readonly IRunService _runs;
    private readonly IStorageService _storage;
    private readonly ILogger<DeliveryRunsController> _logger;
    private readonly ApplicationDbContext _db;

    public DeliveryRunsController(IRunService runs, IStorageService storage, ILogger<DeliveryRunsController> logger, ApplicationDbContext db)
    {
        _runs = runs;
        _storage = storage;
        _logger = logger;
        _db = db;
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.Drivers = await _db.Drivers
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem { Value = d.Name, Text = d.Name })
            .ToListAsync();
        ViewBag.Vehicles = await _db.Vehicles
            .OrderBy(v => v.Name)
            .Select(v => new SelectListItem { Value = v.Name, Text = string.IsNullOrWhiteSpace(v.Plate) ? v.Name : $"{v.Name} ({v.Plate})" })
            .ToListAsync();

        var eligibleOrderIds = await _db.Orders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .Select(o => o.Id)
            .ToListAsync();
        var alreadyInStops = await _db.DeliveryStops.Select(s => s.OrderId).ToListAsync();
        var openOrders = await _db.Orders
            .Where(o => eligibleOrderIds.Contains(o.Id) && !alreadyInStops.Contains(o.Id))
            .Include(o => o.Customer)
            .OrderBy(o => o.PromisedAt)
            .Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.OrderNo} - {o.Customer!.Name} ({o.Subtotal:C})"
            })
            .ToListAsync();
        ViewBag.OpenOrders = openOrders;
    }

    // GET: /DeliveryRuns
    public async Task<IActionResult> Index()
    {
        var list = await _runs.ListRunsAsync(HttpContext.RequestAborted);
        return View(list);
    }

    // GET: /DeliveryRuns/Create
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new CreateRunDto { RunDate = DateOnly.FromDateTime(DateTime.Now) });
    }

    // POST: /DeliveryRuns/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRunDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(dto);
        }
        try
        {
            var run = await _runs.CreateRunAsync(dto.RunDate, dto.DriverName, dto.Vehicle, dto.OrderIds, HttpContext.RequestAborted);
            return RedirectToAction(nameof(Details), new { id = run.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating run");
            ModelState.AddModelError(string.Empty, "Unable to create run.");
            await LoadLookupsAsync();
            return View(dto);
        }
    }

    // GET: /DeliveryRuns/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var run = await _runs.GetRunAsync(id, HttpContext.RequestAborted);
        if (run == null) return NotFound();
        return View(run);
    }

    // POST: /DeliveryRuns/MarkDelivered
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int runId, int orderId)
    {
        var ok = await _runs.MarkDeliveredAsync(runId, orderId, DateTimeOffset.UtcNow, null, null, HttpContext.RequestAborted);
        if (!ok) TempData["Error"] = "Unable to mark delivered.";
        return RedirectToAction(nameof(Details), new { id = runId });
    }

    // POST: /DeliveryRuns/AddOrder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOrder(int runId, int orderId)
    {
        var ok = await _runs.AddOrderAsync(runId, orderId, HttpContext.RequestAborted);
        if (!ok) TempData["Error"] = "Unable to add order to run. Ensure it is Confirmed and not already in a run.";
        return RedirectToAction(nameof(Details), new { id = runId });
    }

    // POST: /DeliveryRuns/UploadPod
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPod(int orderId, IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "No file selected.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            var path = await _storage.SavePodAsync(orderId, file, HttpContext.RequestAborted);
            TempData["Message"] = $"POD uploaded for order {orderId}.";
            _logger.LogInformation("Saved POD at {Path}", path);
        }
        catch (InvalidDataException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload POD");
            TempData["Error"] = "Failed to upload POD.";
        }
        return RedirectToAction(nameof(Index));
    }
}
