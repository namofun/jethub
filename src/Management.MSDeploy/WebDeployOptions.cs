namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Xylab.Management.WebDeploy.Authentication;

public sealed class WebDeployOptions
{
    [Range(1024 * 1024, 256 * 1024 * 1024)]
    public int MaximumRequestBytes { get; set; } = 70 * 1024 * 1024;

    [Range(1, 16)]
    public int MaximumConcurrentSyncRequests { get; set; } = 2;

    public Func<HttpRequest, CancellationToken, ValueTask<bool>> AuthenticateAsync { get; set; } = (_, _) => ValueTask.FromResult(true);

    public Func<HttpResponse, CancellationToken, ValueTask> ChallengeAsync { get; set; } = (_, _) => ValueTask.CompletedTask;
}
