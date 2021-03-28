using JetHub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

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

            services.Configure<GlobalOptions>(options =>
            {
                options.HostName = System.Net.Dns.GetHostName();
                var info = typeof(Startup).Assembly.GetCustomAttribute<GitVersionAttribute>();
                options.Branch = info.Branch;
                options.CommitId = info.Version;
            });

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<GlobalOptions>>().Value);

            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                services.AddSingleton<ISystemInfo, ProcfsSystemInfo>();
            }
            else
            {
                services.AddSingleton<ISystemInfo, FakeSystemInfo>();
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
            });
        }
    }
}
