namespace Xylab.Remoting.PowerShellWebService;

using System.Management.Automation;
using System.Management.Automation.Host;
using Microsoft.Extensions.Options;

public class PowerShellAuthorizationManager(IOptions<PowerShellOptions> options)
    : AuthorizationManager(options.Value.ShellId)
{
    protected override bool ShouldRun(CommandInfo commandInfo, CommandOrigin origin, PSHost host, out Exception? reason)
    {
        if (origin == CommandOrigin.Internal)
        {
            reason = null;
            return true;
        }
        else
        {
            return base.ShouldRun(commandInfo, origin, host, out reason);
        }
    }
}
