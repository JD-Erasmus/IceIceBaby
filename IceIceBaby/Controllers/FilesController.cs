using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceIceBaby.Controllers;

[Authorize(Roles = "Driver,Manager,Clerk")]
public class FilesController : Controller
{
    // GET: /Files/Download?path=...
    [HttpGet]
    public IActionResult Download(string path)
    {
        // TODO: Secure and validate path, stream file
        if (string.IsNullOrWhiteSpace(path)) return NotFound();
        return NotFound(); // Placeholder until storage service is implemented
    }
}
