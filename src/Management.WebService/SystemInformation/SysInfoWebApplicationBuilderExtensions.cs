namespace Xylab.Management;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xylab.Management.Services;
using Xylab.Management.SystemInformation;

public static class SysInfoWebApplicationBuilderExtensions
{
    public static IServiceCollection AddHostSystemApi(this IServiceCollection services)
    {
        services.TryAddSingleton<IHostSystem>(IHostSystem.CreateDefault());
        return services;
    }

    public static RouteHandlerBuilder MapSystemInformation(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePatternPrefix)
    {
        IHostSystem hostSystem = builder.ServiceProvider.GetRequiredService<IHostSystem>();
        SysInfoEndpoint endpoint = new(hostSystem);

        RouteGroupBuilder group = builder.MapGroup(routePatternPrefix);
        List<RouteHandlerBuilder> routes =
        [
            group.MapGet("status", endpoint.Status),
            group.MapGet("cpu", endpoint.Cpu),
            group.MapGet("kernel", endpoint.Kernel),
            group.MapGet("disks", endpoint.Disks),
            group.MapGet("processes", endpoint.Processes),
            group.MapGet("services", endpoint.Services),
        ];

        if (OperatingSystem.IsLinux())
        {
            routes.Add(group.MapGet("packages", endpoint.Packages));
        }

        return new RouteHandlerBuilder(routes);
    }
}
