namespace Xylab.Management.WebDeploy.UnitTests.Manual;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class ReferenceProxy(
    IHttpClientFactory httpClientFactory,
    ReferenceProxyOptions options,
    ProtocolCaptureStore captureStore)
{
    public const string ClientName = "MSDeployReferenceManualTest";

    public async Task ForwardAsync(HttpContext context, CancellationToken cancellationToken)
    {
        byte[] requestBody = await ReadBodyAsync(context.Request.Body, options.MaximumCaptureBytes, cancellationToken);
        using HttpRequestMessage request = new(new HttpMethod(context.Request.Method), options.Endpoint);
        foreach (var header in context.Request.Headers)
        {
            if (IsHopByHopHeader(header.Key)
                || header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}")));

        if (requestBody.Length > 0 || HttpMethods.IsPost(context.Request.Method))
        {
            request.Content = new ByteArrayContent(requestBody);
            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }

        HttpClient client = httpClientFactory.CreateClient(ClientName);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyHeaders(response.Headers, context.Response.Headers);
        CopyHeaders(response.Content.Headers, context.Response.Headers);
        context.Response.Headers.Remove("transfer-encoding");
        context.Response.Headers.Remove("content-length");

        byte[] responseBody = await ForwardResponseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            context.Response,
            options.MaximumCaptureBytes,
            cancellationToken);

        await captureStore.SaveAsync(
            context.Request,
            requestBody,
            response,
            responseBody,
            options,
            cancellationToken);
    }

    private static async Task<byte[]> ForwardResponseAsync(
        Stream source,
        HttpResponse destination,
        int maximumCaptureBytes,
        CancellationToken cancellationToken)
    {
        using MemoryStream capture = new();
        byte[] chunk = new byte[81920];
        while (true)
        {
            int read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                return capture.ToArray();
            }

            await destination.Body.WriteAsync(chunk.AsMemory(0, read), cancellationToken);

            int remaining = maximumCaptureBytes - (int)capture.Length;
            if (remaining > 0)
            {
                await capture.WriteAsync(
                    chunk.AsMemory(0, Math.Min(read, remaining)),
                    cancellationToken);
            }
        }
    }

    private static async Task<byte[]> ReadBodyAsync(Stream stream, int maximumBytes, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        while (true)
        {
            int read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + read > maximumBytes)
            {
                throw new InvalidDataException(
                    $"Protocol capture exceeds the configured {maximumBytes}-byte limit.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
    }

    private static void CopyHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        foreach (var header in source)
        {
            if (!IsHopByHopHeader(header.Key))
            {
                destination[header.Key] = header.Value.ToArray();
            }
        }
    }

    private static bool IsHopByHopHeader(string name)
    {
        return name.Equals("Connection", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
            || name.Equals("TE", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Trailer", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
    }
}
