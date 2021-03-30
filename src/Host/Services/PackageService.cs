using JetHub.Models;
using System;
using System.Collections.Generic;
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

    public class AptPackageService : IPackageService
    {
        private readonly ISystemInfo _systemInfo;

        public AptPackageService(ISystemInfo systemInfo)
        {
            _systemInfo = systemInfo;
        }

        public IReadOnlyList<InstalledPackage> Last { get; internal set; }

        public IReadOnlyList<InstalledPackage> LastInChroot { get; internal set; }

        public DateTimeOffset? LastCache { get; internal set; }

        private async Task<List<InstalledPackage>> GetInstalledPackagesAsyncCore(string a, string b)
        {
            var results = await _systemInfo.RunAsync(a, b, 5000);
            var lines = results.Split('\n');
            var ans = new List<InstalledPackage>();
            foreach (var line in lines)
            {
                // "acpid/bionic,now 1:2.0.28-1ubuntu1 amd64 [installed]"
                var items = line.Trim().Split(' ');
                if (items.Length != 4) continue;
                var a = items[0].Split(new[] { '/' }, 2);
                if (a.Length != 4) continue;

                ans.Add(new InstalledPackage
                {
                    Name = a[0],
                    Attach = a[1],
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
    }
}
