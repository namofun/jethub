using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public sealed class LogPumper : IDisposable
    {
        private long _idx;
        private readonly string _file;
        private readonly string _connectionId;
        private readonly IHubContext<LogHub> _hubContext;
        private readonly CancellationTokenSource _cts;

        public LogPumper(string file, string conn, IHubContext<LogHub> hub)
        {
            _idx = -1;
            _file = file;
            _connectionId = conn;
            _hubContext = hub;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cts.Token);
                    using var stream = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.Read);

                    if (_idx == -1) stream.Seek(Math.Max(-4000, -stream.Length), SeekOrigin.End);
                    else stream.Seek(_idx, SeekOrigin.Begin);
                    var logs = new List<string>();

                    using var reader = new StreamReader(stream);
                    string last = null;
                    while ((last = await reader.ReadLineAsync()) != null) logs.Add(last);
                    _idx = stream.Position;

                    await _hubContext.Clients.Client(_connectionId).SendAsync("ReceiveLog", logs);
                }
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.Client(_connectionId)?
                    .SendAsync("ReceiveLog",
                        new[] { "Log stream exiting, ex: " + ex.Message });

                await _hubContext.Clients.Client(_connectionId)?
                    .SendAsync("Exit");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    public class LogHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            var httpContext = Context.GetHttpContext();
            var services = httpContext.RequestServices;
            string fileName = null;

            if (httpContext.Request.Query.TryGetValue("host", out var hosts) && hosts.Count == 1)
            {
                fileName = "/opt/domjudge/judgehost/log/judge." + hosts[0] + ".log";
                if (!File.Exists(fileName)) fileName = "playground/null.log";
            }

            if (fileName != null)
            {
                var pumper = new LogPumper(
                    fileName,
                    Context.ConnectionId,
                    services.GetRequiredService<IHubContext<LogHub>>());

                Context.Items[typeof(LogPumper)] = pumper;
                _ = pumper.StartAsync();
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveLog", new[] { "No such judgehost found. Exiting..." });
                await Clients.Caller.SendAsync("Exit");
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ((LogPumper)Context.Items[typeof(LogPumper)])?.Dispose();
            return base.OnDisconnectedAsync(exception);
        }
    }
}
