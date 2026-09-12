namespace Xylab.Management;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xylab.Management.ServiceVersioning;

public static class ServiceVersionWebApplicationBuilderExtensions
{
    public static IServiceCollection ConfigureServiceVersion(this IServiceCollection services, Assembly startupAssembly)
    {
        return services.Configure<ServiceVersionOptions>(options =>
        {
            options.HostName = System.Net.Dns.GetHostName();
            options.ServiceName = startupAssembly.GetName().Name!;
            options.Branch = startupAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GitBranchName")?.Value ?? "unknown";
            options.CommitId = startupAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GitCommitId")?.Value ?? "unknown";
            options.Version = startupAssembly.GetName().Version?.ToString() ?? "0.0.0.0";
        });
    }

    public static RouteHandlerBuilder MapServiceVersion(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePattern)
    {
        return builder.MapGet(routePattern, (IOptions<ServiceVersionOptions> options) =>
        {
            return new
            {
                startup = options.Value.ServiceName,
                hostName = options.Value.HostName,
                commitId = options.Value.CommitId,
                branch = options.Value.Branch,
                version = options.Value.Version,
            };
        });
    }
}
