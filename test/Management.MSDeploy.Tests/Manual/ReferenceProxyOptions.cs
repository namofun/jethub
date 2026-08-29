using System;

namespace Xylab.Management.WebDeploy.UnitTests.Manual;

internal sealed class ReferenceProxyOptions
{
    public required Uri Endpoint { get; init; }

    public required string Username { get; init; }

    public required string Password { get; init; }

    public required string CaptureDirectory { get; init; }

    public int MaximumCaptureBytes { get; init; } = 16 * 1024 * 1024;
}
