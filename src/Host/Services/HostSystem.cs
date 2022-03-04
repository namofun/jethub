using JetHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemInformationAsync();

        Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/");

        Task<List<CpuInformation>> GetCpuInformationAsync();

        Task<KernelInformation> GetKernelInformationAsync();

        Task<List<DriveInformation>> GetDriveInformationAsync(bool fixedOnly = true);
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

        public async Task<List<CpuInformation>> GetCpuInformationAsync()
        {
            List<CpuInformation> cpus = new();
            string cpuinfo = await File.ReadAllTextAsync("/proc/cpuinfo");
            string[] processors = cpuinfo.Trim().Split("\n\n");
            foreach (string processor in processors)
            {
                Dictionary<string, string> properties =
                    processor.Trim()
                        .Split('\n')
                        .Select(a => a.Split(new[] { ':' }, 2))
                        .ToDictionary(k => k[0].Trim(), v => v[1].Trim());

                cpus.Add(new CpuInformation()
                {
                    ModelName = properties["model name"],
                    PhysicalId = int.Parse(properties["physical id"]),
                    CoreId = int.Parse(properties["core id"]),
                    ProcessorId = int.Parse(properties["processor"]),
                    CacheSize = properties["cache size"],
                    ClockSpeed = properties["cpu MHz"] + " MHz",
                });
            }

            return cpus;
        }

        public Task<List<DriveInformation>> GetDriveInformationAsync(bool fixedOnly = true)
        {
            List<DriveInformation> drives = new();

            IntPtr mtab = Interop.Libc.setmntent("/etc/mtab", "r");
            if (mtab == IntPtr.Zero)
            {
                throw new AccessViolationException("Failed to open /etc/mtab");
            }

            try
            {
                while (Interop.Libc.getmntent(mtab) is Interop.Libc.mntent_t mnt)
                {
                    DriveType type = Interop.Libc.GetDriveType(mnt.mnt_type);
                    if (!fixedOnly || type == DriveType.Fixed)
                    {
                        Interop.Libc.statvfs(mnt.mnt_dir, out Interop.Libc.statvfs_t vfs);
                        drives.Add(new DriveInformation()
                        {
                            MountPoint = mnt.mnt_dir,
                            FileSystem = mnt.mnt_fsname,
                            Type = mnt.mnt_type,
                            Category = type,
                            TotalSizeBytes = vfs.f_bsize * vfs.f_blocks,
                            UsedSizeBytes = (vfs.f_blocks - vfs.f_bavail) * vfs.f_bsize,
                        });
                    }
                }
            }
            finally
            {
                Interop.Libc.endmntent(mtab);
            }

            drives.RemoveAll(d => d.TotalSizeBytes == 0);
            return Task.FromResult(drives);
        }

        public async Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/")
        {
            root += (root.EndsWith("/") ? "" : "/") + "var/lib/dpkg/status";
            if (!File.Exists(root))
            {
                return new List<InstalledPackage>();
            }

            return ParseDpkgStatus(await File.ReadAllLinesAsync(root));
        }

        public async Task<KernelInformation> GetKernelInformationAsync()
        {
            return new KernelInformation()
            {
                Cmdline = (await File.ReadAllTextAsync("/proc/cmdline")).Trim(),
                Version = (await File.ReadAllTextAsync("/proc/version")).Trim(),
            };
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
        public Task<List<CpuInformation>> GetCpuInformationAsync()
        {
            return Task.FromResult(new List<CpuInformation>()
            {
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 0, CoreId = 0, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 1, CoreId = 0, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 2, CoreId = 1, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 3, CoreId = 1, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
            });
        }

        public Task<List<DriveInformation>> GetDriveInformationAsync(bool fixedOnly = true)
        {
            return Task.FromResult(new List<DriveInformation>()
            {
                new DriveInformation()
                {
                    Type = "ext4",
                    Category = DriveType.Fixed,
                    FileSystem = "/dev/vda1",
                    MountPoint = "/",
                    TotalSizeBytes = (ulong)(19.9 * 1024 * 1024 * 1024),
                    UsedSizeBytes = (ulong)(6.24 * 1024 * 1024 * 1024),
                },
                new DriveInformation()
                {
                    Type = "fat",
                    Category = DriveType.Fixed,
                    FileSystem = "/dev/vda2",
                    MountPoint = "/boot/efi",
                    TotalSizeBytes = (ulong)(49.9 * 1024 * 1024),
                    UsedSizeBytes = (ulong)(12.35 * 1024 * 1024),
                },
            });
        }

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

        public Task<KernelInformation> GetKernelInformationAsync()
        {
            return Task.FromResult(new KernelInformation()
            {
                Cmdline =
                    "BOOT_IMAGE=/boot/vmlinuz-4.15.0-112-generic " +
                    "root=UUID=00000000-0000-0000-0000-000000000000 " +
                    "ro " +
                    "vga=792 " +
                    "console=tty0 " +
                    "console=ttyS0,115200n8 " +
                    "net.ifnames=0 " +
                    "noibrs " +
                    "quiet " +
                    "splash " +
                    "vt.handoff=1",

                Version =
                    "Linux version 4.15.0-112-generic " +
                    "(buildd@lcy01-amd64-027) " +
                    "(gcc version 7.5.0 (Ubuntu 7.5.0-3ubuntu1~18.04)) " +
                    "#113-Ubuntu SMP Thu Jul 9 23:41:39 UTC 2020",
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
