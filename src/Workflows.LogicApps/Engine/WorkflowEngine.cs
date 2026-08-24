namespace Xylab.Workflows.LogicApps.Engine;

using System.Web.Http;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Web.Engines;
using Microsoft.Azure.Workflows.Worker;

public sealed class WorkflowEngine : IFlowConfigurationHolder
{
    required public EdgeFlowConfiguration Configuration { get; init; }

    required public EdgeFlowWebManagementEngine Management { get; init; }

    required public EdgeFlowJobsDispatcher JobsDispatcher { get; init; }

    required public HttpConfiguration HttpConfiguration { get; init; }

    required public FlowUriTemplateEngine UriTemplate { get; init; }

    FlowConfiguration IFlowConfigurationHolder.FlowConfiguration => Configuration;
}
