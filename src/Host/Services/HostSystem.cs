using JetHub.Models;
using System;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemInformationAsync();
    }

    public class LinuxSystem : IHostSystem
    {
        public Task<SystemInformation> GetSystemInformationAsync()
        {
            Interop.Libc.sysinfo(out Interop.Libc.sysinfo_t sysinfo);
            return Task.FromResult(new SystemInformation
            {
                UsedSwapBytes = sysinfo.totalswap - sysinfo.freeswap,
                UsedMemoryBytes = sysinfo.totalram - sysinfo.freeram - sysinfo.bufferram,
                Uptime = TimeSpan.FromSeconds(sysinfo.uptime),
                TotalSwapBytes = sysinfo.totalswap,
                TotalMemoryBytes = sysinfo.totalram,
                LoadAverages = Interop.Libc.sysinfo_loads_to100(sysinfo.loads),
                ProcessCount = sysinfo.procs,
            });
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
                ProcessCount = 309,
            });
        }
    }
}
