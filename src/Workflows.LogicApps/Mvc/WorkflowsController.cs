namespace Xylab.Workflows.LogicApps.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using Newtonsoft.Json.Linq;
using Xylab.Workflows.LogicApps.Engine;

[RequestCorrelationFilter]
[ErrorResponseMessageExceptionFilter]
public abstract class WorkflowsControllerBase(WorkflowEngineProvider workflowEngineProvider) : ControllerBase, IAsyncActionFilter
{
    private WorkflowEngine? engine;

    protected WorkflowEngine Engine
    {
        get => engine ?? throw new InvalidOperationException("Engine is used before initialized.");
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetFlows()
    {
        SegmentedList<Flow> flows = await Engine.FindFlowsSegmented();
        return Json(flows.Select(Engine.GetFlowDefinition));
    }

    [HttpGet("{workflowId}")]
    public virtual async Task<IActionResult> GetFlow([FromRoute] string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        return Json(Engine.GetFlowDefinition(flow), flow.EntityTag);
    }

    [HttpGet("{workflowId}/versions")]
    public virtual async Task<IActionResult> GetVersions([FromRoute] string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        SegmentedList<Flow> ver = await Engine.FindFlowVersionsSegmented(flow);
        return Json(ver.Select(Engine.GetFlowVersionDefinition));
    }

    [HttpGet("{workflowId}/versions/{version}")]
    public virtual async Task<IActionResult> GetVersion([FromRoute] string workflowId, [FromRoute] string version)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        Flow ver = await Engine.FindFlowVersion(flow, version).NotNull(ErrorResponseCode.WorkflowVersionNotFound);
        return Json(Engine.GetFlowVersionDefinition(ver), ver.EntityTag);
    }

    [HttpGet("{workflowId}/triggers")]
    public virtual async Task<IActionResult> GetTriggers([FromRoute] string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        return Json(
            flow.Definition.Triggers.Keys.Select(
                triggerName => Engine.GetFlowTriggerDefinition(flow, triggerName)),
            flow.EntityTag);
    }

    [HttpGet("{workflowId}/triggers/{triggerName}")]
    public virtual async Task<IActionResult> GetTrigger([FromRoute] string workflowId, [FromRoute] string triggerName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        Validation.Trigger(flow, triggerName);
        return Json(Engine.GetFlowTriggerDefinition(flow, triggerName), flow.EntityTag);
    }

    [Route("{workflowId}/triggers/{triggerName}/paths/invoke")]
    public virtual async Task<IActionResult> InvokeTrigger([FromRoute] string workflowId, [FromRoute] string triggerName)
    {
        RequestCorrelationContext.Current.AuthenticationIdentity.AuthorizedBy = RequestAuthorizationSource.Direct;
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowTemplateTrigger trigger = Validation.Trigger(flow, triggerName);

        return await Engine.InvokeFlowTrigger(
            flow: flow,
            triggerName: triggerName,
            trigger: trigger,
            req: await HttpRequestMessageFactory.FromHttpContext(Request),
            cancellationToken: HttpContext.RequestAborted);
    }

    [HttpGet("{workflowId}/runs")]
    public virtual async Task<IActionResult> GetRuns([FromRoute] string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        SegmentedList<FlowRun> runs = await Engine.FindFlowRunsSegmented(flow);
        return Json(runs.Select(run => Engine.GetFlowRunDefinition(flow, run)));
    }

    [HttpGet("{workflowId}/runs/{sequenceId}")]
    public virtual async Task<IActionResult> GetRun([FromRoute] string workflowId, [FromRoute] string sequenceId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction[] actions = await Engine.FindFlowRunActions(flow, run);
        return Json(Engine.GetFlowRunDefinition(flow, run, actions), run.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/contents/{contentName}")]
    public virtual async Task<IActionResult> GetRunContents([FromRoute] string workflowId, [FromRoute] string sequenceId, [FromRoute] string contentName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();

        JToken? result = contentName switch
        {
            "TriggerInputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, run.Trigger?.InputsLink),
            "TriggerOutputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, run.Trigger?.OutputsLink),
            "TriggerError" => run.Trigger?.Error,
            "ResponseInputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, run.Response?.InputsLink),
            "ResponseOutputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, run.Response?.OutputsLink),
            "ResponseError" => run.Response?.Error,
            "Error" => run.Error,
            _ => throw new ErrorResponseMessageException(
                System.Net.HttpStatusCode.NotFound,
                ErrorResponseCode.WorkflowRunOperationNotFound,
                "No content found."),
        };

        return Content(
            Newtonsoft.Json.JsonConvert.SerializeObject(result ?? JRaw.CreateNull(), Newtonsoft.Json.Formatting.Indented),
            "application/json");
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions")]
    public async Task<IActionResult> GetRunActions([FromRoute] string workflowId, [FromRoute] string sequenceId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction[] actions = await Engine.FindFlowRunActions(flow, run);
        return Json(actions.Select(action => Engine.GetFlowRunActionDefinition(flow, run, action)), run.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions/{actionName}")]
    public async Task<IActionResult> GetRunAction([FromRoute] string workflowId, [FromRoute] string sequenceId, [FromRoute] string actionName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction action = await Engine.FindFlowRunAction(flow, run, actionName).NotNull();
        return Json(Engine.GetFlowRunActionDefinition(flow, run, action), action.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions/{actionName}/contents/{contentName}")]
    public async Task<IActionResult> GetRunActionContents([FromRoute] string workflowId, [FromRoute] string sequenceId, [FromRoute] string actionName, [FromRoute] string contentName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction action = await Engine.FindFlowRunAction(flow, run, actionName).NotNull();

        JToken result = contentName switch
        {
            "ActionInputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, action?.InputsLink),
            "ActionOutputs" => await Engine.GetContentLink(flow, run.FlowRunSequenceId, action?.OutputsLink),
            "Error" => run.Error,
            _ => throw new ErrorResponseMessageException(
                System.Net.HttpStatusCode.NotFound,
                ErrorResponseCode.WorkflowRunOperationNotFound,
                "No content found."),
        };

        return Content(
            Newtonsoft.Json.JsonConvert.SerializeObject(result ?? JRaw.CreateNull(), Newtonsoft.Json.Formatting.Indented),
            "application/json");
    }

    [HttpPost("{workflowName}")]
    public async Task<IActionResult> UpsertWorkflow([FromRoute] string workflowName)
    {
        FlowPropertiesDefinition definition =
            await Validation.GetContentJson<FlowPropertiesDefinition>(Request);
        definition.Parameters ??= new();

        await Engine.ValidateAndCreateFlow(workflowName, definition);
        Flow flow = await Engine.FindFlowByName(workflowName);
        return Json(Engine.GetFlowDefinition(flow), flow.EntityTag, 202);
    }

    private NewtonsoftJsonResult Json(ResourceDefinition resource, string etag, int statusCode = 200)
    {
        if (etag != null) Response.Headers.ETag = etag;
        return new NewtonsoftJsonResult(resource) { StatusCode = statusCode };
    }

    private NewtonsoftJsonResult Json(IEnumerable<ResourceDefinition> resources, string? etag = null)
    {
        if (etag != null) Response.Headers.ETag = etag;
        return new NewtonsoftJsonResult(new { value = resources.ToList() });
    }

    public virtual async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (workflowEngineProvider.IsReady)
        {
            engine = await workflowEngineProvider.GetEngineAsync();
            await next();
        }
        else
        {
            context.Result = new NewtonsoftJsonResult(
                new ErrorResponseMessage(
                    ErrorResponseCode.ServerTimeout,
                    "Workflow engine initialization is in progress."))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }
    }
}
