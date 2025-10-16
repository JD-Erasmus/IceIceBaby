using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Clerk,Manager")]
public class PaymentsController : Controller
{
    // GET: /Payments
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Payments/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Payments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(object form)
    {
        // TODO: Record payment and update order paid state
        TempData["Message"] = "Payment recorded (stub).";
        return RedirectToAction(nameof(Index));
    }
}
