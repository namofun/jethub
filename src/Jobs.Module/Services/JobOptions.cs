﻿using Xylab.Workflows.Legacy.Services;

namespace Xylab.Workflows.Legacy
{
    /// <summary>
    /// The physical storage path options of job files.
    /// </summary>
    public class JobOptions
    {
        /// <summary>
        /// The path of storage directory
        /// </summary>
        public IJobFileProvider Storage { get; set; }
    }
}
