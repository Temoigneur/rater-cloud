// Add this to a new file: Controllers/FallbackController.cs
using Microsoft.AspNetCore.Mvc;

namespace Rater.Controllers
{
    public class FallbackController : Controller
    {
        [Route("/")]
        [Route("/{*url}")]
        public IActionResult Index()
        {
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"), "text/html");
        }
    }
}
