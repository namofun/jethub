using System.Text.Json.Serialization;

namespace Xylab.Management.Models
{
    public class ServiceInformation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("loadState")]
        public string LoadState { get; set; }

        [JsonPropertyName("activeState")]
        public string ActiveState { get; set; }

        [JsonPropertyName("subState")]
        public string SubState { get; set; }
    }
}
