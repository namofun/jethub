namespace Xylab.Management.LogStream;

using System;

public class LogStreamOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromHours(1);

    required public string Path { get; init; }

    public string VfsApiBase { get; init; } = "/api/vfs/";
}
