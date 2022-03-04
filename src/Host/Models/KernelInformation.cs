using System.Text.Json.Serialization;

namespace JetHub.Models
{
    public class KernelInformation
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("cmdline")]
        public string Cmdline { get; set; }
    }
}
