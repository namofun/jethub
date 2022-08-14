namespace Xylab.Management.PowerShell.WebService
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Management.Automation;
    using Xylab.Management.PowerShell.Cmdlets;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.MapGet("/Say-HelloWorld", async (HttpContext context) =>
            {
                context.Response.ContentType = "application/xml";
                //C: \Users\tlylz\Source\Repos\namofun\jethub\artifacts\PowerShell.Cmdlets\bin\Debug\net6.0\Xylab.Management.PowerShell.Cmdlets.dll
                using PowerShell pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
                pwsh.AddCommand(new CmdletInfo("Say-HelloWorld", typeof(SayHelloWorld)));
                var result = await pwsh.InvokeAsync();
                return PSSerializer.Serialize(result);
            });

            app.MapGet("/Get-ChildItem", async (HttpContext context) =>
            {
                context.Response.ContentType = "application/xml";
                //C: \Users\tlylz\Source\Repos\namofun\jethub\artifacts\PowerShell.Cmdlets\bin\Debug\net6.0\Xylab.Management.PowerShell.Cmdlets.dll
                using PowerShell pwsh = PowerShell.Create(RunspaceMode.NewRunspace);
                pwsh.AddScript("Get-ChildItem Cert:\\\\CurrentUser\\\\My\\\\");
                var result = await pwsh.InvokeAsync();
                return PSSerializer.Serialize(result);
            });

            app.Run();
        }
    }
}