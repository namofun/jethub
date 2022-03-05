using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public class FakeSystem : IHostSystem
    {
        public Task<List<CpuInformation>> GetCpusAsync()
        {
            return Task.FromResult(new List<CpuInformation>()
            {
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 0, CoreId = 0, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 1, CoreId = 0, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 2, CoreId = 1, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
                new CpuInformation { ModelName = "Intel(R) Xeon(R) CPU E5-2682 v4 @ 2.50GHz", ProcessorId = 3, CoreId = 1, PhysicalId = 0, ClockSpeed = "2500.000 MHz", CacheSize = "256 KB" },
            });
        }

        public Task<List<DriveInformation>> GetDrivesAsync(bool fixedOnly = true)
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

        public Task<List<InstalledPackage>> GetPackagesAsync(string root = "/")
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

        public Task<KernelInformation> GetKernelAsync()
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

        public Task<List<ProcessInformation>> GetProcessesAsync()
        {
            Process process = Process.GetCurrentProcess();
            return Task.FromResult(new List<ProcessInformation>()
            {
                new ProcessInformation()
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    WorkingSet = process.WorkingSet64,
                    CommandLine = "N/A",
                    ThreadCount = process.Threads.Count,
                    TotalCpuTime = process.TotalProcessorTime,
                    User = "N/A",
                }
            });
        }

        public Task<List<ServiceInformation>> GetServicesAsync()
        {
            return Task.FromResult(new List<ServiceInformation>()
            {
                new ServiceInformation()
                {
                    Name = "ssh",
                    Description = "OpenBSD Secure Shell server",
                    LoadState = "loaded",
                    ActiveState = "active",
                    SubState = "running",
                }
            });
        }

        public Task<SystemInformation> GetSystemStatusAsync()
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
