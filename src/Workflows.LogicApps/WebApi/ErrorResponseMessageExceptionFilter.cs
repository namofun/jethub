namespace Xylab.Workflows.LogicApps.WebApi;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.ErrorResponses;

public sealed class ErrorResponseMessageExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ErrorResponseMessageException ex)
        {
            return new NewtonsoftJsonResult(ex.ToErrorResponseMessage())
            {
                StatusCode = (int)ex.HttpStatus,
            };
        }
    }
}
