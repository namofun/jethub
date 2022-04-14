using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.Azure.Workflows.Common.Entities;
using Microsoft.Azure.Workflows.Data;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Extensions;
using Microsoft.Azure.Workflows.Templates.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using Microsoft.WindowsAzure.ResourceStack.Common.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xylab.Workflows.LogicApps.Mvc;

namespace Xylab.Workflows.LogicApps.Engine
{
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

        public static Task<FlowRun> FindFlowRunBySequenceId(this WorkflowEngine engine, Flow flow, string sequenceId)
        {
            return engine.GetScaleUnitDataProvider(flow.ScaleUnit)
                .FindFlowRun(
                    flowId: flow.FlowId,
                    flowRunSequenceId: sequenceId);
        }

        public static Task<JToken> GetContentLink(this WorkflowEngine engine, Flow flow, string flowContentSequenceId, ContentLink contentLink)
        {
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

        public static async Task<IActionResult> InvokeFlowTrigger(
            this WorkflowEngine engine,
            Flow flow,
            string triggerName,
            FlowTemplateTrigger trigger,
            HttpRequestMessage req,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            using (RequestCorrelationContext.Current.Initialize(
                req,
                apiVersion: FlowConstants.PrivatePreview20190601ApiVersion,
                localizationLanguage: "en-us"))
            {
                RequestIdentity clientRequestIdentity = new()
                {
                    Claims = user.Claims.ToDictionary(k => k.Type, v => v.Value),
                    IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                };

                clientRequestIdentity.AuthorizeRequest(RequestAuthorizationSource.Direct);
                RequestCorrelationContext.Current.SetAuthenticationIdentity(clientRequestIdentity);

                if (trigger.IsFlowRecurrentTrigger() || trigger.IsNotificationTrigger())
                {
                    await engine.Management.RunFlowRecurrentTrigger(flow, flow.FlowName, triggerName);

                    return new NewtonsoftJsonResult(
                        await engine.GetScaleUnitJobsProvider(flow.ScaleUnit)
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
        }

        public static FlowDefinition GetFlowDefinition(this WorkflowEngine engine, Flow flow)
        {
            return flow.ToDefinition(
                engine.GetEndpointConfigurationProvider().GetRegionalEndpoint(new FlowDataPlaneContext(flow)),
                FlowConstants.GeneralAvailabilityApiVersion,
                integrationServiceEnvironmentRuntime: null,
                shouldReturnProperties: true);
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
                triggerJobs: engine.GetScaleUnitJobsProvider(flow.ScaleUnit).GetFlowTriggerSplitOnJobs(flow.FlowId).Result,
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                flowName: flow.FlowName,
                triggerName: triggerName,
                apiVersion: FlowConstants.GeneralAvailabilityApiVersion);
        }

        public static FlowRunDefinition GetFlowRunDefinition(this WorkflowEngine engine, Flow flow, FlowRun run)
        {
            EndpointConfigurationProvider endpoint = engine.GetEndpointConfigurationProvider();
            FlowDataPlaneContext context = new(flow);

            CachedFlowAccessKey accessKey = engine.GetScaleUnitCacheProvider(context, flow.ScaleUnit)
                .FindFlowAccessKey(
                    flowId: flow.FlowId,
                    accessKeyName: FlowConstants.DefaultFlowAccessKeyName)
                .Result;

            return run.ToDefinition(
                endpoint: endpoint.GetRegionalEndpoint(context),
                flow: flow,
                flowAccessKey: accessKey,
                endpointsConfiguration: endpoint.GetFlowEndpointsConfiguration(context),
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                resourceGroupName: FlowConfiguration.EdgeResourceGroupName,
                flowName: flow.FlowName,
                flowRunSequenceId: run.FlowRunSequenceId,
                apiVersion: FlowConstants.GeneralAvailabilityApiVersion);
        }

        public static IServiceCollection AddWorkflowEngine(this IServiceCollection services, Action<WorkflowEngineOptions> configureOptions)
        {
            services.TryAddSingleton<WorkflowEngineProvider>();
            services.TryAddSingleton<IHostedService, WorkflowEngineHostedService>();
            services.Configure(configureOptions);
            return services;
        }
    }
}
