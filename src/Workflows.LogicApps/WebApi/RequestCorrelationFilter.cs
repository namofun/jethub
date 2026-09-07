namespace Xylab.Workflows.LogicApps.WebApi;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Workflows.Common.Constants;
using Microsoft.WindowsAzure.ResourceStack.Common.Instrumentation;

public sealed class RequestCorrelationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using (RequestCorrelationContext.Current.Initialize(
            apiVersion: FlowConstants.PrivatePreview20190601ApiVersion,
            userAgent: context.HttpContext.Request.Headers.UserAgent.FirstOrDefault(),
            localizationLanguage: "en-us"))
        {
            RequestCorrelationContext.Current.SetAuthenticationIdentity(new RequestIdentity()
            {
                Claims = context.HttpContext.User.Claims.GroupBy(k => k.Type).ToDictionary(k => k.Key, v => string.Join(",", v.Select(c => c.Value))),
                IsAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false,
                AuthorizedBy = RequestAuthorizationSource.Management,
            });

            return await next(context);
        }
    }
}
