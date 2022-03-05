using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public class LinuxSystem : IHostSystem
    {
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

            if (fixedOnly) drives.RemoveAll(d => d.TotalSizeBytes == 0);
            return Task.FromResult(drives);
        }

        public async Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/")
        {
            root += (root.EndsWith("/") ? "" : "/") + "var/lib/dpkg/status";
            if (!File.Exists(root))
            {
                return new List<InstalledPackage>();
            }

            return Interop.Parser.DpkgStatus(await File.ReadAllLinesAsync(root));
        }

        public async Task<KernelInformation> GetKernelInformationAsync()
        {
            return new KernelInformation()
            {
                Cmdline = (await File.ReadAllTextAsync("/proc/cmdline")).Trim(),
                Version = (await File.ReadAllTextAsync("/proc/version")).Trim(),
            };
        }

        public Task<List<ProcessInformation>> GetProcessInformationAsync()
        {
            const string ProcfsDir = "/proc/";
            List<(int pid, string stat, string status, string cmdline)> processes = new();
            foreach (string directoryName in Directory.EnumerateDirectories(ProcfsDir))
            {
                if (!int.TryParse(directoryName[ProcfsDir.Length..], out int processId) || processId <= 0) continue;
                processes.Add((
                    pid: processId,
                    stat: File.ReadAllText($"/proc/{processId}/stat"),
                    status: File.ReadAllText($"/proc/{processId}/status"),
                    cmdline: File.ReadAllText($"/proc/{processId}/cmdline")));
            }

            Dictionary<uint, string> usersMapping = new();
            string GetUserNameById(uint userId)
            {
                if (!usersMapping.TryGetValue(userId, out string userName))
                {
                    var passwd = Interop.Libc.getpwuid(userId);
                    usersMapping.Add(userId, passwd?.pw_name ?? userId.ToString());
                }

                return userName;
            }

            List<ProcessInformation> parsedProcesses = new();
            foreach (var process in processes)
            {
                parsedProcesses.Add(
                    Interop.Parser.ProcfsPsinfo(
                        process,
                        GetUserNameById));
            }

            return Task.FromResult(parsedProcesses);
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
}
