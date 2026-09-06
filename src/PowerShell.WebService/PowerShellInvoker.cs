namespace Xylab.Remoting.PowerShellWebService;

using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

public interface IPowerShellInvoker
{
    IPowerShellStream GetScriptStream(string scriptContent);

    IPowerShellStream GetCmdletStream(string cmdletName, Hashtable? boundParameters);

    bool TryParseParameters(string? clixml, out Hashtable? value);
}

internal class PowerShellInvoker(PowerShellRunspaceFactory runspaceFactory) : IPowerShellInvoker
{
    public IPowerShellStream GetScriptStream(string scriptContent)
    {
        Runspace runspace = runspaceFactory.CreateRunspace();
        PowerShell pwsh = PowerShell.Create(runspace);
        pwsh.AddScript(scriptContent);

        return new PowerShellStream(pwsh, runspace);
    }

    public IPowerShellStream GetCmdletStream(string cmdletName, Hashtable? boundParameters)
    {
        Runspace runspace = runspaceFactory.CreateRunspace();
        PowerShell pwsh = PowerShell.Create(runspace);
        pwsh.AddCommand(cmdletName);
        if (boundParameters != null)
        {
            pwsh.AddParameters(boundParameters);
        }

        return new PowerShellStream(pwsh, runspace);
    }

    public bool TryParseParameters(string? clixml, out Hashtable? value)
    {
        value = null;
        if (clixml == null)
        {
            return true;
        }

        try
        {
            if (PSSerializer.Deserialize(clixml) is PSObject { ImmediateBaseObject: Hashtable boundParameters })
            {
                value = boundParameters;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }
}
