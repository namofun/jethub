using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface ISystemInfo
    {
        Task<string> GetVersionAsync();

        Task<string> GetCmdlineAsync();

        Task<string> GetLoadavgAsync();

        Task<TimeSpan> GetUptimeAsync();

        string GetVfsRoot();
    }

    public class FakeSystemInfo : ISystemInfo
    {
        public Task<string> GetVersionAsync()
        {
            return Task.FromResult("Linux version 4.15.0-112-generic (buildd@lcy01-amd64-027) (gcc version 7.5.0 (Ubuntu 7.5.0-3ubuntu1~18.04)) #113-Ubuntu SMP Thu Jul 9 23:41:39 UTC 2020");
        }

        public Task<string> GetCmdlineAsync()
        {
            return Task.FromResult("BOOT_IMAGE=/boot/vmlinuz-4.15.0-112-generic root=UUID=00000000-0000-0000-0000-000000000000 ro vga=792 console=tty0 console=ttyS0,115200n8 net.ifnames=0 noibrs quiet splash vt.handoff=1");
        }

        public Task<string> GetLoadavgAsync()
        {
            return Task.FromResult("0.00, 0.00, 0.00");
        }

        public Task<TimeSpan> GetUptimeAsync()
        {
            return Task.FromResult(TimeSpan.FromSeconds(621260));
        }

        public string GetVfsRoot()
        {
            var playground = Path.Combine(Environment.CurrentDirectory, "playground");
            if (!Directory.Exists(playground)) Directory.CreateDirectory(playground);
            return playground;
        }
    }

    public class ProcfsSystemInfo : ISystemInfo
    {
        public Task<string> GetCmdlineAsync()
        {
            return File.ReadAllTextAsync("/proc/cmdline");
        }

        public async Task<string> GetLoadavgAsync()
        {
            var loadavg = await File.ReadAllTextAsync("/proc/loadavg");
            return string.Join(", ", loadavg.Trim().Split(' ').Take(3));
        }

        public async Task<TimeSpan> GetUptimeAsync()
        {
            var uptime = await File.ReadAllTextAsync("/proc/uptime");
            return TimeSpan.FromSeconds(Math.Floor(double.Parse(uptime.Trim().Split(' ')[0])));
        }

        public Task<string> GetVersionAsync()
        {
            return File.ReadAllTextAsync("/proc/version");
        }

        public string GetVfsRoot()
        {
            return "/opt/domjudge/judgehost/judgings";
        }
    }
}
