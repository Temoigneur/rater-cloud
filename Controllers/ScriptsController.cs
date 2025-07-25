using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Rater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ScriptsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("app.js")]
        public IActionResult GetAppJs()
        {
            var path = Path.Combine(_env.WebRootPath, "js", "app.js");
            if (!System.IO.File.Exists(path))
            {
                return NotFound($"File not found at {path}");
            }

            // Read the file content directly to ensure we get the latest version
            string content = System.IO.File.ReadAllText(path);

            // Add cache control headers to prevent caching
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            // Return the content directly rather than a file reference
            return Content(content, "application/javascript");
        }
    }
}