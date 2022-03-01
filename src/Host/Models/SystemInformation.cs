using System;

namespace JetHub.Models
{
    public class SystemInformation
    {
        public TimeSpan Uptime { get; set; }

        public ulong[] LoadAverages { get; set; }

        public ulong TotalMemory { get; set; }

        public ulong UsedMemory { get; set; }

        public ulong TotalSwap { get; set; }

        public ulong UsedSwap { get; set; }

        public ushort ProcessCount { get; set; }
    }
}
