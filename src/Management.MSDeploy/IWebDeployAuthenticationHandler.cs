namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public interface IWebDeployAuthenticationHandler
{
    ValueTask<bool> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken);

    ValueTask ChallengeAsync(HttpResponse response, CancellationToken cancellationToken);

    public static virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
