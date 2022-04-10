using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xylab.Workflows.Legacy.Entities;
using Xylab.Workflows.Legacy.Services;

namespace Xylab.Workflows.Legacy.Activities
{
    public class FallbackUnknown : IJobExecutor
    {
        public Task<JobStatus> ExecuteAsync(string arguments, Job entry, ILogger logger)
        {
            logger.LogError("Unknown job type.");
            return Task.FromResult(JobStatus.Unknown);
        }
    }
}
