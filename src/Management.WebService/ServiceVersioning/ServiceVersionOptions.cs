namespace Xylab.Management.ServiceVersioning;

public class ServiceVersionOptions
{
    public string ServiceName { get; set; }

    public string HostName { get; set; } = "Unknown";

    public string CommitId { get; set; } = "Unknown";

    public string Branch { get; set; } = "Unknown";

    public string Version { get; set; } = "Unknown";
}
