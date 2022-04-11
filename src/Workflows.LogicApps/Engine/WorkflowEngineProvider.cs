using Microsoft.Azure.Workflows.Common.Extensions;
using Microsoft.Azure.Workflows.Data.CacheProviders;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Data.Providers;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;
using Microsoft.Azure.Workflows.Worker.Dispatcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.ResourceStack.Common.Services;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class WorkflowEngineProvider
    {
        private readonly Task<WorkflowEngine> _engineTask;

        public WorkflowEngineProvider(
            IOptions<WorkflowEngineOptions> options,
            IHostEnvironment environment)
        {
            EdgeFlowConfigurationSource configurationSource =
                EdgeFlowConfigurationSource.CreateDefault(
                    options.Value.EndpointUri ?? new Uri("http://localhost"),
                    options.Value.AppDirectoryPath ?? environment.ContentRootPath);

            configurationSource.SetAzureStorageAccountCredentials(options.Value.AzureStorageAccountConnectionString!);
            EdgeConnectionsDetails details = options.Value.Connections ?? new() { ManagedApiConnections = new(), ServiceProviderConnections = new() };

            _engineTask = CreateEngineAsync(configurationSource, details);
        }

        protected virtual async Task<WorkflowEngine> CreateEngineAsync(
            EdgeFlowConfigurationSource configuration,
            EdgeConnectionsDetails edgeConnectionsDetails)
        {
            CloudConfigurationManager.Instance = configuration;
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

            EdgeFlowJobsDispatcher dispatcher = new(flowConfiguration, httpConfiguration);
            dispatcher.Start();
            dispatcher.ProvisionSystemJobs();

            return new WorkflowEngine(
                flowConfiguration,
                new EdgeFlowWebManagementEngine(flowConfiguration, httpConfiguration),
                dispatcher);
        }

        public Task<WorkflowEngine> GetEngineAsync()
        {
            return _engineTask;
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
    }
}
