using JetHub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

namespace JetHub
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddDirectoryBrowser();

            builder.Services.Configure<GlobalOptions>(options =>
            {
                options.HostName = System.Net.Dns.GetHostName();
                var info = typeof(Program).Assembly.GetCustomAttribute<GitVersionAttribute>();
                options.Branch = info.Branch;
                options.CommitId = info.Version;
                options.Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            });

            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<GlobalOptions>>().Value);
            builder.Services.AddSignalR();

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                builder.Services.AddSingleton<IHostSystem, LinuxSystem>();
                builder.Services.AddSingleton<ISystemInfo, ProcfsSystemInfo>();

                builder.Services.AddSingleton<AptPackageService>();
                builder.Services.AddSingleton<IPackageService>(sp => sp.GetRequiredService<AptPackageService>());
                builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AptPackageService>());

                builder.Services.AddSingleton<DfFreeStorageInfo>();
                builder.Services.AddSingleton<IStorageInfo>(sp => sp.GetRequiredService<DfFreeStorageInfo>());
                builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DfFreeStorageInfo>());

                builder.Services.AddSingleton<OneShotGlobalInfo>();
                builder.Services.AddSingleton<IGlobalInfo>(sp => sp.GetRequiredService<OneShotGlobalInfo>());
                builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<OneShotGlobalInfo>());
            }
            else
            {
                builder.Services.AddSingleton<IHostSystem, FakeSystem>();
                builder.Services.AddSingleton<ISystemInfo, FakeSystemInfo>();
                builder.Services.AddSingleton<IPackageService, FakePackageService>();
                builder.Services.AddSingleton<IStorageInfo, FakeStorageInfo>();
                builder.Services.AddSingleton<IGlobalInfo, FakeGlobalInfo>();
            }

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<LogHub>("/api/log-stream");
            app.MapFileServer("/judgings", new PhysicalFileProvider(app.Services.GetRequiredService<IGlobalInfo>().VfsRoot));

            app.Run();
        }
    }
}
