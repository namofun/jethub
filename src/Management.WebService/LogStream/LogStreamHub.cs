namespace Xylab.Management.LogStream;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

public class LogStreamHub(IOptions<LogStreamHubOptions> options, IHubContext<LogStreamHub> hubContext) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        HttpContext httpContext = Context.GetHttpContext() ?? throw new InvalidOperationException();
        string? fileName = options.Value.FileNameSelector.Invoke(httpContext.Request.Query);

        if (fileName != null)
        {
            LogStreamHubPumper pumper = new(fileName, Context.ConnectionId, hubContext);
            Context.Items[typeof(LogStreamHubPumper)] = pumper;
            _ = pumper.StartAsync();
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveLog", new[] { "No log file found. Exiting..." });
            await Clients.Caller.SendAsync("Exit");
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ((LogStreamHubPumper?)Context.Items[typeof(LogStreamHubPumper)])?.Dispose();
        return base.OnDisconnectedAsync(exception);
    }
}
