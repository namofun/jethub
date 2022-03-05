using System;
using System.Linq;
using Xylab.Management.Models;

namespace Xylab.Management.Interop
{
    internal partial class Parser
    {
        public static ProcessInformation ProcfsPsinfo(
            (int pid, string stat, string status, string cmdline) proc,
            Func<uint, string> userNameResolver)
        {
            ProcessInformation instance = new() { Id = proc.pid };
            foreach (ReadOnlySpan<char> line in proc.status.AsSpan().EnumerateLines())
            {
                int startIndex = line.IndexOf(':');
                if (startIndex == -1) break;

                ReadOnlySpan<char> key = line[..startIndex];
                if (key.SequenceEqual("Name"))
                {
                    instance.Name = line[(startIndex + 1)..].Trim().ToString();
                }
                else if (key.SequenceEqual("Threads"))
                {
                    instance.ThreadCount = int.Parse(line[(startIndex + 1)..].Trim());
                }
                else if (key.SequenceEqual("VmRSS"))
                {
                    instance.WorkingSet = long.Parse(line[(startIndex + 1)..^3].Trim()) << 10;
                }
                else if (key.SequenceEqual("Uid"))
                {
                    ReadOnlySpan<char> uid4 = line[(startIndex + 1)..].TrimStart();
                    uint uid = uint.Parse(uid4[0..uid4.IndexOf('\t')]);
                    instance.User = userNameResolver(uid);
                }
            }

            // According to runtime/src/libraries/Common/src/Interop/Linux/procfs/Interop.ProcFsStat.cs
            // utime -> 14th, stime -> 15th
            ReadOnlySpan<char> stat = proc.stat;
            for (int i = 0; i < 13; i++) NextOrFail(ref stat, ' ', out _);
            NextInt64OrFail(ref stat, ' ', out long utime);
            NextInt64OrFail(ref stat, ' ', out long stime);
            instance.TotalCpuTime = Libc.TicksToTimeSpan(utime + stime);

            instance.CommandLine = proc.cmdline.Trim();
            return instance;
        }

        private static void NextOrFail(ref ReadOnlySpan<char> src, char separator, out ReadOnlySpan<char> output)
        {
            int idx = src.IndexOf(separator);
            if (idx == -1) throw new ArgumentException("Unable to parse.");
            output = src[..idx];
            src = src[(idx + 1)..];
        }

        private static void NextInt64OrFail(ref ReadOnlySpan<char> src, char separator, out long output)
        {
            int idx = src.IndexOf(separator);
            if (idx == -1) throw new ArgumentException("Unable to parse.");
            output = long.Parse(src[..idx]);
            src = src[(idx + 1)..];
        }
    }
}
