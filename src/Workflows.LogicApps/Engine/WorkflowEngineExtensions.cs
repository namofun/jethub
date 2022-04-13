using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Engine
{
    public static class WorkflowEngineExtensions
    {
        public static Task<Flow> FindFlowByIdentifier(this WorkflowEngine engine, string identifier)
        {
            return engine.Management.GetRegionalDataProvider().FindFlowByIdentifier(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                flowId: identifier);
        }

        public static Task<Flow> FindFlowByName(this WorkflowEngine engine, string flowName)
        {
            return engine.Management.GetRegionalDataProvider().FindFlowByName(
                subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                flowName: flowName);
        }

        public static async Task<FlowDefinition> ValidateAndCreateFlow(this WorkflowEngine engine, string name, FlowPropertiesDefinition definition)
        {
            FlowDefinition def = new(FlowConstants.GeneralAvailabilitySchemaVersion)
            {
                Properties = definition,
                FullyQualifiedName = name,
            };

            await engine.Management.ValidateAndCreateFlow(name, def.Properties);
            return def;
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
