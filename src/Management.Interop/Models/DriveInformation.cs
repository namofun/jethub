using System.IO;
using System.Text.Json.Serialization;

namespace Xylab.Management.Models
{
    public class DriveInformation
    {
        [JsonPropertyName("category")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DriveType Category { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("fileSystem")]
        public string FileSystem { get; set; }

        [JsonPropertyName("totalSize")]
        public ulong TotalSizeBytes { get; set; }

        [JsonPropertyName("usedSize")]
        public ulong UsedSizeBytes { get; set; }

        [JsonPropertyName("mountPoint")]
        public string MountPoint { get; set; }
    }
}
