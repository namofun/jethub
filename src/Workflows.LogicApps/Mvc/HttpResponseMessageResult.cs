using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Mvc
{
    public sealed class HttpResponseMessageResult : IActionResult, IDisposable
    {
        public HttpResponseMessage Response { get; }

        public HttpResponseMessageResult(HttpResponseMessage response)
        {
            Response = response;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)Response.StatusCode;
            foreach (var header in Response.Headers.Concat(Response.Content.Headers))
            {
                context.HttpContext.Response.Headers[header.Key] = new StringValues(header.Value.ToArray());
            }

            using Stream respStream = await Response.Content.ReadAsStreamAsync();
            await respStream.CopyToAsync(context.HttpContext.Response.Body);
        }

        public void Dispose()
        {
            Response.Dispose();
        }
    }
}
