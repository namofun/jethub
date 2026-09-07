namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class WebDeployExtensions
{
    public static OptionsBuilder<WebDeployOptions> AddWebDeploy(this IServiceCollection services)
    {
        services.TryAddSingleton<TraceSessionCoordinator>();
        services.TryAddSingleton<MSDeployEndpoint>();
        services.AddSingleton<IValidateOptions<WebDeployOptions>, WebDeployOptionsValidator>();
        return services.AddOptions<WebDeployOptions>().ValidateDataAnnotations().ValidateOnStart();
    }

    public static OptionsBuilder<WebDeployOptions> WithDeploymentTarget<TDeploymentTarget>(
        this OptionsBuilder<WebDeployOptions> builder)
        where TDeploymentTarget : class, IWebDeployDeploymentTarget
    {
        builder.Services.AddSingleton<IWebDeployDeploymentTarget, TDeploymentTarget>();
        TDeploymentTarget.ConfigureServices(builder.Services);
        return builder;
    }

    public static RouteHandlerBuilder MapWebDeploy(this IEndpointRouteBuilder builder, string baseUrl = "")
    {
        RouteGroupBuilder group = builder.MapGroup(baseUrl);
        MSDeployEndpoint endpoint = builder.ServiceProvider.GetRequiredService<MSDeployEndpoint>();
        var rb1 = group.MapMethods("/MsDeployAgentService", ["HEAD", "POST"], endpoint.HandleAsync);
        var rb2 = group.MapMethods("/msdeploy.axd", ["HEAD", "POST"], endpoint.HandleAsync);
        return new RouteHandlerBuilder([rb1, rb2]);
    }

    private class WebDeployOptionsValidator(IServiceProvider serviceProvider) : IValidateOptions<WebDeployOptions>
    {
        public ValidateOptionsResult Validate(string? name, WebDeployOptions options)
        {
            if (serviceProvider.GetService<IWebDeployDeploymentTarget>() == null)
            {
                return ValidateOptionsResult.Fail("No IWebDeployDeploymentTarget implementation is registered.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
