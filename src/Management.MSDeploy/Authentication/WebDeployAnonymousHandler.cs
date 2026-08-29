namespace Xylab.Management.WebDeploy.Authentication;

using Microsoft.AspNetCore.Http;

public class WebDeployAnonymousHandler : IWebDeployAuthenticationHandler
{
    public ValueTask<bool> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask ChallengeAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        return ValueTask.CompletedTask;
    }
}
