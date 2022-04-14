using Microsoft.Azure.Workflows.Common.Extensions;
using Microsoft.Azure.Workflows.Common.Logging;
using Microsoft.Azure.Workflows.Data.CacheProviders;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Data.Providers;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;
using Microsoft.Azure.Workflows.Worker.Dispatcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.ResourceStack.Common.Services;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class WorkflowEngineProvider
    {
        private readonly EdgeFlowConfigurationSource _configurationSource;
        private readonly EdgeConnectionsDetails _details;
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly TaskCompletionSource<WorkflowEngine> _engineLazy;

        public WorkflowEngineProvider(
            IOptions<WorkflowEngineOptions> options,
            IHostEnvironment environment,
            ILogger<WorkflowEngine> logger)
        {
            _configurationSource =
                EdgeFlowConfigurationSource.CreateDefault(
                    options.Value.EndpointUri ?? new Uri("http://localhost"),
                    options.Value.AppDirectoryPath ?? environment.ContentRootPath);

            _configurationSource.SetAzureStorageAccountCredentials(options.Value.AzureStorageAccountConnectionString!);
            _details = options.Value.Connections ?? new() { ManagedApiConnections = new(), ServiceProviderConnections = new() };
            _logger = logger;
            _engineLazy = new TaskCompletionSource<WorkflowEngine>();
        }

        public Task<WorkflowEngine> CreateEngineAsync()
        {
            return CreateEngineAsync(_configurationSource, _details, _logger);
        }

        protected virtual async Task<WorkflowEngine> CreateEngineAsync(
            EdgeFlowConfigurationSource configuration,
            EdgeConnectionsDetails edgeConnectionsDetails,
            ILogger logger)
        {
            CloudConfigurationManager.Instance = configuration;
            typeof(FlowLog).GetProperty("Current")!.SetValue(null, new EdgeFlowLoggerEventSource(logger));
            EdgeFlowConfigurationV2 flowConfiguration = new(configuration, edgeConnectionsDetails)
            {
                FlowEdgeEnvironmentEndpointUri = configuration.EndpointUri,
                FlowEdgeEnvironmentAppDirectoryPath = configuration.AppDirectoryPath,
            };

            await flowConfiguration.Initialize();
            flowConfiguration.EnsureInitialized();

            EdgeConnectionCacheProvider connectionCache = ((EdgeFlowCacheProviders)flowConfiguration.CacheProviders).ConnectionCacheProvider;
            connectionCache.SetConnectionReferences(edgeConnectionsDetails.ManagedApiConnections);
            connectionCache.SetServiceProviderConnections(edgeConnectionsDetails.ServiceProviderConnections);

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

            return new WorkflowEngine(
                flowConfiguration,
                new EdgeFlowWebManagementEngine(flowConfiguration, httpConfiguration),
                new EdgeFlowJobsDispatcher(flowConfiguration, httpConfiguration),
                httpConfiguration);
        }

        internal void SetEngine(WorkflowEngine engine)
        {
            _engineLazy.SetResult(engine);
        }

        internal WorkflowEngine? GetInstanceOrCancel()
        {
            if (_engineLazy.Task.IsCompleted)
            {
                return _engineLazy.Task.Result;
            }
            else
            {
                _engineLazy.TrySetCanceled();
                return null;
            }
        }

        public Task<WorkflowEngine> GetEngineAsync()
        {
            return _engineLazy.Task;
        }

        private class EdgeFlowConfigurationV2 : EdgeFlowConfiguration
        {
            public EdgeFlowConfigurationV2(
                AzureConfigurationManager configurationManager,
                EdgeConnectionsDetails edgeConnectionsDetails)
                : base(configurationManager)
            {
                EdgeConnectionsDataProvider = new StaticEdgeConnectionsDataProvider(
                    this,
                    edgeConnectionsDetails);
            }
        }

        private class StaticEdgeConnectionsDataProvider : EdgeConnectionsDataProvider
        {
            private readonly EdgeConnectionsDetails _details;

            public StaticEdgeConnectionsDataProvider(
                FlowConfiguration flowConfiguration,
                EdgeConnectionsDetails edgeConnectionsDetails)
                : base(flowConfiguration)
            {
                _details = edgeConnectionsDetails;
            }

            public override Task<EdgeConnectionsDetails> GetEdgeConnectionDetails()
            {
                return Task.FromResult(_details);
            }
        }

        private class EdgeFlowLoggerEventSource : IEdgeFlowEventSource
        {
            private readonly ILogger _logger;

            public EdgeFlowLoggerEventSource(ILogger logger)
            {
                _logger = logger;
            }

            public void WorkflowDefinition(string createdTime, string changedTime, string subscriptionId, string resourceGroup, string workflowName, string workflowId, string state, string version, string scaleUnit, string sku, string plan, string location, int totalTriggers, int recurrentTriggers, int httpTriggers, int apiTriggers, int totalActions, int httpActions, int apiActions, int flowActions, int waitActions, string referencedHttpActionUris, string referencedApiActionHosts, int manualTriggers, int httpWebhookTriggers, int httpWebhookActions, int apiConnectionActions, int apiConnectionWebhookActions, int xmlValidationActions, int flatFileEncodingActions, int flatFileDecodingActions, int xsltActions, int scopeActions, int ifActions, int foreachActions, int untilActions, int responseActions, int composeActions, int queryActions, string integrationAccountResourceId, string integrationAccountId, string schemaVersion, int functionActions, int apiManagementActions, int integrationAccountArtifactLookupActions, int terminateActions, int nonEmptyElseBranches, int actionsL0, int actionsL1, int actionsL2, int actionsL3, int actionsL4, int runAfterSucceededCount, int runAfterSkippedCount, int runAfterFailedCount, string referencedApiNames, string usedPrimitiveTypes, int parallelIntentGroups, string tags, int parseJsonActions, int runAfterTimedOutCount, string correlationId)
            {
                _logger.LogInformation(
                    new EventId(200, nameof(WorkflowDefinition)),
                    "WorkflowDefinition. createdTime='{createdTime}', changedTime='{changedTime}', workflowName='{workflowName}', workflowId='{workflowId}', state='{state}', version='{version}', scaleUnit='{scaleUnit}', location='{callback}', totalTriggers='{totalTriggers}', recurrentTriggers='{recurrentTriggers}', httpTriggers='{httpTriggers}', apiTriggers='{apiTriggers}', totalActions='{totalActions}', httpActions='{httpActions}', apiActions='{apiActions}', flowActions='{flowActions}', waitActions='{waitActions}', referencedHttpActionUris='{referencedHttpActionUris}', referencedApiActionHosts='{referencedApiActionHosts}', manualTriggers='{manualTriggers}', httpWebhookTriggers='{httpWebhookTriggers}', httpWebhookActions='{httpWebhookActions}', apiConnectionActions='{apiConnectionActions}', apiConnectionWebhookActions='{apiConnectionWebhookActions}', xmlValidationActions='{xmlValidationActions}', flatFileEncodingActions='{flatFileEncodingActions}', flatFileDecodingActions='{flatFileDecodingActions}', xsltActions='{xsltActions}', scopeActions='{scopeActions}', ifActions='{ifActions}', foreachActions='{foreachActions}', untilActions='{untilActions}', responseActions='{responseActions}', composeActions='{composeActions}', queryActions='{queryActions}', integrationAccountResourceId='{integrationAccountResourceId}', integrationAccountId='{integrationAccountId}', schemaVersion='{schemaVersion}', functionActions='{functionActions}', apiManagementActions='{apiManagementActions}', integrationAccountArtifactLookupActions='{integrationAccountArtifactLookupActions}', terminateActions='{terminateActions}', nonEmptyElseBranches='{nonEmptyElseBranches}', actionsL0='{actionsL0}', actionsL1='{actionsL1}', actionsL2='{actionsL2}', actionsL3='{actionsL3}', actionsL4='{actionsL4}', runAfterSucceededCount='{runAfterSucceededCount}', runAfterSkippedCount='{runAfterSkippedCount}', runAfterFailedCount='{runAfterFailedCount}', referencedApiNames='{referencedApiNames}', usedPrimitiveTypes='{usedPrimitiveTypes}', parallelIntentGroups='{parallelIntentGroups}', tags='{tags}', parseJsonActions='{parseJsonActions}', runAfterTimedOutCount='{runAfterTimedOutCount}', correlationId='{correlationId}'.",
                    createdTime, changedTime, workflowName, workflowId, state, version, scaleUnit, location, totalTriggers, recurrentTriggers, httpTriggers, apiTriggers, totalActions, httpActions, apiActions, flowActions, waitActions, referencedHttpActionUris, referencedApiActionHosts, manualTriggers, httpWebhookTriggers, httpWebhookActions, apiConnectionActions, apiConnectionWebhookActions, xmlValidationActions, flatFileEncodingActions, flatFileDecodingActions, xsltActions, scopeActions, ifActions, foreachActions, untilActions, responseActions, composeActions, queryActions, integrationAccountResourceId, integrationAccountId, schemaVersion, functionActions, apiManagementActions, integrationAccountArtifactLookupActions, terminateActions, nonEmptyElseBranches, actionsL0, actionsL1, actionsL2, actionsL3, actionsL4, runAfterSucceededCount, runAfterSkippedCount, runAfterFailedCount, referencedApiNames, usedPrimitiveTypes, parallelIntentGroups, tags, parseJsonActions, runAfterTimedOutCount, correlationId);
            }

            public void WorkflowProperties(string createdTime, string changedTime, string subscriptionId, string resourceGroup, string workflowName, string workflowId, string state, string version, string scaleUnit, string sku, string plan, string location, string integrationAccountResourceId, string integrationAccountId, string schemaVersion, string tags, string workflowDefinition, string managedServiceIdentity, string integrationServiceEnvironmentResourceId, string integrationServiceEnvironmentId)
            {
                _logger.LogInformation(
                    new EventId(250, nameof(WorkflowProperties)),
                    "WorkflowProperties. createdTime='{createdTime}', changedTime='{changedTime}', workflowName='{workflowName}', workflowId='{workflowId}', state='{state}', version='{version}', scaleUnit='{scaleUnit}', location='{callback}', integrationAccountResourceId='{integrationAccountResourceId}', integrationAccountId='{integrationAccountId}', schemaVersion='{schemaVersion}', tags='{tags}', workflowDefinition='{workflowDefinition}', managedServiceIdentity='{managedServiceIdentity}', integrationServiceEnvironmentResourceId='{integrationServiceEnvironmentResourceId}', integrationServiceEnvironmentId='{integrationServiceEnvironmentId}'.",
                    createdTime, changedTime, workflowName, workflowId, state, version, scaleUnit, location, integrationAccountResourceId, integrationAccountId, schemaVersion, tags, workflowDefinition, managedServiceIdentity, integrationServiceEnvironmentResourceId, integrationServiceEnvironmentId);
            }

            public void WorkflowOperationProperties(string subscriptionId, string resourceGroup, string workflowName, string workflowId, string version, string operationCategory, string operationName, string operationSummary, string operationReferences)
            {
                _logger.LogInformation(
                    new EventId(251, nameof(WorkflowOperationProperties)),
                    "WorkflowOperationProperties. workflowName='{flowName}', workflowId='{workflowId}', version='{version}', operationCategory='{operationCategory}', operationName='{operationName}', operationSummary='{operationSummary}', operationReferences='{operationReferences}'.",
                    workflowName, workflowId, version, operationCategory, operationName, operationSummary, operationReferences);
            }

            public void WorkflowRunStart(string subscriptionId, string resourceGroup, string flowName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, string sku, string planId, string clientTrackingId, string properties, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string kind, string runtimeOperationOptions)
            {
                _logger.LogInformation(
                    new EventId(201, nameof(WorkflowRunStart)),
                    "Workflow run starts. flowName='{flowName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', clientTrackingId='{clientTrackingId}', properties='{properties}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', kind='{kind}', runtimeOperationOptions='{runtimeOperationOptions}'.",
                    flowName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, clientTrackingId, properties, sequencerType, flowLocation, flowScaleUnit, platformOptions, kind, runtimeOperationOptions);
            }

            public void WorkflowRunDispatched(string subscriptionId, string resourceGroup, string flowName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, string sku, string planId, string clientTrackingId, string properties, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string kind, string runtimeOperationOptions)
            {
                _logger.LogInformation(
                    new EventId(212, nameof(WorkflowRunDispatched)),
                    "Workflow run dispatched. flowName='{flowName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', clientTrackingId='{clientTrackingId}', properties='{properties}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', kind='{kind}', runtimeOperationOptions='{runtimeOperationOptions}'.",
                    flowName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, clientTrackingId, properties, sequencerType, flowLocation, flowScaleUnit, platformOptions, kind, runtimeOperationOptions);
            }

            public void WorkflowRunEnd(string subscriptionId, string resourceGroup, string flowName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, string sku, string planId, string clientTrackingId, string properties, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string kind, string runtimeOperationOptions)
            {
                _logger.LogInformation(
                    new EventId(202, nameof(WorkflowRunEnd)),
                    "Workflow run ends. flowName='{flowName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', clientTrackingId='{clientTrackingId}', properties='{properties}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', kind='{kind}', runtimeOperationOptions='{runtimeOperationOptions}'.",
                    flowName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, clientTrackingId, properties, sequencerType, flowLocation, flowScaleUnit, platformOptions, kind, runtimeOperationOptions);
            }

            public void WorkflowTriggerStart(string subscriptionId, string resourceGroup, string flowName, string triggerName, string flowId, string flowSequenceId, string status, string statusCode, string error, long durationInMilliseconds, string flowRunSequenceId, long inputsContentSize, long outputsContentSize, string sku, string planId, string clientTrackingId, string properties, string triggerType, string flowLocation, string flowScaleUnit, string subscriptionSku, string triggerKind, string sourceTriggerHistoryName)
            {
                _logger.LogInformation(
                    new EventId(203, nameof(WorkflowTriggerStart)),
                    "Workflow trigger starts. flowName='{flowName}', triggerName='{triggerName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', flowRunSequenceId='{flowRunSequenceId}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', clientTrackingId='{clientTrackingId}', properties='{properties}', triggerType='{triggerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', triggerKind='{triggerKind}' sourceTriggerHistoryName='{sourceTriggerHistoryName}'.",
                    flowName, triggerName, flowId, flowSequenceId, status, statusCode, error, durationInMilliseconds, flowRunSequenceId, inputsContentSize, outputsContentSize, clientTrackingId, properties, triggerType, flowLocation, flowScaleUnit, triggerKind, sourceTriggerHistoryName);
            }

            public void WorkflowTriggerEnd(string subscriptionId, string resourceGroup, string flowName, string triggerName, string flowId, string flowSequenceId, string status, string statusCode, string error, long durationInMilliseconds, string flowRunSequenceId, long inputsContentSize, long outputsContentSize, string sku, string planId, string clientTrackingId, string properties, string triggerType, string flowLocation, string flowScaleUnit, string subscriptionSku, string triggerKind, string sourceTriggerHistoryName)
            {
                _logger.LogInformation(
                    new EventId(204, nameof(WorkflowTriggerEnd)),
                    "Workflow trigger ends. flowName='{flowName}', triggerName='{triggerName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', flowRunSequenceId='{flowRunSequenceId}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', clientTrackingId='{clientTrackingId}', properties='{properties}', triggerType='{triggerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', triggerKind='{triggerKind}', sourceTriggerHistoryName='{sourceTriggerHistoryName}'.",
                    flowName, triggerName, flowId, flowSequenceId, status, statusCode, error, durationInMilliseconds, flowRunSequenceId, inputsContentSize, outputsContentSize, clientTrackingId, properties, triggerType, flowLocation, flowScaleUnit, triggerKind, sourceTriggerHistoryName);
            }

            public void WorkflowActionStart(string subscriptionId, string resourceGroup, string flowName, string actionName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, long inputsContentSize, long outputsContentSize, string sku, string planId, string actionTrackingId, string clientTrackingId, string properties, string actionType, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string retryHistory)
            {
                _logger.LogInformation(
                    new EventId(205, nameof(WorkflowActionStart)),
                    "Workflow action starts. flowName='{flowName}', actionName='{actionName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}',status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', actionTrackingId='{actionTrackingId}', clientTrackingId='{clientTrackingId}', properties='{properties}', actionType='{actionType}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', retryHistory='{retryHistory}'.",
                    flowName, actionName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, inputsContentSize, outputsContentSize, actionTrackingId, clientTrackingId, properties, actionType, sequencerType, flowLocation, flowScaleUnit, platformOptions, retryHistory);
            }

            public void WorkflowActionEnd(string subscriptionId, string resourceGroup, string flowName, string actionName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, long inputsContentSize, long outputsContentSize, string sku, string planId, string actionTrackingId, string clientTrackingId, string properties, string actionType, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string retryHistory)
            {
                _logger.LogInformation(
                    new EventId(206, nameof(WorkflowActionEnd)),
                    "Workflow action ends. flowName='{flowName}', actionName='{actionName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', actionTrackingId='{actionTrackingId}', clientTrackingId='{clientTrackingId}', properties='{properties}', actionType='{actionType}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', retryHistory='{retryHistory}'.",
                    flowName, actionName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, inputsContentSize, outputsContentSize, actionTrackingId, clientTrackingId, properties, actionType, sequencerType, flowLocation, flowScaleUnit, platformOptions, retryHistory);
            }

            public void WorkflowTriggerExecutionThrottled(string subscriptionId, string resourceGroup, string flowName, string triggerName, string flowId, string flowSequenceId, string status, string statusCode, string error, long durationInMilliseconds, string flowRunSequenceId, long inputsContentSize, long outputsContentSize, string sku, string planId, string clientTrackingId, string properties, string triggerType, string flowLocation, string flowScaleUnit, string subscriptionSku, string sourceTriggerHistoryName)
            {
                _logger.LogInformation(
                    new EventId(207, nameof(WorkflowTriggerExecutionThrottled)),
                    "Workflow trigger execution is throttled. flowName='{flowName}', triggerName='{triggerName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', flowRunSequenceId='{flowRunSequenceId}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', clientTrackingId='{clientTrackingId}', properties='{properties}', triggerType='{triggerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', sourceTriggerHistoryName='{sourceTriggerHistoryName}'.",
                    flowName, triggerName, flowId, flowSequenceId, status, statusCode, error, durationInMilliseconds, flowRunSequenceId, inputsContentSize, outputsContentSize, clientTrackingId, properties, triggerType, flowLocation, flowScaleUnit, sourceTriggerHistoryName);
            }

            public void WorkflowRunStartThrottled(string subscriptionId, string resourceGroup, string flowName, string triggerName, string flowId, string flowSequenceId, string flowRunSequenceId, string status, string statusCode, string sku, string planId, string clientTrackingId, string properties, string flowLocation, string flowScaleUnit, string subscriptionSku, string sourceTriggerHistoryName, string correlationId)
            {
                _logger.LogInformation(
                    new EventId(211, nameof(WorkflowRunStartThrottled)),
                    "Workflow run execution is throttled. flowName='{flowName}', triggerName='{triggerName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', status='{status}', statusCode='{statusCode}', clientTrackingId='{clientTrackingId}', properties='{properties}',runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', sourceTriggerHistoryName='{16}, , correlationId='{correlationId}''.",
                    flowName, triggerName, flowId, flowSequenceId, flowRunSequenceId, status, statusCode, clientTrackingId, properties, flowLocation, flowScaleUnit, sourceTriggerHistoryName, correlationId);
            }

            public void WorkflowActionExecutionThrottled(string subscriptionId, string resourceGroup, string flowName, string actionName, string flowId, string flowSequenceId, string flowRunSequenceId, string correlationId, string status, string statusCode, string error, long durationInMilliseconds, long inputsContentSize, long outputsContentSize, string sku, string planId, string actionTrackingId, string clientTrackingId, string properties, string actionType, string sequencerType, string flowLocation, string flowScaleUnit, string platformOptions, string subscriptionSku, string retryHistory)
            {
                _logger.LogInformation(
                    new EventId(208, nameof(WorkflowActionExecutionThrottled)),
                    "Workflow action execution is throttled. flowName='{flowName}', actionName='{actionName}', flowId='{flowId}', flowSequenceId='{flowSequenceId}', flowRunSequenceId='{flowRunSequenceId}', correlationId='{correlationId}', status='{status}', statusCode='{statusCode}', error='{error}', durationInMilliseconds='{durationInMilliseconds}', inputsContentSize='{inputsContentSize}', outputsContentSize='{outputsContentSize}', actionTrackingId='{actionTrackingId}', clientTrackingId='{clientTrackingId}', properties='{properties}', actionType='{actionType}', sequencerType='{sequencerType}', runtimeOperationOptions='{flowLocation}', flowScaleUnit='{flowScaleUnit}', platformOptions='{platformOptions}', retryHistory='{retryHistory}'.",
                    flowName, actionName, flowId, flowSequenceId, flowRunSequenceId, correlationId, status, statusCode, error, durationInMilliseconds, inputsContentSize, outputsContentSize, actionTrackingId, clientTrackingId, properties, actionType, sequencerType, flowLocation, flowScaleUnit, platformOptions, retryHistory);
            }

            public void WorkflowBatchMessageSend(string subscriptionId, string resourceGroup, string flowName, string actionName, string flowRunSequenceId, string status, string properties)
            {
                _logger.LogInformation(
                    new EventId(209, nameof(WorkflowBatchMessageSend)),
                    "Workflow batch message was sent. flowName='{flowName}', actionName='{actionName}', flowRunSequenceId='{flowRunSequenceId}', status='{status}', 'properties='{properties}'.",
                    flowName, actionName, flowRunSequenceId, status, properties);
            }

            public void WorkflowBatchMessageRelease(string subscriptionId, string resourceGroup, string flowName, string triggerName, string flowRunSequenceId, string status, string properties)
            {
                _logger.LogInformation(
                    new EventId(210, nameof(WorkflowBatchMessageRelease)),
                    "Workflow batch message was released. flowName='{flowName}', triggerName='{triggerName}', flowRunSequenceId='{flowRunSequenceId}', status='{status}', 'properties='{properties}'.",
                    flowName, triggerName, flowRunSequenceId, status, properties);
            }

            public void WorkflowBillingUsageEvent(string usageEventPartitionKey, string usageEventRowKey, string subscriptionId, string eventId, string eventDateTime, double quantity, string meterId, string resourceUri, string tags, string location, int bucket, string usageType)
            {
            }

            public void WorkflowBillingUsageReadyMessage(string partitionId, string batchId, string queueMessageTime)
            {
            }

            public void IntegrationAccountTrackingEvent(string subscriptionId, string resourceGroup, string integrationAccountName, string integrationAccountId, string apiVersion, string clientRequestId, string properties)
            {
            }

            public void IntegrationAccountDefinition(string createdTime, string changedTime, string subscriptionId, string resourceGroup, string integrationAccountName, string integrationAccountId, string scaleUnit, string sku, string location, int schemaArtifacts, int mapArtifacts, int certificateArtifacts, int partnerArtifacts, int agreementArtifacts, int as2Agreements, int x12Agreements, int edifactAgreements, string tags, int businessIdentitiesMaxCount, long metadataMaxSize, string integrationServiceEnvironmentResourceId, string integrationServiceEnvironmentId, string state, int rosettaNetProcessConfigurationArtifacts, int groupArtifacts, int batchConfigurationArtifacts, int scheduleArtifacts)
            {
            }

            public void IABillingUsageEvent(string usageEventPartitionKey, string usageEventRowKey, string integrationAccountId, string subscriptionId, string eventId, string eventDateTime, double quantity, string meterId, string resourceUri, string tags, string location)
            {
            }

            public void IABillingUsageReadyMessage(string partitionId, string batchId, string queueMessageTime)
            {
            }

            public void IntegrationServiceEnvironmentDefinition(string createdTime, string changedTime, string subscriptionId, string resourceGroup, string integrationServiceEnvironmentName, string integrationServiceEnvironmentId, string integrationServiceEnvironmentSequenceId, string scaleUnit, string skuName, string skuCapacity, string location, string tags, string state, string properties, string networkHealthState, string networkHealth, string provisioningState)
            {
            }

            public void IseBillingUsageEvent(string usageEventPartitionKey, string usageEventRowKey, string integrationServiceEnvironmentId, string subscriptionId, string eventId, string eventDateTime, double quantity, string meterId, string resourceUri, string tags, string location, string usageType)
            {
            }

            public void IseBillingUsageReadyMessage(string partitionId, string batchId, string queueMessageTime)
            {
            }

            public void IseVirtualNetworkSnapshot(string integrationServiceEnvironmentId, string vNetResourceId, string vNetAddressSpace, string subnetResourceId, string subnetDefinition, string vNetPeeringResourceId, string vNetPeeringDefinition)
            {
            }

            public void IseVirtualNetworkSubnetSnapshot(string integrationServiceEnvironmentId, string vNetResourceId, string vNetAddressSpace, string subnetResourceId, string subnetDefinition, string vNetPeeringResourceId, string vNetPeeringDefinition)
            {
            }

            public void IseVirtualNetworkPeeringSnapshot(string integrationServiceEnvironmentId, string vNetResourceId, string vNetAddressSpace, string subnetResourceId, string subnetDefinition, string vNetPeeringResourceId, string vNetPeeringDefinition)
            {
            }

            public void ResourceHealthClusterEvent(string region, string scaleUnit, string status)
            {
                _logger.LogInformation(
                    new EventId(500, nameof(ResourceHealthClusterEvent)),
                    "Resource health cluster event. region:'{0}', scaleUnit:'{1}', status:'{2}'.",
                    region, scaleUnit, status);
            }

            public void JobDebug(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string jobPartition, string jobId, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(50, nameof(JobDebug)),
                    "Job debug message: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, jobPartition, jobId, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobWarning(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string jobPartition, string jobId, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogWarning(
                    new EventId(51, nameof(JobWarning)),
                    "Job warning: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, jobPartition, jobId, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobError(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string jobPartition, string jobId, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(52, nameof(JobError)),
                    "Job error: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, jobPartition, jobId, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobCritical(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string jobPartition, string jobId, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogCritical(
                    new EventId(53, nameof(JobCritical)),
                    "Job critical error: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, jobPartition, jobId, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobOperation(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string jobPartition, string jobId, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(54, nameof(JobOperation)),
                    "Job operation: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, jobPartition, jobId, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobDispatchingError(string operationName, string jobPartition, string jobId, string message, string exception, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(57, nameof(JobDispatchingError)),
                    "Job dispatching error: operationName='{operationName}', jobPartition='{jobPartition}', jobId='{jobId}', message='{message}', exception='{exception}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    operationName, jobPartition, jobId, message, exception, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void JobHistory(string jobPartition, string jobId, string callback, string startTime, string endTime, string executionTimeInMilliseconds, string executionDelayInMilliseconds, string executionIntervalInMilliseconds, string executionStatus, string executionMessage, string executionDetails, string nextExecutionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string dequeueCount, string advanceVersion, string triggerId, string messageId, string state, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties, string jobDurabilityLevel)
            {
                _logger.LogInformation(
                    new EventId(55, nameof(JobHistory)),
                    "Job history: jobPartition='{jobPartition}', jobId='{jobId}', callback='{callback}', startTime='{startTime}', endTime='{endTime}', executionTimeInMilliseconds='{executionTimeInMilliseconds}', executionDelayInMilliseconds='{executionDelayInMilliseconds}', executionIntervalInMilliseconds='{executionIntervalInMilliseconds}', executionStatus='{executionStatus}', executionMessage='{executionMessage}', executionDetails='{executionDetails}', nextExecutionTime='{nextExecutionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', dequeueCount='{dequeueCount}', advanceVersion='{advanceVersion}', triggerId='{triggerId}', messageId='{messageId}', state='{state}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', jobDurabilityLevel='{jobDurabilityLevel}'.",
                    jobPartition, jobId, callback, startTime, endTime, executionTimeInMilliseconds, executionDelayInMilliseconds, executionIntervalInMilliseconds, executionStatus, executionMessage, executionDetails, nextExecutionTime, correlationId, principalOid, principalPuid, dequeueCount, advanceVersion, triggerId, messageId, state, organizationId, activityVector, realPuid, altSecId, additionalProperties, jobDurabilityLevel);
            }

            public void JobDefinition(string jobPartition, string jobId, string version, string callback, string location, string locationsAffinity, string flags, string state, string executionState, string startTime, string endTime, int repeatCount, long repeatInterval, string repeatUnit, string repeatSchedule, int currentRepeatCount, int retryCount, long retryInterval, string retryUnit, int currentRetryCount, int currentExecutionCount, string timeout, string retention, string nextExecutionTime, string lastExecutionTime, string lastExecutionStatus, string createdTime, string changedTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, int totalSucceededCount, int totalCompletedCount, int totalFailedCount, int totalFaultedCount, int totalPostponedCount, string parentJobCompletionTrigger, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(56, nameof(JobDefinition)),
                    "Job definition: jobPartition='{jobPartition}', jobId='{jobId}', version='{version}', callback='{callback}', location='{callback}', locationsAffinity='{locationsAffinity}', flags='{flags}', state='{state}', executionState='{executionState}', startTime='{startTime}', endTime='{endTime}', repeatCount='{repeatCount}', repeatInterval='{repeatInterval}', repeatUnit='{repeatUnit}', repeatSchedule='{repeatSchedule}', currentRepeatCount='{currentRepeatCount}', retryCount='{retryCount}', retryInterval='{retryInterval}', retryUnit='{retryUnit}', currentRetryCount='{currentRetryCount}', currentExecutionCount='{currentExecutionCount}', timeout='{timeout}', retention='{retention}', nextExecutionTime='{nextExecutionTime}', lastExecutionTime='{lastExecutionTime}', lastExecutionStatus='{lastExecutionStatus}', createdTime='{createdTime}', changedTime='{changedTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', totalSucceededCount='{totalSucceededCount}', totalCompletedCount='{totalCompletedCount}', totalFailedCount='{totalFailedCount}', totalFaultedCount='{totalFaultedCount}', totalPostponedCount='{totalPostponedCount}', parentJobCompletionTrigger='{parentJobCompletionTrigger}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    jobPartition, jobId, version, callback, location, locationsAffinity, flags, state, executionState, startTime, endTime, repeatCount, repeatInterval, repeatUnit, repeatSchedule, currentRepeatCount, retryCount, retryInterval, retryUnit, currentRetryCount, currentExecutionCount, timeout, retention, nextExecutionTime, lastExecutionTime, lastExecutionStatus, createdTime, changedTime, correlationId, principalOid, principalPuid, totalSucceededCount, totalCompletedCount, totalFailedCount, totalFaultedCount, totalPostponedCount, parentJobCompletionTrigger, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void StorageRequestStart(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, string resourceType, string resourceName, string clientRequestId, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(38, nameof(StorageRequestStart)),
                    "Storage request starts: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', accountName='{accountName}', resourceType='{resourceType}', resourceName='{resourceName}', clientRequestId='{clientRequestId}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, accountName, resourceType, resourceName, clientRequestId, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void StorageRequestEndWithSuccess(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, string resourceType, string resourceName, string clientRequestId, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(39, nameof(StorageRequestEndWithSuccess)),
                    "Storage request succeeded: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', accountName='{accountName}', resourceType='{resourceType}', resourceName='{resourceName}', clientRequestId='{clientRequestId}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, accountName, resourceType, resourceName, clientRequestId, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void StorageRequestEndWithServerFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, string resourceType, string resourceName, string clientRequestId, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(40, nameof(StorageRequestEndWithServerFailure)),
                    "Storage request ends with server failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', accountName='{accountName}', resourceType='{resourceType}', resourceName='{resourceName}', clientRequestId='{clientRequestId}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, accountName, resourceType, resourceName, clientRequestId, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void StorageRequestEndWithClientFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, string resourceType, string resourceName, string clientRequestId, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(41, nameof(StorageRequestEndWithClientFailure)),
                    "Storage request ends with client failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', accountName='{accountName}', resourceType='{resourceType}', resourceName='{resourceName}', clientRequestId='{clientRequestId}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, accountName, resourceType, resourceName, clientRequestId, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void StorageOperation(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, string resourceType, string resourceName, string clientRequestId, string operationStatus, long durationInMilliseconds, string exceptionMessage, int requestsStarted, int requestsCompleted, int requestsTimedout, string requestsDetails, string organizationId, string activityVector, long ingressBytes, long egressBytes, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(42, nameof(StorageOperation)),
                    "Storage operation completed: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', accountName='{accountName}', resourceType='{resourceType}', resourceName='{resourceName}', clientRequestId='{clientRequestId}', operationStatus='{operationStatus}', durationInMilliseconds='{durationInMilliseconds}', exceptionMessage='{exceptionMessage}', requestsStarted='{requestsStarted}', requestsCompleted='{requestsCompleted}', requestsTimedout='{requestsTimedout}', requestsDetails='{requestsDetails}', organizationId='{organizationId}', activityVector='{activityVector}', ingressBytes='{ingressBytes}', egressBytes='{egressBytes}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, accountName, resourceType, resourceName, clientRequestId, operationStatus, durationInMilliseconds, exceptionMessage, requestsStarted, requestsCompleted, requestsTimedout, requestsDetails, organizationId, activityVector, ingressBytes, egressBytes, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherDebug(string dispatcherName, string operationName, string message, string queueMessage, string exception, string errorCode, int dequeueCount, string insertionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(60, nameof(DispatcherDebug)),
                    "Dispatcher debug message: dispatcherName='{dispatcherName}', operationName='{operationName}', message='{message}', queueMessage='{queueMessage}', exception='{exception}', errorCode='{errorCode}', dequeueCount='{dequeueCount}', insertionTime='{insertionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, operationName, message, queueMessage, exception, errorCode, dequeueCount, insertionTime, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherWarning(string dispatcherName, string operationName, string message, string queueMessage, string exception, string errorCode, int dequeueCount, string insertionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogWarning(
                    new EventId(61, nameof(DispatcherWarning)),
                    "Dispatcher warning: dispatcherName='{dispatcherName}', operationName='{operationName}', message='{message}', queueMessage='{queueMessage}', exception='{exception}', errorCode='{errorCode}', dequeueCount='{dequeueCount}', insertionTime='{insertionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, operationName, message, queueMessage, exception, errorCode, dequeueCount, insertionTime, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherError(string dispatcherName, string operationName, string message, string queueMessage, string exception, string errorCode, int dequeueCount, string insertionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(62, nameof(DispatcherError)),
                    "Dispatcher error: dispatcherName='{dispatcherName}', operationName='{operationName}', message='{message}', queueMessage='{queueMessage}', exception='{exception}', errorCode='{errorCode}', dequeueCount='{dequeueCount}', insertionTime='{insertionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, operationName, message, queueMessage, exception, errorCode, dequeueCount, insertionTime, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherCritical(string dispatcherName, string operationName, string message, string queueMessage, string exception, string errorCode, int dequeueCount, string insertionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogCritical(
                    new EventId(63, nameof(DispatcherCritical)),
                    "Dispatcher critical error: dispatcherName='{dispatcherName}', operationName='{operationName}', message='{message}', queueMessage='{queueMessage}', exception='{exception}', errorCode='{errorCode}', dequeueCount='{dequeueCount}', insertionTime='{insertionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, operationName, message, queueMessage, exception, errorCode, dequeueCount, insertionTime, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherOperation(string dispatcherName, string operationName, string message, string queueMessage, string exception, string errorCode, int dequeueCount, string insertionTime, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(64, nameof(DispatcherOperation)),
                    "Dispatcher operation: dispatcherName='{dispatcherName}', operationName='{operationName}', message='{message}', queueMessage='{queueMessage}', exception='{exception}', errorCode='{errorCode}', dequeueCount='{dequeueCount}', insertionTime='{insertionTime}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, operationName, message, queueMessage, exception, errorCode, dequeueCount, insertionTime, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void DispatcherQueueDepth(string dispatcherName, string storageAccount, string queue, double depth, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(65, nameof(DispatcherQueueDepth)),
                    "Dispatcher queue depth: dispatcherName='{dispatcherName}', storageAccount='{storageAccount}', queue='{queue}', depth='{depth}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    dispatcherName, storageAccount, queue, depth, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void RedisOperationStart(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, long durationInMilliseconds, string exceptionMessage, string organizationId, string activityVector, string realPuid, string altSecId, string databaseId, string cacheKey, string additionalProperties)
            {
            }

            public void RedisOperationEndWithSuccess(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, long durationInMilliseconds, string exceptionMessage, string organizationId, string activityVector, string realPuid, string altSecId, string databaseId, string cacheKey, string additionalProperties)
            {
            }

            public void RedisOperationEndWithFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string accountName, long durationInMilliseconds, string exceptionMessage, string organizationId, string activityVector, string realPuid, string altSecId, string databaseId, string cacheKey, string additionalProperties)
            {
            }

            public void ServiceStarting(string serviceName, string name, string value, string version, string message, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(10, nameof(ServiceStarting)),
                    "Service starting: serviceName='{serviceName}', name='{name}', value='{value}', version='{version}', message='{message}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    serviceName, name, value, version, message, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void ServiceStarted(string serviceName, string version, string name, string value, string message, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(11, nameof(ServiceStarted)),
                    "Service started: serviceName='{serviceName}', name='{name}', value='{value}', version='{version}', message='{message}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    serviceName, version, name, value, message, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void ServiceStopping(string serviceName, string version, string name, string value, string message, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(12, nameof(ServiceStopping)),
                    "Service stopping: serviceName='{serviceName}', name='{name}', value='{value}', version='{version}', message='{message}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    serviceName, version, name, value, message, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void ServiceStopped(string serviceName, string version, string name, string value, string message, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(13, nameof(ServiceStopped)),
                    "Service stopped: serviceName='{serviceName}', name='{name}', value='{value}', version='{version}', message='{message}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    serviceName, version, name, value, message, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void ServiceConfiguration(string serviceName, string version, string name, string value, string message, string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogInformation(
                    new EventId(14, nameof(ServiceConfiguration)),
                    "Service configuration: serviceName='{serviceName}', name='{name}', value='{value}', version='{version}', message='{message}', correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    serviceName, version, name, value, message, correlationId, principalOid, principalPuid, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void Debug(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogDebug(
                    new EventId(20, nameof(Debug)),
                    "Debug message: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void Warning(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogWarning(
                    new EventId(21, nameof(Warning)),
                    "Warning message: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void Error(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogError(
                    new EventId(22, nameof(Error)),
                    "Error message: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void Critical(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string message, string exception, string organizationId, string activityVector, string realPuid, string altSecId, string additionalProperties)
            {
                _logger.LogCritical(
                    new EventId(23, nameof(Critical)),
                    "Critical error: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', message='{message}', exception='{exception}', organizationId='{organizationId}', activityVector='{activityVector}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}'.",
                    correlationId, principalOid, principalPuid, operationName, message, exception, organizationId, activityVector, realPuid, altSecId, additionalProperties);
            }

            public void HttpIncomingRequestStart(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string authorizationSource, string authorizationAction, string operationName, string httpMethod, string hostName, string targetUri, string userAgent, string clientRequestId, string clientSessionId, string clientIpAddress, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string failureCause, string errorMessage, string referer, string commandName, string parameterSetName, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(30, nameof(HttpIncomingRequestStart)),
                    "Incoming HTTP request starts: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', authorizationSource='{authorizationSource}', authorizationAction='{authorizationAction}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', userAgent='{userAgent}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientIpAddress='{clientIpAddress}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', failureCause='{failureCause}', errorMessage='{errorMessage}', referer='{referer}', commandName='{commandName}', parameterSetName='{parameterSetName}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, authorizationSource, authorizationAction, operationName, httpMethod, hostName, targetUri, userAgent, clientRequestId, clientSessionId, clientIpAddress, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, failureCause, errorMessage, referer, commandName, parameterSetName, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpIncomingRequestEndWithSuccess(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string authorizationSource, string authorizationAction, string operationName, string httpMethod, string hostName, string targetUri, string userAgent, string clientRequestId, string clientSessionId, string clientIpAddress, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string failureCause, string errorMessage, string referer, string commandName, string parameterSetName, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(31, nameof(HttpIncomingRequestEndWithSuccess)),
                    "Incoming HTTP request succeeded: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', authorizationSource='{authorizationSource}', authorizationAction='{authorizationAction}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', userAgent='{userAgent}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientIpAddress='{clientIpAddress}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', failureCause='{failureCause}', errorMessage='{errorMessage}', referer='{referer}', commandName='{commandName}', parameterSetName='{parameterSetName}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, authorizationSource, authorizationAction, operationName, httpMethod, hostName, targetUri, userAgent, clientRequestId, clientSessionId, clientIpAddress, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, failureCause, errorMessage, referer, commandName, parameterSetName, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpIncomingRequestEndWithServerFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string authorizationSource, string authorizationAction, string operationName, string httpMethod, string hostName, string targetUri, string userAgent, string clientRequestId, string clientSessionId, string clientIpAddress, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string failureCause, string errorMessage, string referer, string commandName, string parameterSetName, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogError(
                    new EventId(32, nameof(HttpIncomingRequestEndWithServerFailure)),
                    "Incoming HTTP request ends with server failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', authorizationSource='{authorizationSource}', authorizationAction='{authorizationAction}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', userAgent='{userAgent}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientIpAddress='{clientIpAddress}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', failureCause='{failureCause}', errorMessage='{errorMessage}', referer='{referer}', commandName='{commandName}', parameterSetName='{parameterSetName}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, authorizationSource, authorizationAction, operationName, httpMethod, hostName, targetUri, userAgent, clientRequestId, clientSessionId, clientIpAddress, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, failureCause, errorMessage, referer, commandName, parameterSetName, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpIncomingRequestEndWithClientFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string authorizationSource, string authorizationAction, string operationName, string httpMethod, string hostName, string targetUri, string userAgent, string clientRequestId, string clientSessionId, string clientIpAddress, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string failureCause, string errorMessage, string referer, string commandName, string parameterSetName, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(33, nameof(HttpIncomingRequestEndWithClientFailure)),
                    "Incoming HTTP request ends with client failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', authorizationSource='{authorizationSource}', authorizationAction='{authorizationAction}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', userAgent='{userAgent}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientIpAddress='{clientIpAddress}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', failureCause='{failureCause}', errorMessage='{errorMessage}', referer='{referer}', commandName='{commandName}', parameterSetName='{parameterSetName}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, authorizationSource, authorizationAction, operationName, httpMethod, hostName, targetUri, userAgent, clientRequestId, clientSessionId, clientIpAddress, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, failureCause, errorMessage, referer, commandName, parameterSetName, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpOutgoingRequestStart(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string httpMethod, string hostName, string targetUri, string clientRequestId, string clientSessionId, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string referer, string failureCause, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(34, nameof(HttpOutgoingRequestStart)),
                    "Outgoing HTTP request starts: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', referer='{referer}', failureCause='{failureCause}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, operationName, httpMethod, hostName, targetUri, clientRequestId, clientSessionId, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, referer, failureCause, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpOutgoingRequestEndWithSuccess(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string httpMethod, string hostName, string targetUri, string clientRequestId, string clientSessionId, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string referer, string failureCause, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(35, nameof(HttpOutgoingRequestEndWithSuccess)),
                    "Outgoing HTTP request succeeded: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', referer='{referer}', failureCause='{failureCause}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, operationName, httpMethod, hostName, targetUri, clientRequestId, clientSessionId, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, referer, failureCause, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpOutgoingRequestEndWithServerFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string httpMethod, string hostName, string targetUri, string clientRequestId, string clientSessionId, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string referer, string failureCause, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogWarning(
                    new EventId(36, nameof(HttpOutgoingRequestEndWithServerFailure)),
                    "Outgoing HTTP request ends with server failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', referer='{referer}', failureCause='{failureCause}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, operationName, httpMethod, hostName, targetUri, clientRequestId, clientSessionId, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, referer, failureCause, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void HttpOutgoingRequestEndWithClientFailure(string subscriptionId, string correlationId, string principalOid, string principalPuid, string tenantId, string operationName, string httpMethod, string hostName, string targetUri, string clientRequestId, string clientSessionId, string clientApplicationId, string apiVersion, long contentLength, string serviceRequestId, long durationInMilliseconds, int httpStatusCode, string exceptionMessage, string errorCode, string errorMessage, string referer, string failureCause, string contentType, string contentEncoding, string armServiceRequestId, string organizationId, string activityVector, string locale, string realPuid, string altSecId, string additionalProperties, string targetResourceProvider, string targetResourceType)
            {
                _logger.LogInformation(
                    new EventId(37, nameof(HttpOutgoingRequestEndWithClientFailure)),
                    "Outgoing HTTP request ends with client failure: correlationId='{correlationId}', principalOid='{principalOid}', principalPuid='{principalPuid}', operationName='{operationName}', httpMethod='{httpMethod}', hostName='{hostName}', targetUri='{targetUri}', clientRequestId='{clientRequestId}', clientSessionId='{clientSessionId}', clientApplicationId='{clientApplicationId}', apiVersion='{apiVersion}', contentLength='{contentLength}', serviceRequestId='{serviceRequestId}', durationInMilliseconds='{durationInMilliseconds}', httpStatusCode='{httpStatusCode}', exceptionMessage='{exceptionMessage}', errorCode='{errorCode}', errorMessage='{errorMessage}', referer='{referer}', failureCause='{failureCause}', contentType='{contentType}', contentEncoding='{contentEncoding}', armServiceRequestId='{armServiceRequestId}', organizationId='{organizationId}', activityVector='{activityVector}', locale='{locale}', realPuid='{realPuid}', altSecId='{altSecId}', additionalProperties='{additionalProperties}', targetResourceProvider='{targetResourceProvider}', targetResourceType='{targetResourceType}'.",
                    correlationId, principalOid, principalPuid, operationName, httpMethod, hostName, targetUri, clientRequestId, clientSessionId, clientApplicationId, apiVersion, contentLength, serviceRequestId, durationInMilliseconds, httpStatusCode, exceptionMessage, errorCode, errorMessage, referer, failureCause, contentType, contentEncoding, armServiceRequestId, organizationId, activityVector, locale, realPuid, altSecId, additionalProperties, targetResourceProvider, targetResourceType);
            }

            public void EdgeError(string operationName, string message, string exception)
            {
                _logger.LogError(
                    new EventId(801, nameof(EdgeError)),
                    "Workflow Error: operationName='{operationName}', message='{message}', exception='{exception}'.",
                    operationName, message, exception);
            }

            public void EdgeWarning(string operationName, string message, string exception)
            {
                _logger.LogWarning(
                    new EventId(802, nameof(EdgeWarning)),
                    "Workflow Warning: operationName='{operationName}', message='{message}', exception='{exception}'.",
                    operationName, message, exception);
            }

            public void EdgeInfo(string operationName, string message, string exception)
            {
                _logger.LogInformation(
                    new EventId(803, nameof(EdgeInfo)),
                    "Workflow Info: operationName='{operationName}', message='{message}', exception='{exception}'.",
                    operationName, message, exception);
            }

            public void EdgeDebug(string operationName, string message, string exception)
            {
                _logger.LogDebug(
                    new EventId(804, nameof(EdgeDebug)),
                    "Workflow Debug: operationName='{operationName}', message='{message}', exception='{exception}'.",
                    operationName, message, exception);
            }

            public void EdgeTrace(string operationName, string message, string exception)
            {
                _logger.LogTrace(
                    new EventId(805, nameof(EdgeTrace)),
                    "Workflow Trace: operationName='{operationName}', message='{message}', exception='{exception}'.",
                    operationName, message, exception);
            }
        }
    }
}
