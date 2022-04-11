using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;
using System;

namespace Xylab.Workflows.LogicApps.Engine
{
    public sealed class WorkflowEngine : IDisposable
    {
        public EdgeFlowConfiguration Configuration { get; }

        public EdgeFlowWebManagementEngine Management { get; }

        public EdgeFlowJobsDispatcher JobsDispatcher { get; }

        public WorkflowEngine(
            EdgeFlowConfiguration configuration,
            EdgeFlowWebManagementEngine engine,
            EdgeFlowJobsDispatcher jobsDispatcher)
        {
            Configuration = configuration;
            Management = engine;
            JobsDispatcher = jobsDispatcher;
        }

        public void Dispose()
        {
            JobsDispatcher.Dispose();
        }
    }
}
