namespace Xylab.Remoting.PowerShellWebService;

using Microsoft.AspNetCore.Http;
using System.Collections;

public class PowerShellEndpoint(IPowerShellInvoker invoker)
{
    public async Task<IResult> ExecuteScript(HttpRequest request)
    {
        if (request.ContentType == null || !request.ContentType.StartsWith("text/plain"))
        {
            return Results.BadRequest(new { error = "Invalid request body." });
        }

        string scriptContent;
        using (StreamReader sr = new(request.Body))
        {
            scriptContent = await sr.ReadToEndAsync();
        }

        using IPowerShellStream pwsh = invoker.GetScriptStream(scriptContent);
        var resultClixml = await pwsh.ReadAsClixmlAsync();

        return Results.Content(resultClixml, "application/xml");
    }

    public async Task<IResult> ExecuteCmdlet(string cmdletName, HttpRequest request)
    {
        string serializedBoundParameters;
        using (StreamReader sr = new(request.Body))
        {
            serializedBoundParameters = await sr.ReadToEndAsync();
        }

        Hashtable? param = null;
        if (serializedBoundParameters.Length > 0)
        {
            if (request.ContentType == null || !request.ContentType.StartsWith("application/xml"))
            {
                return Results.BadRequest(new { error = "Invalid request body content type." });
            }

            if (!invoker.TryParseParameters(serializedBoundParameters, out param))
            {
                return Results.BadRequest(new { error = "Invalid $PSBoundParameters." });
            }
        }

        using IPowerShellStream pwsh = invoker.GetCmdletStream(cmdletName, param);
        var resultClixml = await pwsh.ReadAsClixmlAsync();

        return Results.Content(resultClixml, "application/xml");
    }
}
