namespace Xylab.Workflows.LogicApps.WebApi;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xylab.Workflows.LogicApps.Engine;

public class WorkflowsEndpoint(WorkflowEngineProvider provider)
{
    public WorkflowEngine Engine => provider.Instance!;

    [HttpGet("")]
    public async Task<IResult> GetFlows()
    {
        SegmentedList<Flow> flows = await Engine.FindFlowsSegmented();
        return Json(flows.Select(Engine.GetFlowDefinition));
    }

    [HttpGet("{workflowId}")]
    public async Task<IResult> GetFlow(string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        return Json(Engine.GetFlowDefinition(flow), flow.EntityTag);
    }

    [HttpGet("{workflowId}/versions")]
    public async Task<IResult> GetVersions(string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        SegmentedList<Flow> ver = await Engine.FindFlowVersionsSegmented(flow);
        return Json(ver.Select(Engine.GetFlowVersionDefinition));
    }

    [HttpGet("{workflowId}/versions/{version}")]
    public async Task<IResult> GetVersion(string workflowId, string version)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        Flow ver = await Engine.FindFlowVersion(flow, version).NotNull(ErrorResponseCode.WorkflowVersionNotFound);
        return Json(Engine.GetFlowVersionDefinition(ver), ver.EntityTag);
    }

    [HttpGet("{workflowId}/triggers")]
    public async Task<IResult> GetTriggers(string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        return Json(
            flow.Definition.Triggers.Keys.Select(
                triggerName => Engine.GetFlowTriggerDefinition(flow, triggerName)),
            flow.EntityTag);
    }

    [HttpGet("{workflowId}/triggers/{triggerName}")]
    public async Task<IResult> GetTrigger(string workflowId, string triggerName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        Validation.Trigger(flow, triggerName);
        return Json(Engine.GetFlowTriggerDefinition(flow, triggerName), flow.EntityTag);
    }

    [HttpAny("{workflowId}/triggers/{triggerName}/paths/invoke")]
    public async Task<IResult> InvokeTrigger(string workflowId, string triggerName, HttpRequest request, CancellationToken cancellationToken)
    {
        RequestCorrelationContext.Current.AuthenticationIdentity.AuthorizedBy = RequestAuthorizationSource.Direct;
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowTemplateTrigger trigger = Validation.Trigger(flow, triggerName);

        return await Engine.InvokeFlowTrigger(
            flow: flow,
            triggerName: triggerName,
            trigger: trigger,
            req: await HttpRequestMessageFactory.FromHttpContext(request),
            cancellationToken: cancellationToken);
    }

    [HttpGet("{workflowId}/runs")]
    public async Task<IResult> GetRuns(string workflowId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        SegmentedList<FlowRun> runs = await Engine.FindFlowRunsSegmented(flow);
        return Json(runs.Select(run => Engine.GetFlowRunDefinition(flow, run)));
    }

    [HttpGet("{workflowId}/runs/{sequenceId}")]
    public async Task<IResult> GetRun(string workflowId, string sequenceId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction[] actions = await Engine.FindFlowRunActions(flow, run);
        return Json(Engine.GetFlowRunDefinition(flow, run, actions), run.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/contents/{contentName}")]
    public async Task<IResult> GetRunContents(string workflowId, string sequenceId, string contentName)
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

        return Content(result);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions")]
    public async Task<IResult> GetRunActions(string workflowId, string sequenceId)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction[] actions = await Engine.FindFlowRunActions(flow, run);
        return Json(actions.Select(action => Engine.GetFlowRunActionDefinition(flow, run, action)), run.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions/{actionName}")]
    public async Task<IResult> GetRunAction(string workflowId, string sequenceId, string actionName)
    {
        Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
        FlowRun run = await Engine.FindFlowRun(flow, sequenceId).NotNull();
        FlowRunAction action = await Engine.FindFlowRunAction(flow, run, actionName).NotNull();
        return Json(Engine.GetFlowRunActionDefinition(flow, run, action), action.EntityTag);
    }

    [HttpGet("{workflowId}/runs/{sequenceId}/actions/{actionName}/contents/{contentName}")]
    public async Task<IResult> GetRunActionContents(string workflowId, string sequenceId, string actionName, string contentName)
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

        return Content(result);
    }

    [HttpPost("{workflowName}")]
    public async Task<IResult> UpsertWorkflow(string workflowName, HttpRequest request)
    {
        FlowPropertiesDefinition definition = await Validation.GetContentJson<FlowPropertiesDefinition>(request);
        definition.Parameters ??= new();

        await Engine.ValidateAndCreateFlow(workflowName, definition);
        Flow flow = await Engine.FindFlowByName(workflowName);
        return Json(Engine.GetFlowDefinition(flow), flow.EntityTag, 202);
    }

    private static NewtonsoftJsonResult Json(ResourceDefinition resource, string etag, int statusCode = StatusCodes.Status200OK)
    {
        return new NewtonsoftJsonResult(resource) { StatusCode = statusCode, ETag = etag };
    }

    private static NewtonsoftJsonResult Json(IEnumerable<ResourceDefinition> resources, string? etag = null)
    {
        return new NewtonsoftJsonResult(new { value = resources.ToList() }) { ETag = etag };
    }

    private static IResult Content(JToken? token)
    {
        return Results.Content(
            JsonConvert.SerializeObject(token ?? JRaw.CreateNull(), Formatting.Indented),
            contentType: "application/json");
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute([StringSyntax("Route")] string path) : Attribute
    {
        public string Path { get; } = path;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpGetAttribute([StringSyntax("Route")] string path) : RouteAttribute(path);

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPostAttribute([StringSyntax("Route")] string path) : RouteAttribute(path);

    [AttributeUsage(AttributeTargets.Method)]
    public class HttpAnyAttribute([StringSyntax("Route")] string path) : RouteAttribute(path);
}
