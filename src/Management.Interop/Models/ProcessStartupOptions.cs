#nullable enable
namespace Xylab.Management.Models;

using System;
using System.Collections.Generic;

public class ProcessStartupOptions
{
    public bool UseMassiveOutput { get; set; }

    public bool WriteStandardErrorToLogger { get; set; }

    public TimeSpan? Timeout { get; set; }

    public IDictionary<string, string>? EnvironmentVariable { get; set; }
}
