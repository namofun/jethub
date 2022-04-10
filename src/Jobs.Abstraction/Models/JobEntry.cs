using System.Collections.Generic;
using Xylab.Workflows.Legacy.Entities;

namespace Xylab.Workflows.Legacy.Models
{
    /// <summary>
    /// The model class representing entries of job.
    /// </summary>
    public class JobEntry : Job
    {
        /// <summary>
        /// The children collection
        /// </summary>
        public ICollection<JobEntry>? Children { get; set; }
    }
}
