namespace Xylab.Management.LogStream;

using Microsoft.AspNetCore.SignalR;

public sealed class LogStreamHubPumper(string file, string connectionId, IHubContext<LogStreamHub> hubContext) : IDisposable
{
    private long idx = -1;
    private readonly CancellationTokenSource cts = new();

    public async Task StartAsync()
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(1000, cts.Token);
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (idx == -1)
                {
                    stream.Seek(Math.Max(-4000, -stream.Length), SeekOrigin.End);
                }
                else
                {
                    stream.Seek(idx, SeekOrigin.Begin);
                }

                List<string> logs = new();

                using StreamReader reader = new(stream);
                string? last;
                while ((last = await reader.ReadLineAsync()) != null)
                {
                    logs.Add(last);
                }

                idx = stream.Position;
                if (logs.Count > 0)
                {
                    await hubContext.Clients.Client(connectionId).SendAsync("ReceiveLog", logs);
                }
            }
        }
        catch (Exception ex)
        {
            await hubContext.Clients.Client(connectionId).SendAsync("ReceiveLog", new[] { "Log stream exiting, ex: " + ex.Message });
            await hubContext.Clients.Client(connectionId).SendAsync("Exit");
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
