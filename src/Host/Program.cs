using JetHub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO.Abstractions;
using System.Reflection;
using Xylab.Management.Services;
using Xylab.Workflows.LogicApps.Engine;

namespace JetHub
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.Configure<GlobalOptions>(options =>
            {
                options.HostName = System.Net.Dns.GetHostName();
                var info = typeof(Program).Assembly.GetCustomAttribute<GitVersionAttribute>();
                options.Branch = info.Branch;
                options.CommitId = info.Version;
                options.Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            });

            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IFileSystemV2, FileSystemV2>();
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                builder.Services.AddSingleton<IHostSystem, LinuxSystem>();
            }
            else
            {
                builder.Services.AddSingleton<IHostSystem, FakeSystem>();
            }

            builder.Services.AddWorkflowEngine(options =>
            {
                options.AzureStorageAccountConnectionString = "UseDevelopmentStorage=true";
                options.InitializeConnectionsFromJson(
                    System.IO.File.ReadAllText(
                        System.IO.Path.Combine(
                            builder.Environment.ContentRootPath,
                            "connections.json")));
            });

            var app = builder.Build();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<LogHub>("/api/log-stream");

            app.Run();
        }
    }
}
