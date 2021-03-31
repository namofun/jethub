using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface ISystemInfo
    {
        Task<string> GetLoadavgAsync();

        Task<TimeSpan> GetUptimeAsync();

        Task<List<string>> GetRunningServicesAsync();

        Task<(string CommitId, string Branch)> GetJudgehostVersionInfoAsync();

        Task<string> RunAsync(string fileName, string cmdline, int timeout, bool massiveOutput = false);
    }

    public class FakeSystemInfo : ISystemInfo
    {
        public Task<string> GetLoadavgAsync()
        {
            return Task.FromResult("0.00, 0.00, 0.00");
        }

        public Task<TimeSpan> GetUptimeAsync()
        {
            return Task.FromResult(TimeSpan.FromSeconds(621260));
        }

        public Task<List<string>> GetRunningServicesAsync()
        {
            return Task.FromResult(new List<string>
            {
                "domjudge-judgehost@0",
                "domjudge-judgehost@1",
                "domjudge-judgehost@2",
                "domjudge-judgehost@3",
            });
        }

        public Task<(string, string)> GetJudgehostVersionInfoAsync()
        {
            return Task.FromResult(("abcdefg", "master"));
        }

        public Task<string> RunAsync(string fileName, string cmdline, int timeout, bool massiveOutput)
        {
            throw new NotSupportedException();
        }
    }

    public class ProcfsSystemInfo : ISystemInfo
    {
        private readonly ILogger<ProcfsSystemInfo> _logger;

        public ProcfsSystemInfo(ILogger<ProcfsSystemInfo> logger)
        {
            _logger = logger;
        }

        public async Task<(string, string)> GetJudgehostVersionInfoAsync()
        {
            const string VersionFile = "/opt/domjudge/judgehost/.version";
            if (File.Exists(VersionFile))
            {
                var all = await File.ReadAllTextAsync(VersionFile);
                var vers = all.Trim().Split(new[] { ' ' }, 2);
                if (vers.Length == 2) return (vers[0], vers[1]);
            }
            
            return ("unknown", "unknown");
        }

        public async Task<string> GetLoadavgAsync()
        {
            var loadavg = await File.ReadAllTextAsync("/proc/loadavg");
            return string.Join(", ", loadavg.Trim().Split(' ').Take(3));
        }

        public Task<List<string>> GetRunningServicesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<TimeSpan> GetUptimeAsync()
        {
            var uptime = await File.ReadAllTextAsync("/proc/uptime");
            return TimeSpan.FromSeconds(Math.Floor(double.Parse(uptime.Trim().Split(' ')[0])));
        }

        public Task<string> RunAsync(string fileName, string cmdline, int timeout, bool massiveOutput)
        {
            var tcs = new TaskCompletionSource<string>();

            Task.Run(() =>
            {
                _logger.LogInformation(
                    "Starting {args}...",
                    fileName + " " + cmdline);

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                using var proc = Process.Start(
                    new ProcessStartInfo(fileName, cmdline)
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Environment = { ["LANG"] = "en-US" },
                    });

                if (proc == null)
                {
                    _logger.LogError(
                        "Process start failed with \"{args}\"...",
                        fileName + " " + cmdline);
                    tcs.SetException(new NotSupportedException("Process start failed."));
                    return;
                }

                StringBuilder stdout = null, stderr = null;
                if (massiveOutput)
                {
                    stdout = new StringBuilder();
                    stderr = new StringBuilder();
                    proc.OutputDataReceived += (sender, args) => stdout.AppendLine(args.Data);
                    proc.ErrorDataReceived += (sender, args) => stderr.AppendLine(args.Data);
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }

                proc.StandardInput.Close();
                if (!proc.WaitForExit(timeout))
                {
                    proc.Kill(true);
                    _logger.LogError(
                        "Process \"{args}\" running out of time, killing.",
                        fileName + " " + cmdline);
                    tcs.SetException(new TimeoutException());
                }
                else
                {
                    stopWatch.Stop();
                    tcs.SetResult(massiveOutput ? stdout.ToString().Trim() : proc.StandardOutput.ReadToEnd().Trim());

                    _logger.LogInformation(
                        "Process \"{args}\" finished in {elapsed}ms.",
                        fileName + " " + cmdline,
                        stopWatch.ElapsedMilliseconds);

                    var stderrFinal = massiveOutput ? stderr.ToString().Trim() : proc.StandardOutput.ReadToEnd().Trim();
                    if ( !string.IsNullOrWhiteSpace(stderrFinal))
                    {
                        _logger.LogInformation(
                            "Process \"{args}\" with stderr:\r\n{stderr}",
                            fileName + " " + cmdline,
                            stderrFinal);
                    }
                }
            });

            return tcs.Task;
        }
    }
}
