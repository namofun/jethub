using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Mvc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequestCorrelationFilterAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            using (RequestCorrelationContext.Current.Initialize(
                apiVersion: FlowConstants.PrivatePreview20190601ApiVersion,
                userAgent: context.HttpContext.Request.Headers.UserAgent.FirstOrDefault(),
                localizationLanguage: "en-us"))
            {
                RequestCorrelationContext.Current.SetAuthenticationIdentity(new RequestIdentity()
                {
                    Claims = context.HttpContext.User.Claims.ToDictionary(k => k.Type, v => v.Value),
                    IsAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false,
                    AuthorizedBy = RequestAuthorizationSource.Management,
                });

                await next();
            }
        }
    }
}
