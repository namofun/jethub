using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Workflows.Common.ErrorResponses;
using System;

namespace Xylab.Workflows.LogicApps.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ErrorResponseMessageExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is ErrorResponseMessageException ex)
            {
                context.ExceptionHandled = true;
                context.Result = new NewtonsoftJsonResult(ex.ToErrorResponseMessage())
                {
                    StatusCode = (int)ex.HttpStatus
                };
            }
        }
    }
}
