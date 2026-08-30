namespace Xylab.Workflows.LogicApps.Engine;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.Azure.Workflows.Common.Entities;
using Microsoft.Azure.Workflows.Data;
using Microsoft.Azure.Workflows.Data.Common;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Extensions;
using Microsoft.Azure.Workflows.Templates.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Storage;
using Newtonsoft.Json.Linq;
using Xylab.Workflows.LogicApps.Mvc;

public static class WorkflowEngineExtensions
{
    public static Task<Flow> FindFlowByIdentifier(this WorkflowEngine engine, string identifier)
    {
        return engine.GetRegionalDataProvider()
            .FindFlowByIdentifier(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                flowId: identifier);
    }

    public static Task<Flow> FindFlowByName(this WorkflowEngine engine, string flowName)
    {
        return engine.GetRegionalDataProvider()
            .FindFlowByName(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                flowName: flowName);
    }

    public static async Task<SegmentedList<Flow>> FindFlowsSegmented(
        this WorkflowEngine engine,
        FlowStorageFilter? filter = null,
        int? top = null,
        DataContinuationToken? continuationToken = null)
    {
        return new SegmentedList<Flow>(
            await engine.GetRegionalDataProvider()
                .FindFlowsSegmentedBySubscription(
                    subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                    filter: filter ?? new FlowStorageFilter(),
                    top: top,
                    continuationToken: continuationToken));
    }

    public static async Task<Flow> FindFlowByIdOrName(this WorkflowEngine engine, string id)
    {
        return (await engine.FindFlowByIdentifier(id))
            ?? (await engine.FindFlowByName(id));
    }

    public static async Task<SegmentedList<FlowRun>> FindFlowRunsSegmented(
        this WorkflowEngine engine,
        Flow flow,
        string? startSequenceId = null,
        string? endSequenceId = null,
        ValueFilter<FlowStatus>[]? statusFilters = null,
        int? top = null,
        DataContinuationToken? continuationToken = null)
    {
        return new SegmentedList<FlowRun>(
            await engine.GetScaleUnitDataProvider(new FlowDataPlaneContext(flow), flow.ScaleUnit)
                .FindFlowRunsSegmented(
                    flowId: flow.FlowId,
                    startSequenceId: startSequenceId,
                    endSequenceId: endSequenceId,
                    statusFilters: statusFilters,
                    top: top,
                    continuationToken: continuationToken));
    }

    public static Task<FlowRun> FindFlowRun(this WorkflowEngine engine, Flow flow, string sequenceId)
    {
        return engine.GetScaleUnitDataProvider(flow.ScaleUnit)
            .FindFlowRun(
                flowId: flow.FlowId,
                flowRunSequenceId: sequenceId);
    }

    public static Task<FlowRunAction[]> FindFlowRunActions(this WorkflowEngine engine, Flow flow, FlowRun run)
    {
        return engine.GetScaleUnitDataProvider(flow.ScaleUnit)
            .FindFlowRunActions(
                platformOptions: run.PlatformOptions,
                flowId: flow.FlowId,
                flowRunSequenceId: run.FlowRunSequenceId);
    }

    public static Task<FlowRunAction> FindFlowRunAction(this WorkflowEngine engine, Flow flow, FlowRun run, string actionName)
    {
        return engine.GetScaleUnitDataProvider(flow.ScaleUnit)
            .FindFlowRunAction(
                platformOptions: run.PlatformOptions,
                flowId: flow.FlowId,
                flowRunSequenceId: run.FlowRunSequenceId,
                actionName: actionName);
    }

    public static Task<JToken> GetContentLink(this WorkflowEngine engine, Flow flow, string flowContentSequenceId, ContentLink? contentLink)
    {
        if (contentLink == null)
        {
            return Task.FromResult<JToken>(JRaw.CreateNull());
        }

        return engine.GetScaleUnitDataProvider(flow.ScaleUnit)
            .DownloadFlowOperationContent(
                flowId: flow.FlowId,
                flowContentSequenceId: flowContentSequenceId,
                contentLink: contentLink);
    }

    public static async Task<SegmentedList<Flow>> FindFlowVersionsSegmented(
        this WorkflowEngine engine,
        Flow flow,
        int? top = null,
        DataContinuationToken? continuationToken = null)
    {
        return new(await engine.GetRegionalDataProvider()
            .FindFlowVersionsSegmentedByIdentifier(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                flowId: flow.FlowId,
                top: top,
                continuationToken: continuationToken));
    }

    public static async Task<Flow> FindFlowVersion(this WorkflowEngine engine, Flow flow, string version)
    {
        return await engine.GetRegionalDataProvider()
            .FindFlowBySequence(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                flowId: flow.FlowId,
                flowSequenceId: version);
    }

    public static Task ValidateAndCreateFlow(
        this WorkflowEngine engine,
        string flowName,
        FlowPropertiesDefinition flowPropertiesDefinition)
    {
        return engine.Management.ValidateAndCreateFlow("app", flowName, flowPropertiesDefinition, FlowConfiguration.EdgeResourceGroupName);
    }

    public static async Task<IActionResult> InvokeFlowTrigger(
        this WorkflowEngine engine,
        Flow flow,
        string triggerName,
        FlowTemplateTrigger trigger,
        HttpRequestMessage req,
        CancellationToken cancellationToken)
    {
        if (trigger.IsFlowRecurrentTrigger() || trigger.IsNotificationTrigger())
        {
            await engine.Management.RunFlowRecurrentTrigger(new CachedSubscription(), flow, new FlowDataPlaneContext(flow), flow.FlowName, triggerName);

            return new NewtonsoftJsonResult(
                await engine.GetScaleUnitJobsProvider(flow.ScaleUnit, FlowExecutionClusterType.NotSpecified)
                    .GetFlowRecurrentTriggerJob(
                        flowId: flow.FlowId,
                        triggerName: triggerName))
            {
                StatusCode = (int)System.Net.HttpStatusCode.Accepted
            };
        }
        else
        {
            FlowHttpEngine httpEngine = engine.GetFlowHttpEngine();

            JToken triggerOutput = await httpEngine.GetOperationOutput(
                request: req,
                flowLogger: engine.Configuration.EventSource,
                excludedHeaders: new InsensitiveHashSet(),
                cancellationToken);

            return new HttpResponseMessageResult(
                await engine.Management.RunFlowPushTrigger(
                    request: req,
                    context: new FlowDataPlaneContext(flow),
                    trigger: trigger,
                    subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                    resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                    flowName: flow.FlowName,
                    triggerName: triggerName,
                    triggerOutput: triggerOutput,
                    clientCancellationToken: cancellationToken));
        }
    }

    public static FlowDefinition GetFlowDefinition(this WorkflowEngine engine, Flow flow)
    {
        return flow.ToDefinition(
            FlowConstants.GeneralAvailabilityApiVersion,
            integrationServiceEnvironmentRuntime: null,
            flowUriTemplateEngine: engine.UriTemplate,
            engine.GetEndpointConfigurationProvider().GetRegionalEndpoint(new FlowDataPlaneContext(flow)),
            shouldReturnAllProperties: true);
    }

    public static FlowVersionDefinition GetFlowVersionDefinition(this WorkflowEngine engine, Flow flow)
    {
        return flow.ToVersionDefinition(
            FlowConstants.GeneralAvailabilityApiVersion,
            integrationServiceEnvironmentRuntime: null);
    }

    public static FlowTriggerDefinition GetFlowTriggerDefinition(this WorkflowEngine engine, Flow flow, string triggerName)
    {
        return FlowTriggerDefinition.GetDefinition(
            flow: flow,
            trigger: flow.Definition.GetTrigger(triggerName),
            triggerJobs: engine.GetScaleUnitJobsProvider(flow.ScaleUnit, FlowExecutionClusterType.NotSpecified).GetFlowTriggerSplitOnJobs(flow.FlowId).Result,
            subscriptionId: FlowConfiguration.EdgeSubscriptionId,
            resourceGroup: FlowConfiguration.EdgeResourceGroupName,
            flowName: flow.FlowName,
            triggerName: triggerName,
            apiVersion: FlowConstants.GeneralAvailabilityApiVersion);
    }

    public static FlowRunDefinition GetFlowRunDefinition(this WorkflowEngine engine, Flow flow, FlowRun run, FlowRunAction[]? actions = null)
    {
        EndpointConfigurationProvider endpoint = engine.GetEndpointConfigurationProvider();
        FlowDataPlaneContext context = new(flow);

        return run.ToDefinition(
            endpoint: endpoint.GetRegionalEndpoint(context),
            flow: flow,
            flowAccessKey: emptyAccessKey,
            endpointsConfiguration: endpoint.GetFlowEndpointsConfiguration(context),
            subscriptionId: FlowConfiguration.EdgeSubscriptionId,
            resourceGroupName: FlowConfiguration.EdgeResourceGroupName,
            flowName: flow.FlowName,
            flowRunSequenceId: run.FlowRunSequenceId,
            apiVersion: FlowConstants.GeneralAvailabilityApiVersion,
            flowUriTemplateEngine: engine.UriTemplate,
            ipAddresses: "1.2.3.4",
            actions: actions);
    }

    public static FlowRunActionDefinition GetFlowRunActionDefinition(this WorkflowEngine engine, Flow flow, FlowRun run, FlowRunAction action)
    {
        EndpointConfigurationProvider endpoint = engine.GetEndpointConfigurationProvider();
        FlowDataPlaneContext context = new(flow);

        return action.ToDefinition(
            flow: flow,
            flowAccessKey: emptyAccessKey,
            endpoint: endpoint.GetRegionalEndpoint(context),
            subscriptionId: FlowConfiguration.EdgeSubscriptionId,
            resourceGroupName: FlowConfiguration.EdgeResourceGroupName,
            flowName: flow.FlowName,
            triggerName: run.TriggerName,
            inputs: engine.GetContentLink(flow, run.FlowRunSequenceId, action.InputsLink).Result,
            outputs: engine.GetContentLink(flow, run.FlowRunSequenceId, action.OutputsLink).Result,
            apiVersion: FlowConstants.GeneralAvailabilityApiVersion,
            ipAddresses: "1.2.3.4",
            flowUriTemplateEngine: engine.UriTemplate,
            isLinear: false,
            executionOrder: null);
    }

    private static readonly CachedFlowAccessKey emptyAccessKey = new()
    {
        SecondaryKey = string.Empty,
        PrimaryKey = string.Empty,
        FlowAccessKeyName = FlowConstants.DefaultFlowAccessKeyName,
        FlowId = string.Empty,
    };

    public static OptionsBuilder<WorkflowEngineOptions> AddWorkflowEngine(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<WorkflowEngineProvider>();
        services.TryAddSingleton<IHostedService, WorkflowEngineHostedService>();
        return services.AddOptions<WorkflowEngineOptions>().ValidateDataAnnotations().ValidateOnStart();
    }

    public static OptionsBuilder<WorkflowEngineOptions> WithAzureStorageAccountConnectionString(
        this OptionsBuilder<WorkflowEngineOptions> builder,
        string azureStorageAccountConnectionString)
    {
        return builder.Configure(options => options.AzureStorageAccountConnectionString = azureStorageAccountConnectionString);
    }
}
