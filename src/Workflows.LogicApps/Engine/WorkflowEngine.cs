using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;
using System.Web.Http;

namespace Xylab.Workflows.LogicApps.Engine
{
    public sealed class WorkflowEngine : IFlowConfigurationHolder
    {
        public EdgeFlowConfiguration Configuration { get; }

        public EdgeFlowWebManagementEngine Management { get; }

        public EdgeFlowJobsDispatcher JobsDispatcher { get; }

        public HttpConfiguration HttpConfiguration { get; }

        FlowConfiguration IFlowConfigurationHolder.FlowConfiguration => Configuration;

        public WorkflowEngine(
            EdgeFlowConfiguration configuration,
            EdgeFlowWebManagementEngine engine,
            EdgeFlowJobsDispatcher jobsDispatcher,
            HttpConfiguration httpConfiguration)
        {
            Configuration = configuration;
            Management = engine;
            JobsDispatcher = jobsDispatcher;
            HttpConfiguration = httpConfiguration;
        }
    }
}
