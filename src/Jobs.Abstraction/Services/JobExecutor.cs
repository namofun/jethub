using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xylab.Workflows.Legacy.Entities;

namespace Xylab.Workflows.Legacy.Services
{
    /// <summary>
    /// The executor of job.
    /// </summary>
    public interface IJobExecutor
    {
        /// <summary>
        /// Asynchronously execute the targeted job with argument.
        /// </summary>
        /// <param name="arguments">The job arguments.</param>
        /// <param name="entry">The job entry.</param>
        /// <param name="logger">The job logger.</param>
        /// <returns>The job status.</returns>
        Task<JobStatus> ExecuteAsync(string arguments, Job entry, ILogger logger);
    }
}
