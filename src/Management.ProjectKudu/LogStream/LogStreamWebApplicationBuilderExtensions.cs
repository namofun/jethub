namespace Xylab.Management;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xylab.Management.LogStream;

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
