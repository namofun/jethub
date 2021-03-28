using Microsoft.AspNetCore.Mvc;

namespace JetHub.Controllers
{
    [Route("/api/[action]")]
    public class ApiController : ControllerBase
    {
        [HttpGet]
        public IActionResult Test()
        {
            return Ok(new[]
            {
                new
                {
                    hello = "world",
                    href = Url.Action(),
                    arr = new[]
                    {
                        new { a = 1, b = 2 },
                        new { a = 2, b = 3 },
                    }
                },
                new
                {
                    hello = "world",
                    href = Url.Action(),
                    arr = new[]
                    {
                        new { a = 1, b = 2 },
                        new { a = 2, b = 3 },
                    }
                }
            });
        }
    }
}
