using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Manager")]
public class DashboardController : Controller
{
    // GET: /Dashboard
    public IActionResult Index()
    {
        // TODO: Load KPI metrics
        return View();
    }
}
