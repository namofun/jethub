using JetHub.Models;
using JetHub.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JetHub.Controllers
{
    [Route("/api/[action]")]
    public class ApiController : ControllerBase
    {
        [HttpGet]
        public Task<SystemInformation> Sysinfo(
            [FromServices] IHostSystem hostSystem)
        {
            return hostSystem.GetSystemInformationAsync();
        }

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
