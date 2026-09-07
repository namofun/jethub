namespace Xylab.Workflows.LogicApps.WebApi;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Xylab.Workflows.LogicApps.Engine;

public class EngineReadinessFilter(WorkflowEngineProvider workflowEngineProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (workflowEngineProvider.IsReady)
        {
            var engine = await workflowEngineProvider.GetEngineAsync();
            context.HttpContext.Features.Set(engine);
            return await next(context);
        }
        else
        {
            return new NewtonsoftJsonResult(
                new ErrorResponseMessage(
                    ErrorResponseCode.ServerTimeout,
                    "Workflow engine initialization is in progress."))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }
    }
}
