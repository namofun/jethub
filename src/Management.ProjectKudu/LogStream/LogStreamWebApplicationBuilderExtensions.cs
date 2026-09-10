namespace Xylab.Management.LogStream;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class LogStreamWebApplicationBuilderExtensions
{
    public static IEndpointConventionBuilder MapLogStream(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePattern,
        LogStreamOptions options)
    {
        LogStreamEndpoint endpoint = new(options);
        return builder.MapGet(routePattern, endpoint.InvokeAsync);
    }
}
