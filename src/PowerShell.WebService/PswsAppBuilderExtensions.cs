namespace Xylab.Remoting.PowerShellWebService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public static class PswsAppBuilderExtensions
{
    public static IServiceCollection AddPowerShellWebService(
        this IServiceCollection services,
        Action<PowerShellOptions>? configure = null)
    {
        services.AddOptions<PowerShellOptions>();
        services.AddSingleton<PowerShellAuthorizationManager>();
        services.AddSingleton<PowerShellRunspaceFactory>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        return services;
    }

    public static HubEndpointConventionBuilder MapPowerShellWebSocket(
        this IEndpointRouteBuilder builder,
        string pattern,
        Action<HttpConnectionDispatcherOptions>? configureOptions = null)
    {
        return builder.MapHub<PowerShellHub>(pattern, configureOptions);
    }
}
