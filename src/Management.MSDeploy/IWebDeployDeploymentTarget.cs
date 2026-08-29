namespace Xylab.Management.WebDeploy;

using Microsoft.Extensions.DependencyInjection;

public interface IWebDeployDeploymentTarget
{
    Task<WebDeployResult> DeployAsync(
        string destination,
        DeploymentPayload payload,
        bool writeContent,
        bool dryRun,
        CancellationToken cancellationToken);

    public static virtual void ConfigureServices(IServiceCollection services)
    {
    }
}

public sealed record WebDeployResult(
    string SiteName,
    string SiteRoot,
    int FilesWritten,
    int FilesDeleted,
    int DirectoriesCreated,
    long BytesWritten);
