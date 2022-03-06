using System;
using System.Text.Json.Serialization;

namespace Xylab.Management.VirtualFileSystem
{
    // https://github.com/Azure-App-Service/KuduLite
    public class VfsStatEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("mtime")]
        public DateTimeOffset ModifyTime { get; set; }

        [JsonPropertyName("crtime")]
        public DateTimeOffset CreateTime { get; set; }

        [JsonPropertyName("mime")]
        public string Mime { get; set; }

        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
}
