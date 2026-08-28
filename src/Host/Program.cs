using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xylab.Management.Automation.Cmdlets;
using Xylab.Management.Services;
using Xylab.Remoting.PowerShellWebService;
using Xylab.Workflows.LogicApps.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

// add System Interop services
builder.Services.AddSingleton<IHostSystem>(IHostSystem.CreateDefault());

builder.Services.AddWorkflowEngine(options =>
{
    options.AzureStorageAccountConnectionString = "UseDevelopmentStorage=true";
});

builder.Services.AddPowerShellWebService(options =>
{
    options.AssembliesToImport.Add(typeof(SayHelloWorld).Assembly);
});

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LogHub>("/api/log-stream");
app.MapPowerShellWebSocket("/powershell/stream");

app.Run();
