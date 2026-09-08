namespace Xylab.Workflows.LogicApps.WebApi;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Xylab.Workflows.LogicApps.Engine;

public class EngineReadinessFilter(WorkflowEngineProvider workflowEngineProvider) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (workflowEngineProvider.Instance != null)
        {
            return next(context);
        }
        else
        {
            return ValueTask.FromResult<object?>(new NewtonsoftJsonResult(
                new ErrorResponseMessage(
                    ErrorResponseCode.ServerTimeout,
                    "Workflow engine initialization is in progress."))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            });
        }
    }
}
