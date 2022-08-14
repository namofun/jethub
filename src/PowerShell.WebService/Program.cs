namespace Xylab.Management.PowerShell.WebService
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using Xylab.Management.Automation;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.MapGet("/api/pwsh/{cmdletName}", async (HttpContext context, string cmdletName) =>
            {
                context.Response.ContentType = "application/xml";

                using Runspace runspace = Bundle.CreateRunspace();
                using PowerShell pwsh = PowerShell.Create(runspace);
                pwsh.AddCommand(cmdletName);
                var result = await pwsh.InvokeAsync();
                return PSSerializer.Serialize(result);
            });

            app.Run();
        }
    }
}