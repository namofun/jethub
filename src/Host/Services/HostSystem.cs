using JetHub.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemInformationAsync();
    }

    public class LinuxSystem : IHostSystem
    {
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
