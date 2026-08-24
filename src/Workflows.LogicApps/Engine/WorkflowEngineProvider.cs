namespace Xylab.Workflows.LogicApps.Engine;

using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Workflows.Common.Extensions;
using Microsoft.Azure.Workflows.Common.Logging;
using Microsoft.Azure.Workflows.Common.PerformanceCounters;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.WebJobs.Extensions.Configuration;
using Microsoft.Azure.Workflows.Worker;
using Microsoft.Azure.Workflows.Worker.Dispatcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.ResourceStack.Common.Services;

public class WorkflowEngineProvider(
    IOptions<WorkflowEngineOptions> options,
    IHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    ILogger<WorkflowEngine> logger)
{
    private readonly EdgeFlowConfigurationSource configurationSource =
        EdgeFlowConfigurationSource.CreateDefault(
            options.Value.EndpointUri ?? new Uri("http://localhost"),
            options.Value.AppDirectoryPath ?? environment.ContentRootPath)
            .WithAzureStorageAccountCredentials(options.Value.AzureStorageAccountConnectionString!);

    private readonly TaskCompletionSource<WorkflowEngine> engineLazy = new();

    public Task<WorkflowEngine> CreateEngineAsync()
    {
        return CreateEngineAsync(configurationSource);
    }

    protected virtual async Task<WorkflowEngine> CreateEngineAsync(EdgeFlowConfigurationSource configuration)
    {
        CloudConfigurationManager.Instance = configuration;

        FlowEventSource flowEventSource = new(
            logger,
            new EdgeEtwEventSource(),
            new HostNameProvider(),
            "$extensionVersion",
            "$siteName",
            "$slotName",
            FlowConfiguration.EdgeSubscriptionId,
            FlowConfiguration.EdgeResourceGroupName,
            "$regionName",
            "$locationId");

        ServiceCollection workflowServices = new();

        EdgeFlowConfiguration flowConfiguration = new(
            configuration,
            workflowServices,
            flowEventSource,
            new WorkflowPerformanceCounters(flowEventSource),
            new ConfigurationRoot([]),
            configuration.AppDirectoryPath,
            configuration.EndpointUri);

        await flowConfiguration.Initialize();
        flowConfiguration.EnsureInitialized();

        HttpConfiguration httpConfiguration = new()
        {
            Formatters = new()
            {
                FlowJsonExtensions.JsonMediaTypeFormatter,
            },
        };

        EdgeManagementEngine edgeEngine = new(flowConfiguration, httpConfiguration);
        await edgeEngine.RegisterEdgeEnvironment();

        FlowJobsCallbackFactory callbackFactory = new(flowConfiguration, httpConfiguration, requestPipeline: null);
        flowConfiguration.InitializeFlowJobCallbackConfiguration(callbackFactory);

        EdgeFlowWebManagementEngine webMgmtEngine = new(flowConfiguration, httpConfiguration);
        EdgeFlowJobsDispatcher jobsDispatcher = new(flowConfiguration, httpConfiguration);
        WorkflowUriTemplateEngine uriTemplateEngine = new(httpContextAccessor, linkGenerator);

        workflowServices.AddSingleton<IMeteringEngine, EdgeMeteringEngine>();
        workflowServices.AddSingleton<FlowConfiguration>(flowConfiguration);
        workflowServices.AddSingleton<FlowUriTemplateEngine>(uriTemplateEngine);
        workflowServices.AddSingleton<HttpConfiguration>(httpConfiguration);
        workflowServices.AddSingleton<EdgeManagementEngine>(edgeEngine);
        workflowServices.AddSingleton<FlowJobsCallbackFactory>(callbackFactory);
        workflowServices.AddSingleton<EdgeFlowWebManagementEngine>(webMgmtEngine);
        workflowServices.AddSingleton<EdgeFlowJobsDispatcher>(jobsDispatcher);
        workflowServices.AddSingleton<FlowDebuggingEngine>();

        return new WorkflowEngine
        {
            Configuration = flowConfiguration,
            Management = webMgmtEngine,
            JobsDispatcher = jobsDispatcher,
            HttpConfiguration = httpConfiguration,
            UriTemplate = uriTemplateEngine,
        };
    }

    internal void SetEngine(WorkflowEngine engine)
    {
        engineLazy.SetResult(engine);
    }

    internal WorkflowEngine? GetInstanceOrCancel()
    {
        if (engineLazy.Task.IsCompleted)
        {
            return engineLazy.Task.Result;
        }
        else
        {
            engineLazy.TrySetCanceled();
            return null;
        }
    }

    public Task<WorkflowEngine> GetEngineAsync()
    {
        return engineLazy.Task;
    }
}
