#nullable enable
using System;
using System.Collections.Generic;

namespace Xylab.Management.Models
{
    public class ProcessStartupOptions
    {
        public bool UseMassiveOutput { get; set; }

        public bool WriteStandardErrorToLogger { get; set; }

        public TimeSpan? Timeout { get; set; }

        public IDictionary<string, string>? EnvironmentVariable { get; set; }
    }
}
