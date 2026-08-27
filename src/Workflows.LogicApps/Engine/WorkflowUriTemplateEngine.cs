namespace Xylab.Workflows.LogicApps.Engine;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Workflows.Data.Common;
using Microsoft.Azure.Workflows.Data.Engines;
using Xylab.Workflows.LogicApps.Mvc;

public class WorkflowUriTemplateEngine(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator) : FlowUriTemplateEngine
{
    protected override Uri GetFlowDirectApiSystemIdUri(Uri endpoint, string scaleUnit, string flowId, FlowExecutionClusterType flowExecutionClusterType)
    {
        return GetActionUrl(nameof(WorkflowsControllerBase.GetFlow), new { workflowId = flowId });
    }

    public override Uri GetFlowRunActionDirectApiUri(Uri endpoint, string apiVersion, string scaleUnit, string flowId, string runName, string actionName, FlowExecutionClusterType flowExecutionClusterType)
    {
        return GetActionUrl(nameof(WorkflowsControllerBase.GetRunAction), new { workflowId = flowId, sequenceId = runName, actionName });
    }

    public override Uri GetFlowRunActionContentDirectApiUri(Uri endpoint, string apiVersion, string scaleUnit, string flowId, string flowRunSequenceId, string actionName, string contentName, FlowExecutionClusterType flowExecutionClusterType)
    {
        return GetActionUrl(nameof(WorkflowsControllerBase.GetRunActionContents), new { workflowId = flowId, sequenceId = flowRunSequenceId, actionName, contentName });
    }

    protected override Uri GetFlowRunTriggerDirectApiUri(Uri endpoint, string apiVersion, string scaleUnit, string flowId, string triggerName, FlowExecutionClusterType flowExecutionClusterType)
    {
        return GetActionUrl(nameof(WorkflowsControllerBase.GetTrigger), new { workflowId = flowId, triggerName });
    }

    public override Uri GetFlowRunOperationContentDirectApiUri(Uri endpoint, string apiVersion, string scaleUnit, string flowId, string flowRunSequenceId, string contentName, FlowExecutionClusterType flowExecutionClusterType)
    {
        return GetActionUrl(nameof(WorkflowsControllerBase.GetRunContents), new { workflowId = flowId, sequenceId = flowRunSequenceId, contentName });
    }

    private Uri GetActionUrl(string action, object values)
    {
        return new Uri(linkGenerator.GetUriByAction(httpContextAccessor.HttpContext!, action, controller: null, values)!);
    }
}
