using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Data.Entities;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xylab.Workflows.LogicApps.Engine;

namespace JetHub.Controllers
{
    public class WorkflowsController : Xylab.Workflows.LogicApps.Mvc.WorkflowsController
    {
        public WorkflowsController(WorkflowEngineProvider workflowEngineProvider)
            : base(workflowEngineProvider)
        {
        }

        [Route("workflows/{workflowId}/triggers/{triggerName}/paths/invoke")]
        public async Task<IActionResult> InvokeTrigger(
            [FromRoute] string workflowId,
            [FromRoute] string triggerName)
        {
            Flow flow = await FindFlowAsync(workflowId);
            if (flow == null || !flow.Definition.Triggers.ContainsKey(triggerName)) return NotFound();
            return await InvokeFlowTrigger(flow, triggerName);
        }

        [Route("[action]/{**slug}")]
        public async Task<IActionResult> PingPong()
        {
            string body = null;
            if (Request.ContentLength.HasValue)
            {
                MemoryStream stream = new();
                await Request.Body.CopyToAsync(stream);
                body = Encoding.UTF8.GetString(stream.ToArray());
            }

            return new JsonResult(new
            {
                headers = Request.Headers.ToDictionary(k => k.Key, v => v.Value.Count == 1 ? v.Value.Single() : (object)v.Value.ToArray()),
                body,
                method = Request.Method,
                url = $"http://localhost{Request.Path}{Request.QueryString}",
            });
        }
    }
}
