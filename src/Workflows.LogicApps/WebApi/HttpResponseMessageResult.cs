namespace Xylab.Workflows.LogicApps.WebApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

public sealed class HttpResponseMessageResult : IResult, IDisposable
{
    public HttpResponseMessage Response { get; }

    public HttpResponseMessageResult(HttpResponseMessage response)
    {
        Response = response;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)Response.StatusCode;
        foreach (var header in Response.Headers.Concat(Response.Content.Headers))
        {
            context.Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
        }

        using Stream respStream = await Response.Content.ReadAsStreamAsync();
        await respStream.CopyToAsync(context.Response.Body);
    }

    public void Dispose()
    {
        Response.Dispose();
    }
}
