namespace Xylab.Management;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Xylab.Management.LogStream;

public static class LogStreamWebApplicationBuilderExtensions
{
    public static OptionsBuilder<LogStreamHubOptions> AddLogStreamWebSocket(this IServiceCollection services)
    {
        services.AddSignalR();
        return services.AddOptions<LogStreamHubOptions>();
    }

    public static OptionsBuilder<LogStreamHubOptions> WithFileNameSelector(
        this OptionsBuilder<LogStreamHubOptions> builder,
        Func<IQueryCollection, string?> selector)
    {
        builder.Configure(options => options.FileNameSelector = selector);
        return builder;
    }

    public static HubEndpointConventionBuilder MapLogStreamWebSocket(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePattern,
        Action<HttpConnectionDispatcherOptions>? configureOptions = null)
    {
        return builder.MapHub<LogStreamHub>(routePattern, configureOptions);
    }
}
