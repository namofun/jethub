using JetHub.Models;
using JetHub.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JetHub.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        [HttpGet("/index")]
        public async Task<IActionResult> Index([FromServices] ISystemInfo systemInfo)
        {
            return View(new IndexModel
            {
                LoadAverage = await systemInfo.GetLoadavgAsync(),
                Uptime = await systemInfo.GetUptimeAsync(),
            });
        }


        [Route("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
