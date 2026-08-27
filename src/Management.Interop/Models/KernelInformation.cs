namespace Xylab.Management.Models;

using System.Text.Json.Serialization;

public class KernelInformation
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("cmdline")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Cmdline { get; set; }
}
