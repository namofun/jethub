namespace Xylab.Remoting.PowerShellWebService;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class PswsAppBuilderExtensions
{
    public static OptionsBuilder<PowerShellOptions> AddPowerShellWebService(this IServiceCollection services)
    {
        services.AddSingleton<PowerShellAuthorizationManager>();
        services.AddSingleton<PowerShellRunspaceFactory>();

        return services.AddOptions<PowerShellOptions>();
    }

    public static OptionsBuilder<PowerShellOptions> WithWebSocketSupport(this OptionsBuilder<PowerShellOptions> builder)
    {
        builder.Services.AddSignalR();
        return builder;
    }

    public static OptionsBuilder<PowerShellOptions> ImportAssembly(this OptionsBuilder<PowerShellOptions> builder, params Assembly[] assemblies)
    {
        builder.Configure(options => options.AssembliesToImport.AddRange(assemblies));
        return builder;
    }

    public static OptionsBuilder<PowerShellOptions> ImportModule(this OptionsBuilder<PowerShellOptions> builder, params string[] modules)
    {
        builder.Configure(options => options.ModulesToImport.AddRange(modules));
        return builder;
    }

    public static HubEndpointConventionBuilder MapPowerShellWebSocket(this IEndpointRouteBuilder builder, string pattern, Action<HttpConnectionDispatcherOptions>? configureOptions = null)
    {
        return builder.MapHub<PowerShellHub>(pattern, configureOptions);
    }
}
