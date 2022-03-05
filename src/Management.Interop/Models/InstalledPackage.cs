using System.Text.Json.Serialization;

namespace Xylab.Management.Models
{
    public class InstalledPackage
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("arch")]
        public string Architect { get; set; }
    }
}
