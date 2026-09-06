namespace Xylab.Remoting.PowerShellWebService;

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class PowerShellHub(IPowerShellInvoker invoker) : Hub
{
    public async IAsyncEnumerable<KeyValuePair<string, string>> ExecuteScript(string scriptContent)
    {
        using IPowerShellStream pwsh = invoker.GetScriptStream(scriptContent);
        await foreach (var entry in pwsh.EnumerateAsStreamClixmlAsync().WithCancellation(Context.ConnectionAborted))
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<KeyValuePair<string, string>> ExecuteCmdlet(string cmdletName, string? serializedBoundParameters)
    {
        if (!invoker.TryParseParameters(serializedBoundParameters, out Hashtable? param))
        {
            throw new HubException("Invalid $PSBoundParameters.");
        }

        using IPowerShellStream pwsh = invoker.GetCmdletStream(cmdletName, param);
        await foreach (var entry in pwsh.EnumerateAsStreamClixmlAsync().WithCancellation(Context.ConnectionAborted))
        {
            yield return entry;
        }
    }
}
