namespace Xylab.Workflows.LogicApps;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xylab.Workflows.LogicApps.Engine;
using Xylab.Workflows.LogicApps.WebApi;

public static class WorkflowsWebApplicationBuilderExtensions
{
    public static OptionsBuilder<WorkflowEngineOptions> AddWorkflowEngine(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<WorkflowEngineProvider>();
        services.AddHostedService<WorkflowEngineHostedService>();
        return services.AddOptions<WorkflowEngineOptions>().ValidateDataAnnotations().ValidateOnStart();
    }

    public static OptionsBuilder<WorkflowEngineOptions> WithAzureStorageAccountConnectionString(
        this OptionsBuilder<WorkflowEngineOptions> builder,
        string azureStorageAccountConnectionString)
    {
        return builder.Configure(options => options.AzureStorageAccountConnectionString = azureStorageAccountConnectionString);
    }

    public static RouteHandlerBuilder MapWorkflows(
        this IEndpointRouteBuilder builder,
        [StringSyntax("Route")] string routePatternPrefix)
    {
        var accessor = builder.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        WorkflowsEndpoint endpoint = new(accessor);

        RouteGroupBuilder group = builder.MapGroup(routePatternPrefix);
        group.AddEndpointFilter<EngineReadinessFilter>();
        group.AddEndpointFilter<RequestCorrelationFilter>();
        group.AddEndpointFilter<ErrorResponseMessageExceptionFilter>();

        System.Delegate[] endpoints =
        [
            endpoint.GetFlows,
            endpoint.GetFlow,
            endpoint.GetVersions,
            endpoint.GetVersion,
            endpoint.GetTriggers,
            endpoint.GetTrigger,
            endpoint.InvokeTrigger,
            endpoint.GetRuns,
            endpoint.GetRun,
            endpoint.GetRunContents,
            endpoint.GetRunActions,
            endpoint.GetRunAction,
            endpoint.GetRunActionContents,
            endpoint.UpsertWorkflow,
        ];

        List<RouteHandlerBuilder> routes = new();
        foreach (var requestDelegate in endpoints)
        {
            var route = requestDelegate.Method.GetCustomAttributes(true).OfType<WorkflowsEndpoint.RouteAttribute>().Single();
            var routeMap = route switch
            {
                WorkflowsEndpoint.HttpGetAttribute => group.MapGet(route.Path, requestDelegate),
                WorkflowsEndpoint.HttpPostAttribute => group.MapPost(route.Path, requestDelegate),
                WorkflowsEndpoint.HttpAnyAttribute => group.Map(route.Path, requestDelegate),
                _ => throw new InvalidDataException("Unexpected workflow endpoint metadata"),
            };

            routeMap.WithName(nameof(WorkflowsEndpoint) + "." + requestDelegate.Method.Name);
            routes.Add(routeMap);
        }

        return new RouteHandlerBuilder(routes);
    }
}
