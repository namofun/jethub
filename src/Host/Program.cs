using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Xylab.Management.Automation.Cmdlets;
using Xylab.Management.Services;
using Xylab.Management.WebDeploy;
using Xylab.Management.WebDeploy.Deployment;
using Xylab.Remoting.PowerShellWebService;
using Xylab.Workflows.LogicApps.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration);

builder.Services.AddAuthorizationBuilder()
    .AddFallbackPolicy("DefaultRequireAuth", new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.WriteIndented = true);

builder.Services.Configure<GlobalOptions>(options =>
{
    options.HostName = System.Net.Dns.GetHostName();
    options.Branch = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GitBranchName")?.Value ?? "unknown";
    options.CommitId = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GitCommitId")?.Value ?? "unknown";
    options.Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
});

builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);

// add Virtual File System services
builder.Services.AddSingleton<IFileSystemV2, FileSystemV2>();

// add System Information services
builder.Services.AddSingleton<IHostSystem>(IHostSystem.CreateDefault());

// add Workflow Engine services
builder.Services.AddWorkflowEngine()
    .WithAzureStorageAccountConnectionString(builder.Configuration.GetValue<string>("WorkflowEngineStorage"));

// add Remoting PowerShell services
builder.Services.AddPowerShellWebService()
    .WithWebSocketSupport()
    .ImportAssembly(typeof(SayHelloWorld).Assembly);

// add Web Deploy services
builder.Services.AddWebDeploy()
    .WithDeploymentTarget<NginxStaticSite>();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LogHub>("/api/log-stream");
app.MapWebDeploy();
app.MapPowerShellWebSocket("/powershell/stream");

app.Run();
