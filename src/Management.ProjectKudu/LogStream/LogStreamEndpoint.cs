namespace Xylab.Management.LogStream;

using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class LogStreamEndpoint(LogStreamOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        IFileSystemV2 fileSystem = context.RequestServices.GetService<IFileSystemV2>() ?? new FileSystemV2();
        ILogger logger = context.RequestServices.GetRequiredService<ILogger<LogStreamEndpoint>>();
        using LogStreamManager manager = new(fileSystem, logger, options);
        await manager.ProcessRequest(context);
    }
}
