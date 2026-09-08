namespace Xylab.Management.VirtualFileSystem;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Routing;

public record VfsRequest : IBindableFromHttpContext<VfsRequest>
{
    required public Stream Body { get; init; }

    required public RouteData RouteData { get; init; }

    required public Uri Uri { get; init; }

    required public RequestHeaders Headers { get; init; }

    public static ValueTask<VfsRequest> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return ValueTask.FromResult(new VfsRequest
        {
            Body = context.Request.Body,
            RouteData = context.GetRouteData(),
            Uri = UriHelper.GetRequestUri(context.Request),
            Headers = context.Request.GetTypedHeaders(),
        });
    }
}
