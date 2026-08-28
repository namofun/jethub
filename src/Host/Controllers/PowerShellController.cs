namespace Xylab.Management.Controllers;

using Microsoft.AspNetCore.Mvc;
using Xylab.Remoting.PowerShellWebService;

[Route("powershell")]
public class PowerShellController(PowerShellRunspaceFactory runspaceFactory)
    : PowerShellControllerBase(runspaceFactory)
{
}
