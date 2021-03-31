using JetHub.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IPackageService
    {
        IReadOnlyList<InstalledPackage> Last { get; }

        IReadOnlyList<InstalledPackage> LastInChroot { get; }

        DateTimeOffset? LastCache { get; }

        Task<List<InstalledPackage>> GetInstalledPackagesAsync();

        Task<List<InstalledPackage>> GetInstalledPackagesInChrootAsync();
    }

    public class FakePackageService : IPackageService
    {
        public IReadOnlyList<InstalledPackage> Last { get; }
            = new List<InstalledPackage>
            {
                new InstalledPackage
                {
                    Name = "binutils",
                    Attach = "bionic-security,bionic-updates,now",
                    Version = "2.30-21ubuntu1~18.04.5",
                    Architect = "amd64",
                    Status = new List<string> { "installed", "automatic" },
                }
            };

        public IReadOnlyList<InstalledPackage> LastInChroot => Last;

        public DateTimeOffset? LastCache => DateTimeOffset.Now.Date;

        public Task<List<InstalledPackage>> GetInstalledPackagesAsync()
        {
            return Task.FromResult((List<InstalledPackage>)Last);
        }

        public Task<List<InstalledPackage>> GetInstalledPackagesInChrootAsync()
        {
            return GetInstalledPackagesAsync();
        }
    }

    public class AptPackageService : BackgroundService, IPackageService
    {
        private readonly ISystemInfo _systemInfo;
        private readonly ILogger<AptPackageService> _logger;

        public AptPackageService(ISystemInfo systemInfo, ILogger<AptPackageService> logger)
        {
            _systemInfo = systemInfo;
            Last = Array.Empty<InstalledPackage>();
            LastInChroot = Array.Empty<InstalledPackage>();
            _logger = logger;
        }

        public IReadOnlyList<InstalledPackage> Last { get; internal set; }

        public IReadOnlyList<InstalledPackage> LastInChroot { get; internal set; }

        public DateTimeOffset? LastCache { get; internal set; }

        private async Task<List<InstalledPackage>> GetInstalledPackagesAsyncCore(string a, string b)
        {
            var results = await _systemInfo.RunAsync(a, b, 10000);
            var lines = results.Split('\n');
            var ans = new List<InstalledPackage>();
            foreach (var line in lines)
            {
                // "acpid/bionic,now 1:2.0.28-1ubuntu1 amd64 [installed]"
                var items = line.Trim().Split(' ');
                if (items.Length != 4) continue;
                var pkg = items[0].Split(new[] { '/' }, 2);
                if (pkg.Length != 4) continue;

                ans.Add(new InstalledPackage
                {
                    Name = pkg[0],
                    Attach = pkg[1],
                    Version = items[1],
                    Architect = items[2],
                    Status = items[3].TrimStart('[').TrimEnd(']').Split(',')
                });
            }

            return ans;
        }

        public Task<List<InstalledPackage>> GetInstalledPackagesAsync()
        {
            return GetInstalledPackagesAsyncCore("apt", "list --installed");
        }

        public Task<List<InstalledPackage>> GetInstalledPackagesInChrootAsync()
        {
            return GetInstalledPackagesAsyncCore("chroot", "/chroot/domjudge/ apt list --installed");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;

                try
                {
                    Last = await GetInstalledPackagesAsync();
                    LastInChroot = await GetInstalledPackagesInChrootAsync();
                    LastCache = DateTimeOffset.Now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during read apt...");
                    Last ??= Array.Empty<InstalledPackage>();
                    LastInChroot ??= Array.Empty<InstalledPackage>();
                    LastCache = null;
                }

                var nextExecuteTime = now - now.TimeOfDay + TimeSpan.FromHours(3);
                if (nextExecuteTime < now) nextExecuteTime = nextExecuteTime.AddDays(1);
                var executeSpan = nextExecuteTime - DateTimeOffset.Now;

                await Task.Delay(executeSpan, stoppingToken);
            }
        }
    }
}
