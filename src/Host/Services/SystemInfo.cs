using System;
using System.Collections.Generic;
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

        Task<List<string>> GetRunningServicesAsync();

        Task<Dictionary<string, int>> GetProcessorsAsync();

        Task<(double Used, double Total)> GetMemoryStatisticsAsync();

        Task<Dictionary<string, (string Type, double Used, double Total)>> GetHardDriveStatisticsAsync();

        Task<(string CommitId, string Branch)> GetJudgehostVersionInfoAsync();
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

        public Task<List<string>> GetRunningServicesAsync()
        {
            return Task.FromResult(new List<string>
            {
                "domjudge-judgehost@0",
                "domjudge-judgehost@1",
                "domjudge-judgehost@2",
                "domjudge-judgehost@3",
            });
        }

        public Task<Dictionary<string, int>> GetProcessorsAsync()
        {
            return Task.FromResult(new Dictionary<string, int>
            {
                ["Intel(R) Xeon(R) CPU E5-2682 v4 @@ 2.50GHz"] = 4,
            });
        }

        public Task<(double Used, double Total)> GetMemoryStatisticsAsync()
        {
            return Task.FromResult((118.0, 2048.0));
        }

        public Task<Dictionary<string, (string Type, double Used, double Total)>> GetHardDriveStatisticsAsync()
        {
            return Task.FromResult(new Dictionary<string, (string Type, double Used, double Total)>
            {
                ["/dev/vda1"] = ("ext4", 6.24, 20.0),
                ["/dev/vda2"] = ("ext4", 12.35, 20.0),
            });
        }

        public Task<(string, string)> GetJudgehostVersionInfoAsync()
        {
            return Task.FromResult(("abcdefg", "master"));
        }
    }

    public class ProcfsSystemInfo : ISystemInfo
    {
        public Task<string> GetCmdlineAsync()
        {
            return File.ReadAllTextAsync("/proc/cmdline");
        }

        public Task<Dictionary<string, (string Type, double Used, double Total)>> GetHardDriveStatisticsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetJudgehostVersionInfoAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetLoadavgAsync()
        {
            var loadavg = await File.ReadAllTextAsync("/proc/loadavg");
            return string.Join(", ", loadavg.Trim().Split(' ').Take(3));
        }

        public Task<(double Used, double Total)> GetMemoryStatisticsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, int>> GetProcessorsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetRunningServicesAsync()
        {
            throw new NotImplementedException();
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
