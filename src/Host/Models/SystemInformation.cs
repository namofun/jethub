using System;

namespace JetHub.Models
{
    public class SystemInformation
    {
        public TimeSpan Uptime { get; set; }

        public double[] LoadAverages { get; set; }

        public ulong TotalMemoryBytes { get; set; }

        public ulong UsedMemoryBytes { get; set; }

        public ulong TotalSwapBytes { get; set; }

        public ulong UsedSwapBytes { get; set; }
    }
}
