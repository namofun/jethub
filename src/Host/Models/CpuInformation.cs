using System.Text.Json.Serialization;

namespace JetHub.Models
{
    public class CpuInformation
    {
        [JsonPropertyName("processorId")]
        public int ProcessorId { get; set; }

        [JsonPropertyName("modelName")]
        public string ModelName { get; set; }

        [JsonPropertyName("physicalId")]
        public int PhysicalId { get; set; }

        [JsonPropertyName("coreId")]
        public int CoreId { get; set; }

        [JsonPropertyName("clockSpeed")]
        public string ClockSpeed { get; set; }

        [JsonPropertyName("cacheSize")]
        public string CacheSize { get; set; }
    }
}
