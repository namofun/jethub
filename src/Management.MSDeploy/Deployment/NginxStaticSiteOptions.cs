namespace Xylab.Management.WebDeploy.Deployment;

using System.ComponentModel.DataAnnotations;

public sealed class NginxStaticSiteOptions
{
    [Required]
    public string DeploymentRoot { get; set; } = "sites";

    public bool Enabled { get; set; }

    public string ConfigurationDirectory { get; set; } = "/etc/nginx/sites-enabled";

    public string Executable { get; set; } = "nginx";

    public string[] ArgumentsPrefix { get; set; } = [];

    [Range(1, 65535)]
    public int ListenPort { get; set; } = 80;
}
