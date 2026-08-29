namespace Xylab.Management.WebDeploy;

using System.ComponentModel.DataAnnotations;

public sealed class WebDeployOptions
{
    [Range(1024 * 1024, 256 * 1024 * 1024)]
    public int MaximumRequestBytes { get; set; } = 70 * 1024 * 1024;

    [Range(1, 16)]
    public int MaximumConcurrentSyncRequests { get; set; } = 2;
}
