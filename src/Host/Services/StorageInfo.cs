using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IStorageInfo
    {
        (double Used, double Total) Memory { get; }

        (double Used, double Total) Swap { get; }

        IReadOnlyDictionary<string, (string Type, double Used, double Total)> HardDrive { get; }

        IReadOnlyList<(string ServiceName, string JudgehostName)> Judgehosts { get; }
    }

    public class FakeStorageInfo : IStorageInfo
    {
        public (double Used, double Total) Memory { get; } = (118.0, 2048.0);

        public (double Used, double Total) Swap { get; } = (0.0, 973.0);

        public IReadOnlyDictionary<string, (string Type, double Used, double Total)> HardDrive { get; }
            = new Dictionary<string, (string Type, double Used, double Total)>
            {
                ["/dev/vda1"] = ("/", 6.24, 20.0),
                ["/dev/vda2"] = ("/boot/efi", 12.35, 20.0),
            };

        public IReadOnlyList<(string ServiceName, string JudgehostName)> Judgehosts { get; }
            = new List<(string ServiceName, string JudgehostName)>
            {
                ("domjudge-judgehost@0", "judgehost-0"),
                ("domjudge-judgehost@0", "judgehost-1"),
                ("domjudge-judgehost@0", "judgehost-2"),
                ("domjudge-judgehost@0", "judgehost-3"),
            };
    }

    public class DfFreeStorageInfo : BackgroundService, IStorageInfo
    {
        private readonly ISystemInfo _systemInfo;
        private readonly ILogger<DfFreeStorageInfo> _logger;

        public DfFreeStorageInfo(ISystemInfo systemInfo, ILogger<DfFreeStorageInfo> logger)
        {
            _systemInfo = systemInfo;
            _logger = logger;
            HardDrive = new Dictionary<string, (string Type, double Used, double Total)>();
            Judgehosts = new List<(string ServiceName, string JudgehostName)>();
        }

        private async Task UpdateMemoryCore()
        {
            try
            {
                /*
              total        used        free      shared  buff/cache   available
Mem:        8033768     5655212      438912      538512     1939644     1555132
Swap:       2097148       81152     2015996
                */
                var free = await _systemInfo.RunAsync("free", "-k", 500);
                var content = free.Trim().Split('\n');
                if (content.Length == 3)
                {
                    var swap_line = content[2];
                    var swap = swap_line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (swap.Length != 4) throw new Exception("Unknown free output.");
                    Swap = (int.Parse(swap[2]) / 1024.0, int.Parse(swap[1]) / 1024.0);

                    var mem_line = content[1];
                    var mem = mem_line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (mem.Length != 7) throw new Exception("Unknown free output.");
                    Memory = (int.Parse(mem[2]) / 1024.0, int.Parse(mem[1]) / 1024.0);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred during updating memory information.");
            }
        }

        private async Task UpdateHardDriveCore()
        {
            try
            {
                /*
Filesystem     1M-blocks  Used Available Use% Mounted on
udev                3899     0      3899   0% /dev
tmpfs                785     4       782   1% /run
/dev/nvme0n1p1    233706 84431    137336  39% /
tmpfs               3923   104      3819   3% /dev/shm
tmpfs                  5     1         5   1% /run/lock
tmpfs               3923     0      3923   0% /sys/fs/cgroup
/dev/loop2           162   162         0 100% /snap/gnome-3-28-1804/128
/dev/loop1             1     1         0 100% /snap/gnome-logs/103
/dev/loop4            98    98         0 100% /snap/core/10185
/dev/loop5             3     3         0 100% /snap/gnome-system-monitor/148
/dev/sda2             96    39        58  41% /boot/efi
tmpfs                785     1       785   1% /run/user/121
tmpfs                785     1       785   1% /run/user/1000
                 */

                var df = await _systemInfo.RunAsync("df", "-m", 1000);
                var dfs = new Dictionary<string, (string Type, double Used, double Total)>();
                foreach (var df_line in df.Trim().Split('\n').Skip(1))
                {
                    var mnt = df_line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (mnt.Length != 6) throw new Exception("Unknown df output.");
                    if (!mnt[0].StartsWith('/')) continue; // tmpfs, cgroups, etc..
                    if (mnt[0].StartsWith("/dev/loop")) continue; // gnome things
                    dfs.Add(mnt[0], (mnt[5], int.Parse(mnt[2]) / 1024.0, int.Parse(mnt[1]) / 1024.0));
                }

                HardDrive = dfs;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred during updating disk information.");
            }
        }

        private Task UpdateJudgehostsCore()
        {
            const string ReadingDirectory = "/opt/domjudge/judgehost/judgings";
            var judgehosts = new List<(string, string)>();
            foreach (var dirname in Directory.GetDirectories(ReadingDirectory))
            {
                if (!dirname.StartsWith(ReadingDirectory + "/")) continue;
                var dirname2 = dirname.Substring(ReadingDirectory.Length + 1);
                judgehosts.Add((dirname2, dirname2));
            }

            Judgehosts = judgehosts;
            return Task.CompletedTask;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int i = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateMemoryCore();
                if (i == 0) await UpdateHardDriveCore();
                if (i == 0) await UpdateJudgehostsCore();
                i++; if (i == 10) i = 0; // update hdd per 5min
                await Task.Delay(30 * 1000, stoppingToken);
            }
        }

        public (double Used, double Total) Memory { get; private set; }
        
        public (double Used, double Total) Swap { get; private set; }
        
        public IReadOnlyDictionary<string, (string Type, double Used, double Total)> HardDrive { get; private set; }

        public IReadOnlyList<(string ServiceName, string JudgehostName)> Judgehosts { get; private set; }
    }
}
