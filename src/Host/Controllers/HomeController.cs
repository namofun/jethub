﻿using JetHub.Models;
using JetHub.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace JetHub.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        [HttpGet("/index")]
        public async Task<IActionResult> Index([FromServices] ISystemInfo systemInfo)
        {
            var judgehostVersion = await systemInfo.GetJudgehostVersionInfoAsync();

            return View(new IndexModel
            {
                LoadAverage = await systemInfo.GetLoadavgAsync(),
                Uptime = await systemInfo.GetUptimeAsync(),
                JudgehostCommitId = judgehostVersion.CommitId,
                JudgehostBranch = judgehostVersion.Branch,
            });
        }


        [HttpGet("/packages")]
        public IActionResult Packages()
        {
            return View();
        }


        [HttpGet("/jsonviewer")]
        public IActionResult JsonViewer(string view_url)
        {
            if (view_url == null || !view_url.StartsWith("/api")) return BadRequest();
            return View();
        }


        [HttpGet("/secrets")]
        public async Task<IActionResult> Secrets([FromServices] IGlobalInfo info)
        {
            var secrets = await System.IO.File.ReadAllLinesAsync(info.SecretFile);
            return View(
                secrets
                    .Where(s => !s.StartsWith('#') && !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Split(new[] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => new EndpointModel(s[0], s[1], s[2], s[3]))
                    .ToList());
        }


        [Route("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
