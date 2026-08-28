namespace Xylab.Remoting.PowerShellWebService;

using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public abstract class PowerShellControllerBase(PowerShellRunspaceFactory runspaceFactory) : ControllerBase
{
    [HttpPost("cmdlet/{cmdletName}")]
    public async Task<IActionResult> Cmdlet([FromRoute] string cmdletName)
    {
        if (Request.ContentType == null || !Request.ContentType.StartsWith("application/xml"))
        {
            Response.Headers.Append("X-PSWS-Error", "Invalid request type.");
            return new BadRequestResult();
        }

        Hashtable boundParameters;
        using (StreamReader sr = new(Request.Body))
        {
            string value = await sr.ReadToEndAsync();
            PSObject inputObject = (PSObject)PSSerializer.Deserialize(value);
            if (inputObject.ImmediateBaseObject is not Hashtable)
            {
                Response.Headers.Append("X-PSWS-Error", "Invalid $PSBoundParameters.");
            }

            boundParameters = (Hashtable)inputObject.ImmediateBaseObject;
        }

        using Runspace runspace = runspaceFactory.CreateRunspace();
        using PowerShell pwsh = PowerShell.Create(runspace);
        pwsh.AddCommand(cmdletName).AddParameters(boundParameters);
        var result = await pwsh.InvokeAsync();

        return new ContentResult
        {
            Content = PSSerializer.Serialize(result),
            ContentType = "application/xml"
        };
    }

    [HttpPost("script")]
    public async Task<IActionResult> Script()
    {
        if (Request.ContentType == null || !Request.ContentType.StartsWith("text/plain"))
        {
            Response.Headers.Append("X-PSWS-Error", "Invalid request type.");
            return new BadRequestResult();
        }

        string ps1Content;
        using (StreamReader sr = new(Request.Body))
        {
            ps1Content = await sr.ReadToEndAsync();
        }

        using Runspace runspace = runspaceFactory.CreateRunspace();
        using PowerShell pwsh = PowerShell.Create(runspace);
        pwsh.AddScript(ps1Content);
        var result = await pwsh.InvokeAsync();

        return new ContentResult
        {
            Content = PSSerializer.Serialize(result),
            ContentType = "application/xml"
        };
    }
}
