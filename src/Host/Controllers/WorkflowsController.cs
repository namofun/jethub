﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Azure.Workflows.Data.Definitions;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xylab.Workflows.LogicApps.Engine;
using Xylab.Workflows.LogicApps.Mvc;

namespace JetHub.Controllers
{
    [ErrorResponseMessageExceptionFilter]
    public class WorkflowsController : ControllerBase
    {
        protected readonly WorkflowEngine Engine;

        public WorkflowsController(WorkflowEngineProvider workflowEngineProvider)
        {
            Engine = workflowEngineProvider.GetEngineAsync().Result;
        }

        [HttpGet("workflows/{workflowId}")]
        public async Task<IActionResult> GetFlow([FromRoute] string workflowId)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            return Json(Engine.GetFlowDefinition(flow), flow.EntityTag);
        }

        [HttpGet("workflows/{workflowId}/versions")]
        public async Task<IActionResult> GetVersions([FromRoute] string workflowId)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            SegmentedList<Flow> ver = await Engine.FindFlowVersionsSegmented(flow);
            return Json(ver.Select(Engine.GetFlowVersionDefinition));
        }

        [HttpGet("workflows/{workflowId}/versions/{version}")]
        public async Task<IActionResult> GetVersion([FromRoute] string workflowId, [FromRoute] string version)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            Flow ver = await Engine.FindFlowVersion(flow, version).NotNull(ErrorResponseCode.WorkflowVersionNotFound);
            return Json(Engine.GetFlowVersionDefinition(ver), ver.EntityTag);
        }

        [HttpGet("workflows/{workflowId}/triggers")]
        public async Task<IActionResult> GetTriggers([FromRoute] string workflowId)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            return Json(
                flow.Definition.Triggers.Keys.Select(
                    triggerName => Engine.GetFlowTriggerDefinition(flow, triggerName)),
                flow.EntityTag);
        }

        [HttpGet("workflows/{workflowId}/triggers/{triggerName}")]
        public async Task<IActionResult> GetTrigger([FromRoute] string workflowId, [FromRoute] string triggerName)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            Validation.Trigger(flow, triggerName);
            return Json(Engine.GetFlowTriggerDefinition(flow, triggerName), flow.EntityTag);
        }

        [Route("workflows/{workflowId}/triggers/{triggerName}/paths/invoke")]
        public async Task<IActionResult> InvokeTrigger([FromRoute] string workflowId, [FromRoute] string triggerName)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            FlowTemplateTrigger trigger = Validation.Trigger(flow, triggerName);

            return await Engine.InvokeFlowTrigger(
                flow: flow,
                triggerName: triggerName,
                trigger: trigger,
                req: await HttpRequestMessageFactory.FromHttpContext(Request),
                user: User,
                cancellationToken: HttpContext.RequestAborted);
        }

        [HttpGet("workflows/{workflowId}/runs")]
        public async Task<IActionResult> GetRuns([FromRoute] string workflowId)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            SegmentedList<FlowRun> runs = await Engine.FindFlowRunsSegmented(flow);
            return Json(runs.Select(run => Engine.GetFlowRunDefinition(flow, run)));
        }

        [HttpGet("workflows/{workflowId}/runs/{sequenceId}")]
        public async Task<IActionResult> GetRun([FromRoute] string workflowId, [FromRoute] string sequenceId)
        {
            Flow flow = await Engine.FindFlowByIdOrName(workflowId).NotNull();
            FlowRun run = await Engine.FindFlowRunBySequenceId(flow, sequenceId).NotNull();
            return Json(Engine.GetFlowRunDefinition(flow, run), run.EntityTag);
        }

        [HttpPost("workflows/{workflowName}")]
        public async Task<IActionResult> UpsertWorkflow([FromRoute] string workflowName)
        {
            FlowPropertiesDefinition definition =
                await Validation.GetContentJson<FlowPropertiesDefinition>(Request);

            await Engine.Management.ValidateAndCreateFlow(workflowName, definition);
            Flow flow = await Engine.FindFlowByName(workflowName);
            return Json(Engine.GetFlowDefinition(flow), flow.EntityTag, 202);
        }

        private NewtonsoftJsonResult Json(ResourceDefinition resource, string etag, int statusCode = 200)
        {
            if (etag != null) Response.Headers.ETag = etag;
            return new NewtonsoftJsonResult(resource) { StatusCode = statusCode };
        }

        private NewtonsoftJsonResult Json(IEnumerable<ResourceDefinition> resources, string etag = null)
        {
            if (etag != null) Response.Headers.ETag = etag;
            return new NewtonsoftJsonResult(new { value = resources });
        }
    }
}
