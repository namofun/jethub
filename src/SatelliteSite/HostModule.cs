using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace SatelliteSite
{
    public class HostModule : AbstractModule
    {
        public override string Area => string.Empty;

        public override void Initialize()
        {
        }

        public override void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureIdentityAdvanced(options =>
            {
                options.ShortenedClaimName = true;
            });
        }

        public override void RegisterEndpoints(IEndpointBuilder endpoints)
        {
            endpoints.MapRequestDelegate("/", context =>
            {
                context.Response.Redirect("/dashboard");
                return Task.CompletedTask;
            })
            .WithDisplayName("Home Page");

            endpoints.MapControllers();
        }

        public override void RegisterMenu(IMenuContributor menus)
        {
            menus.Menu(MenuNameDefaults.DashboardContent, menu =>
            {
                menu.HasSubmenu(300, jobs =>
                {
                    jobs.HasTitle(string.Empty, "Job tests")
                        .HasLink("javascript:;");

                    jobs.HasEntry(0)
                        .HasTitle(string.Empty, "Create Job 1")
                        .HasLink(string.Empty, "Test", "CreateJob");

                    jobs.HasEntry(100)
                        .HasTitle(string.Empty, "Create Job 2")
                        .HasLink(string.Empty, "Test", "CreateJob2");
                });
            });
        }
    }
}
