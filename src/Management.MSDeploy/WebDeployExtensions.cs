namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        baseUrl = ('/' + baseUrl.TrimStart('/')).TrimEnd('/');
        var rb1 = builder.MapMethods(baseUrl + "/MsDeployAgentService", ["HEAD", "POST"], HandleRequestAsync);
        var rb2 = builder.MapMethods(baseUrl + "/msdeploy.axd", ["HEAD", "POST"], HandleRequestAsync);
        return new RouteHandlerBuilder([rb1, rb2]);
    }

    private static Task HandleRequestAsync(HttpContext context, MSDeployEndpoint endpoint, CancellationToken cancellationToken)
    {
        return endpoint.HandleAsync(context, cancellationToken);
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
