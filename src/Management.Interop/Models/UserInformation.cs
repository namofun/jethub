namespace Xylab.Management.Models;

using System.Text.Json.Serialization;

public class UserInformation
{
    [JsonPropertyName("name")]
    public string UserName { get; set; }

    [JsonPropertyName("uid")]
    public uint UserId { get; set; }

    [JsonPropertyName("gid")]
    public uint GroupId { get; set; }

    [JsonPropertyName("gecos")]
    public string Comment { get; set; }

    [JsonPropertyName("dir")]
    public string HomeDirectory { get; set; }

    [JsonPropertyName("shell")]
    public string Shell { get; set; }
}
