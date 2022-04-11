using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.Azure.Workflows.Data;
using Microsoft.Azure.Workflows.Data.Configuration;
using Microsoft.Azure.Workflows.Data.Engines;
using Microsoft.Azure.Workflows.Data.Entities;
using Microsoft.Azure.Workflows.Templates.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xylab.Workflows.LogicApps.Engine;

namespace Xylab.Workflows.LogicApps.Mvc
{
    public abstract class WorkflowsController : ControllerBase
    {
        protected readonly WorkflowEngine Engine;

        protected WorkflowsController(WorkflowEngineProvider workflowEngineProvider)
        {
            Engine = workflowEngineProvider.GetEngineAsync().Result;
        }

        protected async Task<IActionResult> InvokeFlowTrigger(Flow flow, string triggerName)
        {
            HttpRequestMessage req = await HttpRequestMessageFactory.FromHttpContext(Request);
            using (RequestCorrelationContext.Current.Initialize(apiVersion: FlowConstants.PrivatePreview20190601ApiVersion, localizationLanguage: "en-us"))
            {
                RequestIdentity clientRequestIdentity = new()
                {
                    Claims = User.Claims.ToDictionary(k => k.Type, v => v.Value),
                    IsAuthenticated = true,
                };

                clientRequestIdentity.AuthorizeRequest(RequestAuthorizationSource.Direct);
                RequestCorrelationContext.Current.SetAuthenticationIdentity(clientRequestIdentity);

                var trigger = flow.Definition.GetTrigger(triggerName);
                if (trigger.IsFlowRecurrentTrigger() || trigger.IsNotificationTrigger())
                {
                    await Engine.Management.RunFlowRecurrentTrigger(flow, flow.FlowName, triggerName);
                    return new AcceptedResult();
                }
                else
                {
                    FlowHttpEngine httpEngine = Engine.Management.GetFlowHttpEngine();

                    JToken triggerOutput = await httpEngine.GetOperationOutput(
                        request: req,
                        flowLogger: Engine.Configuration.EventSource,
                        HttpContext.RequestAborted);

                    return new HttpResponseMessageResult(
                        await Engine.Management.RunFlowPushTrigger(
                            request: req,
                            context: new FlowDataPlaneContext(flow),
                            trigger: trigger,
                            subscriptionId: FlowConfiguration.EdgeSubscriptionId,
                            resourceGroup: FlowConfiguration.EdgeResourceGroupName,
                            flowName: flow.FlowName,
                            triggerName: triggerName,
                            triggerOutput: triggerOutput,
                            clientCancellationToken: HttpContext.RequestAborted));
                }
            }
        }

        protected virtual async Task<Flow> FindFlowAsync(string workflowId)
        {
            Flow flow = await Engine.FindFlowByIdentifier(workflowId);
            flow ??= await Engine.FindFlowByName(workflowId);
            return flow;
        }
    }
}
