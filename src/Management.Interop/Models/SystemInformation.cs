namespace Xylab.Management.Models;

using System;
using System.Text.Json.Serialization;

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
