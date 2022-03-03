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
        IReadOnlyList<InstalledPackage> Global { get; }

        IReadOnlyList<InstalledPackage> Sandbox { get; }

        DateTimeOffset? LastUpdate { get; }
    }

    public class FakePackageService : IPackageService
    {
        public IReadOnlyList<InstalledPackage> Global { get; }
            = new List<InstalledPackage>
            {
                new InstalledPackage
                {
                    Name = "binutils",
                    Version = "2.30-21ubuntu1~18.04.5",
                    Architect = "amd64",
                }
            };

        public IReadOnlyList<InstalledPackage> Sandbox => Global;

        public DateTimeOffset? LastUpdate => DateTimeOffset.Now.Date;
    }

    public class AptPackageService : BackgroundService, IPackageService
    {
        private readonly IHostSystem _hostSystem;
        private readonly ILogger<AptPackageService> _logger;

        public AptPackageService(IHostSystem hostSystem, ILogger<AptPackageService> logger)
        {
            _hostSystem = hostSystem;
            Global = Array.Empty<InstalledPackage>();
            Sandbox = Array.Empty<InstalledPackage>();
            _logger = logger;
        }

        public IReadOnlyList<InstalledPackage> Global { get; private set; }

        public IReadOnlyList<InstalledPackage> Sandbox { get; private set; }

        public DateTimeOffset? LastUpdate { get; private set; }

        public Task<List<InstalledPackage>> GetInstalledPackagesAsync()
        {
            return _hostSystem.GetInstalledPackagesAsync("/");
        }

        public Task<List<InstalledPackage>> GetInstalledPackagesInChrootAsync()
        {
            return _hostSystem.GetInstalledPackagesAsync("/chroot/domjudge/");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;

                try
                {
                    Global = await GetInstalledPackagesAsync();
                    Sandbox = await GetInstalledPackagesInChrootAsync();
                    LastUpdate = DateTimeOffset.Now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during read dpkg status...");
                    Global ??= Array.Empty<InstalledPackage>();
                    Sandbox ??= Array.Empty<InstalledPackage>();
                    LastUpdate = null;
                }

                var nextExecuteTime = now - now.TimeOfDay + TimeSpan.FromHours(3);
                if (nextExecuteTime < now) nextExecuteTime = nextExecuteTime.AddDays(1);
                var executeSpan = nextExecuteTime - DateTimeOffset.Now;

                await Task.Delay(executeSpan, stoppingToken);
            }
        }
    }
}
