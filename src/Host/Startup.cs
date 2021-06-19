using JetHub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace JetHub
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDirectoryBrowser();

            services.Configure<GlobalOptions>(options =>
            {
                options.HostName = System.Net.Dns.GetHostName();
                var info = typeof(Startup).Assembly.GetCustomAttribute<GitVersionAttribute>();
                options.Branch = info.Branch;
                options.CommitId = info.Version;
                options.Version = typeof(Startup).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            });

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<GlobalOptions>>().Value);
            services.AddSignalR();

            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                services.AddSingleton<ISystemInfo, ProcfsSystemInfo>();

                services.AddSingleton<AptPackageService>();
                services.AddSingleton<IPackageService>(sp => sp.GetRequiredService<AptPackageService>());
                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AptPackageService>());

                services.AddSingleton<DfFreeStorageInfo>();
                services.AddSingleton<IStorageInfo>(sp => sp.GetRequiredService<DfFreeStorageInfo>());
                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DfFreeStorageInfo>());

                services.AddSingleton<OneShotGlobalInfo>();
                services.AddSingleton<IGlobalInfo>(sp => sp.GetRequiredService<OneShotGlobalInfo>());
                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<OneShotGlobalInfo>());
            }
            else
            {
                services.AddSingleton<ISystemInfo, FakeSystemInfo>();
                services.AddSingleton<IPackageService, FakePackageService>();
                services.AddSingleton<IStorageInfo, FakeStorageInfo>();
                services.AddSingleton<IGlobalInfo, FakeGlobalInfo>();
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
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
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHub<LogHub>("/api/log-stream");

                endpoints.Map(
                    "/judgings/{**slug}",
                    endpoints.CreateApplicationBuilder()
                        .Use((context, next) =>
                        {
                            context.SetEndpoint(null);
                            return next();
                        })
                        .UseFileServer(new FileServerOptions
                        {
                            EnableDirectoryBrowsing = true,
                            EnableDefaultFiles = false,
                            RequestPath = "/judgings",
                            FileProvider =
                                new PhysicalFileProvider(
                                    endpoints.ServiceProvider
                                        .GetRequiredService<IGlobalInfo>()
                                        .VfsRoot)
                        })
                        .Use((context, _) =>
                        {
                            context.Response.StatusCode = 404;
                            return Task.CompletedTask;
                        })
                        .Build());
            });
        }
    }
}
