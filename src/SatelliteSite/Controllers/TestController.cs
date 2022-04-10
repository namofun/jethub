using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Workflows.Legacy.Activities;
using Xylab.Workflows.Legacy.Models;
using Xylab.Workflows.Legacy.Services;

namespace SatelliteSite.Controllers
{
    [Route("[controller]/[action]")]
    public class TestController : ViewControllerBase
    {
        private static object Forecast()
        {
            return new
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.Now,
            };
        }


        [HttpGet]
        public async Task<IActionResult> CreateJob(
            [FromServices] IJobScheduler scheduler)
        {
            var j = await scheduler.ScheduleAsync(new JobDescription
            {
                Arguments = Forecast().ToJson(),
                JobType = "Sample.PingPong",
                SuggestedFileName = "ping-pong.json",
                OwnerId = int.Parse(User.GetUserId()),
            });

            return RedirectToAction("Detail", "Jobs", new { area = "Dashboard", id = j.JobId });
        }


        [HttpGet]
        public async Task<IActionResult> CreateJob2(
            [FromServices] IJobScheduler scheduler)
        {
            var children = new List<JobDescription>();
            var owner = int.Parse(User.GetUserId());

            for (int i = 0; i < 10; i++)
            {
                children.Add(new JobDescription
                {
                    Arguments = Forecast().ToJson(),
                    JobType = "Sample.PingPong",
                    SuggestedFileName = "day" + i + ".json",
                    OwnerId = int.Parse(User.GetUserId()),
                });
            }

            var total = ComposeArchive.ForChildren(owner, "day0-9.zip", children);
            var j = await scheduler.ScheduleAsync(total);

            return RedirectToAction("Detail", "Jobs", new { area = "Dashboard", id = j.JobId });
        }
    }
}
