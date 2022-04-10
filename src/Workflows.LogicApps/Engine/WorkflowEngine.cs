using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.Azure.Workflows.Common.Extensions;
using Microsoft.Azure.Workflows.Data;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Extensions;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;
using Microsoft.Azure.Workflows.Worker.Dispatcher;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using Microsoft.WindowsAzure.ResourceStack.Common.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Xylab.Workflows.LogicApps.Engine
{
    public class WorkflowEngine
    {
        public EdgeFlowConfiguration Configuration { get; }

        public EdgeFlowWebManagementEngine Engine { get; }

        public Dictionary<string, FlowDefinition> FlowDefinitions { get; }

        public WorkflowEngine(EdgeFlowConfiguration configuration, EdgeFlowWebManagementEngine engine)
        {
            Configuration = configuration;
            Engine = engine;
            FlowDefinitions = new();
        }

        public Task<Flow> FindFlowByName(string flowName)
        {
            return Engine.GetRegionalDataProvider()
                .FindFlowByName(
                    subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                    resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                    flowName: flowName);
        }

        public async Task ValidateAndCreateFlow(string name, FlowPropertiesDefinition definition)
        {
            FlowDefinition def = new(FlowConstants.GeneralAvailabilitySchemaVersion)
            {
                Properties = definition,
                FullyQualifiedName = name,
            };

            await Engine.ValidateAndCreateFlow(name, def.Properties);
            FlowDefinitions.Add(name, def);
        }

        public static async Task<WorkflowEngine> CreateEngine(EdgeFlowConfigurationSource configuration)
        {
            CloudConfigurationManager.Instance = configuration;
            EdgeFlowConfiguration flowConfiguration = new(configuration) { FlowEdgeEnvironmentEndpointUri = new Uri("http://localhost") };
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

            EdgeFlowJobsDispatcher dispatcher = new(flowConfiguration, httpConfiguration);
            FlowJobsCallbackFactory callbackFactory = new(flowConfiguration, httpConfiguration, requestPipeline: null);
            flowConfiguration.InitializeFlowJobCallbackConfiguration(callbackFactory);

            dispatcher.Start();
            dispatcher.ProvisionSystemJobs();

            return new WorkflowEngine(
                flowConfiguration,
                new EdgeFlowWebManagementEngine(flowConfiguration, httpConfiguration));
        }

        public async Task<HttpResponseMessage> InvokeFlow(FlowDefinition workflow, HttpRequestMessage req, ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default)
        {
            using (RequestCorrelationContext.Current.Initialize(apiVersion: FlowConstants.PrivatePreview20190601ApiVersion, localizationLanguage: "en-us"))
            {
                RequestIdentity clientRequestIdentity = new()
                {
                    Claims = claimsPrincipal.Claims.ToDictionary(k => k.Type, v => v.Value),
                    IsAuthenticated = true,
                };

                clientRequestIdentity.AuthorizeRequest(RequestAuthorizationSource.Direct);
                RequestCorrelationContext.Current.SetAuthenticationIdentity(clientRequestIdentity);

                Flow flow = await FindFlowByName(workflow.Name);
                string triggerName = flow.Definition.Triggers.Keys.Single();
                var trigger = flow.Definition.GetTrigger(triggerName);

                if (trigger.IsFlowRecurrentTrigger() || trigger.IsNotificationTrigger())
                {
                    await Engine.RunFlowRecurrentTrigger(flow, flow.FlowName, triggerName);
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Accepted,
                        Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                        RequestMessage = req,
                    };
                }
                else
                {
                    FlowHttpEngine httpEngine = Engine.GetFlowHttpEngine();

                    JToken triggerOutput = await httpEngine.GetOperationOutput(
                        request: req,
                        flowLogger: Configuration.EventSource,
                        cancellationToken);

                    return await Engine.RunFlowPushTrigger(
                        request: req,
                        context: new FlowDataPlaneContext(flow),
                        trigger: trigger,
                        subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                        resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                        flowName: flow.FlowName,
                        triggerName: triggerName,
                        triggerOutput: triggerOutput,
                        clientCancellationToken: cancellationToken);
                }
            }
        }
    }
}
