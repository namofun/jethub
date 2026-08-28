namespace Xylab.Remoting.PowerShellWebService;

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Options;
using Microsoft.PowerShell.Commands;

public class PowerShellRunspaceFactory(
    IOptions<PowerShellOptions> options,
    PowerShellAuthorizationManager powerShellAuthorizationManager)
{
    public virtual InitialSessionState CreateInitialSessionState()
    {
        InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        initialSessionState.AuthorizationManager = powerShellAuthorizationManager;
        initialSessionState.ImportPSModule(options.Value.ModulesToImport.ToArray());
        return initialSessionState;
    }

    public virtual Runspace CreateRunspace()
    {
        Runspace runspace = RunspaceFactory.CreateRunspace(CreateInitialSessionState());
        runspace.Open();

        using (PowerShell loadModulePwsh = PowerShell.Create(runspace))
        {
            if (options.Value.AssembliesToImport.Count > 0)
            {
                loadModulePwsh
                    .AddCommand(new CmdletInfo("Import-Module", typeof(ImportModuleCommand)))
                    .AddParameter(nameof(ImportModuleCommand.Assembly), options.Value.AssembliesToImport.ToArray())
                    .Invoke();
            }
        }

        return runspace;
    }
}
