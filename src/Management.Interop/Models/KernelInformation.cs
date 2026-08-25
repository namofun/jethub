namespace Xylab.Management.Models;

using System.Text.Json.Serialization;

public class KernelInformation
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("cmdline")]
    public string Cmdline { get; set; }
}
