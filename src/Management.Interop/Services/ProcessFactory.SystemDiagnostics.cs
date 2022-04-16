using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public class SystemDiagnosticsProcessFactory : IProcessFactory
    {
        private readonly ILogger<SystemDiagnosticsProcessFactory> _factoryLogger;
        private readonly ILogger<Process> _processLogger;

        public SystemDiagnosticsProcessFactory(
            ILogger<Process> processLogger,
            ILogger<SystemDiagnosticsProcessFactory> factoryLogger)
        {
            _processLogger = processLogger;
            _factoryLogger = factoryLogger;
        }

        public Task<ProcessResult> StartAsync(
            string fileName,
            string cmdline = null,
            ProcessStartupOptions options = null)
        {
            TaskCompletionSource<ProcessResult> taskCompletionSource = new();
            Task.Run(() => StartCore(taskCompletionSource, fileName, cmdline, options));
            return taskCompletionSource.Task;
        }

        private void StartCore(
            TaskCompletionSource<ProcessResult> taskCompletionSource,
            string fileName,
            string cmdline = null,
            ProcessStartupOptions options = null)
        {
            options ??= new();
            _factoryLogger.LogInformation("Starting {fileName} with cmdline='{cmdline}'...", fileName, cmdline);

            ProcessStartInfo psi = new(fileName, cmdline)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment = { ["LANG"] = "en-US" },
            };

            if (options.EnvironmentVariable != null)
            {
                foreach ((string key, string value) in options.EnvironmentVariable)
                {
                    psi.Environment[key] = value;
                }
            }

            Stopwatch stopWatch = Stopwatch.StartNew();
            using Process process = Process.Start(psi);

            if (process == null)
            {
                _factoryLogger.LogError("Process {fileName} start failed with cmdline='{cmdline}'...", fileName, cmdline);
                taskCompletionSource.SetException(new NotSupportedException("Process start failed."));
                return;
            }

            StringBuilder stdout = null, stderr = null;
            if (options.UseMassiveOutput)
            {
                stdout = new StringBuilder();
                stderr = new StringBuilder();
                process.OutputDataReceived += (sender, args) => stdout.AppendLine(args.Data);
                process.ErrorDataReceived += (sender, args) => stderr.AppendLine(args.Data);
                if (options.WriteStandardErrorToLogger)
                {
                    process.ErrorDataReceived += (sender, args) => _processLogger.LogDebug("[{fileName} stderr] {message}", fileName, args.Data);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            process.StandardInput.Close();
            if (!process.WaitForExit(options.Timeout.HasValue ? (int)options.Timeout.Value.TotalMilliseconds : -1))
            {
                process.Kill(true);
                stopWatch.Stop();
                _factoryLogger.LogError("Process {fileName} with cmdline='{cmdline}' running out of time, killing.", fileName, cmdline);
                taskCompletionSource.SetException(new TimeoutException());
            }
            else
            {
                stopWatch.Stop();
                ProcessResult result = new()
                {
                    FileName = fileName,
                    CommandLine = cmdline,
                    ExitCode = process.ExitCode,
                    StandardOutput = ReadResult(stdout, process.StandardOutput),
                    StandardError = ReadResult(stderr, process.StandardError),
                    ElapsedTimeMilliseconds = stopWatch.ElapsedMilliseconds,
                };

                if (!string.IsNullOrEmpty(result.StandardError))
                {
                    _processLogger.LogDebug("[{fileName} stderr] {message}", fileName, result.StandardError);
                }

                taskCompletionSource.SetResult(result);
                _factoryLogger.LogInformation("Process {fileName} with cmdline='{cmdline}' finished in {elapsed}ms.", fileName, cmdline, stopWatch.ElapsedMilliseconds);
            }
        }

        private static string ReadResult(StringBuilder sb, StreamReader sr)
        {
            if (sb != null) return sb.ToString().Trim();
            else return sr.ReadToEnd().Trim();
        }
    }
}
