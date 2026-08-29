namespace Xylab.Management.WebDeploy;

using System.ComponentModel.DataAnnotations;

public sealed class WebDeployOptions
{
    [Required]
    public string DeploymentRoot { get; set; } = "sites";

    [Range(1024 * 1024, 256 * 1024 * 1024)]
    public int MaximumRequestBytes { get; set; } = 70 * 1024 * 1024;

    [Range(1, 16)]
    public int MaximumConcurrentSyncRequests { get; set; } = 2;

    public NginxOptions Nginx { get; set; } = new();
}

public sealed class NginxOptions
{
    public bool Enabled { get; set; }

    public string ConfigurationDirectory { get; set; } = "/etc/nginx/sites-enabled";

    public string Executable { get; set; } = "nginx";

    public string[] ArgumentsPrefix { get; set; } = [];

    [Range(1, 65535)]
    public int ListenPort { get; set; } = 80;
}
