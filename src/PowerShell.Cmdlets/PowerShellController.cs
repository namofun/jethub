using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace Xylab.Management.Automation.Mvc
{
    [Route("/api/pwsh/[action]")]
    public class PowerShellController : ControllerBase
    {
        [HttpPost("{cmdletName}")]
        public async Task<IActionResult> Cmdlet([FromRoute] string cmdletName)
        {
            if (Request.ContentType == null || !Request.ContentType.StartsWith("application/xml"))
            {
                Response.Headers.Add("X-PSWS-Error", "Invalid request type.");
                return new BadRequestResult();
            }

            Hashtable boundParameters;
            using (StreamReader sr = new(Request.Body))
            {
                string value = await sr.ReadToEndAsync();
                PSObject inputObject = (PSObject)PSSerializer.Deserialize(value);
                if (inputObject.ImmediateBaseObject is not Hashtable)
                {
                    Response.Headers.Add("X-PSWS-Error", "Invalid $PSBoundParameters.");
                }

                boundParameters = (Hashtable)inputObject.ImmediateBaseObject;
            }

            using Runspace runspace = Bundle.CreateRunspace();
            using PowerShell pwsh = PowerShell.Create(runspace);
            pwsh.AddCommand(cmdletName).AddParameters(boundParameters);
            var result = await pwsh.InvokeAsync();

            return new ContentResult
            {
                Content = PSSerializer.Serialize(result),
                ContentType = "application/xml"
            };
        }

        [HttpPost]
        public async Task<IActionResult> Script()
        {
            if (Request.ContentType == null || !Request.ContentType.StartsWith("text/plain"))
            {
                Response.Headers.Add("X-PSWS-Error", "Invalid request type.");
                return new BadRequestResult();
            }

            string ps1Content;
            using (StreamReader sr = new(Request.Body))
            {
                ps1Content = await sr.ReadToEndAsync();
            }

            using Runspace runspace = Bundle.CreateRunspace();
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
}
