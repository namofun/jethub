namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xylab.Management.WebDeploy.Deployment;

public static class WebDeployExtensions
{
    public static OptionsBuilder<WebDeployOptions> AddWebDeploy<TAuthenticationHandler>(this IServiceCollection services)
        where TAuthenticationHandler : class, IWebDeployAuthenticationHandler
    {
        services.AddSingleton<INginxCommandRunner, NginxCommandRunner>();
        services.AddSingleton<NginxSiteManager>();
        services.AddSingleton<StaticSiteReconciler>();
        services.AddSingleton<TraceSessionCoordinator>();
        services.AddSingleton<MSDeployEndpoint>();
        services.AddSingleton<IWebDeployAuthenticationHandler, TAuthenticationHandler>();

        TAuthenticationHandler.ConfigureServices(services);
        return services.AddOptions<WebDeployOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static RouteHandlerBuilder MapWebDeploy(this IEndpointRouteBuilder builder, string baseUrl = "")
    {
        baseUrl = ('/' + baseUrl.TrimStart('/')).TrimEnd('/');
        var rb1 = builder.MapMethods(baseUrl + "/MsDeployAgentService", ["HEAD", "POST"], HandleRequestAsync);
        var rb2 = builder.MapMethods(baseUrl + "/msdeploy.axd", ["HEAD", "POST"], HandleRequestAsync);
        return new RouteHandlerBuilder([rb1, rb2]);
    }

    private static Task HandleRequestAsync(HttpContext context, MSDeployEndpoint endpoint, CancellationToken cancellationToken)
    {
        return endpoint.HandleAsync(context, cancellationToken);
    }
}
