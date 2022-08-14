using Microsoft.PowerShell.Commands;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Xylab.Management.Automation
{
    public static class Bundle
    {
        public static Runspace CreateRunspace()
        {
            InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.AuthorizationManager = new RoleBasedAuthroizationManager("PowerShellWebService");

            Runspace runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            runspace.Open();

            using (PowerShell loadModulePwsh = PowerShell.Create(runspace))
            {
                loadModulePwsh
                    .AddCommand(new CmdletInfo("Import-Module", typeof(ImportModuleCommand)))
                    .AddParameter(nameof(ImportModuleCommand.Assembly), new[] { typeof(Bundle).Assembly })
                    .Invoke();
            }

            return runspace;
        }
    }
}
