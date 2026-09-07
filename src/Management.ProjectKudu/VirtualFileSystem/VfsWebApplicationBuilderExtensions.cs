namespace Xylab.Management.VirtualFileSystem;

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class VfsWebApplicationBuilderExtensions
{
    public static IServiceCollection AddVirtualFileSystem(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystemV2, FileSystemV2>();
        services.AddHttpContextAccessor();
        return services;
    }

    public static RouteHandlerBuilder MapVirtualFileSystem(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePrefix,
        string fileSystemRoot)
    {
        IFileSystemV2 fileSystem = builder.ServiceProvider.GetRequiredService<IFileSystemV2>();
        ILogger<VfsEndpoint> logger = builder.ServiceProvider.GetRequiredService<ILogger<VfsEndpoint>>();
        IHttpContextAccessor accessor = builder.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        VfsEndpoint endpoint = new(logger, fileSystemRoot, fileSystem, accessor);

        RouteGroupBuilder group = builder.MapGroup(routePrefix);
        return new RouteHandlerBuilder(
        [
            group.MapMethods("{**path}", [HttpMethods.Get, HttpMethods.Head], endpoint.GetItem),
            group.MapPut("{**path}", endpoint.PutItem),
            group.MapDelete("{**path}", endpoint.DeleteItem),
        ]);
    }
}
