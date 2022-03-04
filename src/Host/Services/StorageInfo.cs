using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JetHub.Services
{
    public interface IStorageInfo
    {
        IReadOnlyList<(string ServiceName, string JudgehostName)> Judgehosts { get; }
    }

    public class FakeStorageInfo : IStorageInfo
    {
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
        public DfFreeStorageInfo()
        {
            Judgehosts = new List<(string ServiceName, string JudgehostName)>();
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
                if (i == 0) await UpdateJudgehostsCore();
                i++; if (i == 10) i = 0; // update hdd per 5min
                await Task.Delay(30 * 1000, stoppingToken);
            }
        }

        public IReadOnlyList<(string ServiceName, string JudgehostName)> Judgehosts { get; private set; }
    }
}
