using System;
using System.Text.Json.Serialization;

namespace JetHub.Models
{
    public class SystemInformation
    {
        [JsonPropertyName("uptime")]
        public TimeSpan Uptime { get; set; }

        [JsonPropertyName("loadavg")]
        public double[] LoadAverages { get; set; }

        [JsonPropertyName("totalMemory")]
        public ulong TotalMemoryBytes { get; set; }

        [JsonPropertyName("usedMemory")]
        public ulong UsedMemoryBytes { get; set; }

        [JsonPropertyName("totalSwap")]
        public ulong TotalSwapBytes { get; set; }

        [JsonPropertyName("usedSwap")]
        public ulong UsedSwapBytes { get; set; }
    }
}
