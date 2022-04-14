using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Extensions;
using Microsoft.Azure.Workflows.Templates.Schema;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Mvc
{
    public static class Validation
    {
        public static void NotNull(Flow flow, ErrorResponseCode code = ErrorResponseCode.WorkflowNotFound)
        {
            if (flow == null)
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.NotFound,
                    code,
                    "Didn't found workflow with corresponding ID or name.");
            }
        }

        public static void NotNull(FlowRun run)
        {
            if (run == null)
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.NotFound,
                    ErrorResponseCode.WorkflowRunNotFound,
                    "Didn't found workflow run with corresponding ID or name.");
            }
        }

        public static async Task<Flow> NotNull(this Task<Flow> task, ErrorResponseCode code = ErrorResponseCode.WorkflowNotFound)
        {
            Flow flow = await task.ConfigureAwait(false);
            Validation.NotNull(flow, code);
            return flow;
        }

        public static async Task<FlowRun> NotNull(this Task<FlowRun> task)
        {
            FlowRun run = await task.ConfigureAwait(false);
            Validation.NotNull(run);
            return run;
        }

        public static async Task<TValue> GetContentJson<TValue>(HttpRequest request)
        {
            if (!request.Headers.ContentLength.HasValue)
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.BadRequest,
                    ErrorResponseCode.BadRequest,
                    "Request must specify content length.");
            }

            if (request.Headers.ContentType.Count != 1 || request.Headers.ContentType[0] != "application/json")
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.BadRequest,
                    ErrorResponseCode.BadRequest,
                    "Request content type must be application/json.");
            }

            string contentJsonRaw;
            TValue contentObject;
            using (StreamReader sr = new(request.Body))
            {
                contentJsonRaw = await sr.ReadToEndAsync();
            }
            try
            {
                contentObject = JsonConvert.DeserializeObject<TValue>(contentJsonRaw);
            }
            catch (JsonException ex)
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.BadRequest,
                    ErrorResponseCode.BadRequest,
                    "Cannot decode request body. Exception: " + ex.Message);
            }

            return contentObject;
        }

        public static FlowTemplateTrigger Trigger(Flow flow, string triggerName)
        {
            if (!flow.Definition.Triggers.ContainsKey(triggerName))
            {
                throw new ErrorResponseMessageException(
                    HttpStatusCode.NotFound,
                    ErrorResponseCode.WorkflowTriggerNotFound,
                    "Didn't found trigger with corresponding name.");
            }

            return flow.Definition.GetTrigger(triggerName);
        }
    }
}
