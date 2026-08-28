namespace Xylab.Remoting.PowerShellWebService;

using System.Reflection;

public class PowerShellOptions
{
    public string ShellId { get; set; } = "PowerShellWebService";

    public List<Assembly> AssembliesToImport { get; } = [];

    public List<string> ModulesToImport { get; } = [];
}
