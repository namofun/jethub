using JetHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemInformationAsync();

        Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/");
    }

    public class LinuxSystem : IHostSystem
    {
        public static List<InstalledPackage> ParseDpkgStatus(string[] contents)
        {
            const string PackagePrefix = "Package: ";
            const string ArchitecturePrefix = "Architecture: ";
            const string VersionPrefix = "Version: ";

            List<InstalledPackage> installedPackages = new();
            InstalledPackage package = null;
            foreach (string line in contents)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (package?.Name != null)
                    {
                        installedPackages.Add(package);
                        package = null;
                    }

                    continue;
                }

                package ??= new InstalledPackage();
                if (line.StartsWith(PackagePrefix))
                {
                    package.Name = line[PackagePrefix.Length..];
                }
                else if (line.StartsWith(ArchitecturePrefix))
                {
                    package.Architect = line[ArchitecturePrefix.Length..];
                }
                else if (line.StartsWith(VersionPrefix))
                {
                    package.Version = line[VersionPrefix.Length..];
                }
            }

            return installedPackages;
        }

        public async Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/")
        {
            root += (root.EndsWith("/") ? "" : "/") + "var/lib/dpkg/status";
            return ParseDpkgStatus(await File.ReadAllLinesAsync(root));
        }

        public async Task<SystemInformation> GetSystemInformationAsync()
        {
            Interop.Libc.sysinfo(out Interop.Libc.sysinfo_t sysinfo);
            string[] memInfo = await File.ReadAllLinesAsync("/proc/meminfo");
            ulong freeRam2 = 0;
            foreach (string line in memInfo)
            {
                if (line.StartsWith("MemFree:") || line.StartsWith("Buffers:") || line.StartsWith("Cached:"))
                {
                    freeRam2 += ulong.Parse(line.Split(" ", StringSplitOptions.RemoveEmptyEntries)[1]);
                }
            }

            return new SystemInformation
            {
                UsedSwapBytes = sysinfo.totalswap - sysinfo.freeswap,
                UsedMemoryBytes = sysinfo.totalram - (freeRam2 << 10),
                Uptime = TimeSpan.FromSeconds(sysinfo.uptime),
                TotalSwapBytes = sysinfo.totalswap,
                TotalMemoryBytes = sysinfo.totalram,
                LoadAverages = Interop.Libc.sysinfo_loads_to100(sysinfo.loads),
            };
        }
    }

    public class FakeSystem : IHostSystem
    {
        public Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/")
        {
            return Task.FromResult(new List<InstalledPackage>()
            {
                new InstalledPackage
                {
                    Name = root == "/" ? "binutils" : "coreutils",
                    Version = "2.30-21ubuntu1~18.04.5",
                    Architect = "amd64",
                }
            });
        }

        public Task<SystemInformation> GetSystemInformationAsync()
        {
            return Task.FromResult(new SystemInformation
            {
                UsedMemoryBytes = 336282497,
                TotalMemoryBytes = 1971191808,
                LoadAverages = new[] { 0.13, 0.10, 0.09 },
                Uptime = TimeSpan.FromSeconds(((3 * 24 + 5) * 60 + 16) * 60 + 16),
            });
        }
    }
}
