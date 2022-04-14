using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Xylab.Workflows.LogicApps.Mvc
{
    public class HttpRequestMessageFactory
    {
        public static async Task<HttpRequestMessage> FromHttpContext(HttpRequest request)
        {
            HttpRequestMessage req = new();
            req.RequestUri = new Uri($"http://localhost{request.Path}{request.QueryString}");
            req.Method = new HttpMethod(request.Method);

            if (request.ContentLength.HasValue)
            {
                MemoryStream stream = new();
                await request.Body.CopyToAsync(stream);
                stream.Position = 0;
                req.Content = new StreamContent(stream);
            }

            foreach (var header in request.Headers)
            {
                if (header.Key.Contains(':'))
                {
                    // Some HTTP/2 requests are setting up header like ":method". Ignore here.
                    continue;
                }
                else if (header.Key.StartsWith("content-", StringComparison.OrdinalIgnoreCase))
                {
                    req.Content?.Headers.Add(header.Key, header.Value.ToArray());
                }
                else
                {
                    req.Headers.Add(header.Key, header.Value.ToArray());
                }
            }

            return req;
        }
    }
}
