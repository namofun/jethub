using System;
using System.Text.Json.Serialization;

namespace Xylab.Management.Models
{
    public class ProcessInformation
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("threads")]
        public int ThreadCount { get; set; }

        [JsonPropertyName("memory")]
        public long WorkingSet { get; set; }

        [JsonPropertyName("cpuTime")]
        public TimeSpan TotalCpuTime { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("cmdline")]
        public string CommandLine { get; set; }
    }
}
