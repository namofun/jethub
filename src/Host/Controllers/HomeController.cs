﻿using JetHub.Models;
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
            var judgehostVersion = await systemInfo.GetJudgehostVersionInfoAsync();

            return View(new IndexModel
            {
                LoadAverage = await systemInfo.GetLoadavgAsync(),
                Uptime = await systemInfo.GetUptimeAsync(),
                Cmdline = await systemInfo.GetCmdlineAsync(),
                Kernel = await systemInfo.GetVersionAsync(),
                Judgehosts = await systemInfo.GetRunningServicesAsync(),
                Processors = await systemInfo.GetProcessorsAsync(),
                HardDriveStatistics = await systemInfo.GetHardDriveStatisticsAsync(),
                MemoryStatistics = await systemInfo.GetMemoryStatisticsAsync(),
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


        [Route("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
