/// <copyright>
/// 
/// </copyright>
/// 
/// <summary>
///   Copied from https://github.com/Azure-App-Service/KuduLite/blob/dev/Kudu.Contracts/Editor/VfsStatEntry.cs
/// </summary>

namespace Xylab.Management.VirtualFileSystem;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a directory structure. Used by <see cref="VfsControllerBase"/> to browse
/// a Kudu file system or the git repository.
/// </summary>
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
