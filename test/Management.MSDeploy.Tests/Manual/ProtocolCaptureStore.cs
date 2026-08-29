namespace Xylab.Management.WebDeploy.UnitTests.Manual;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

internal sealed class ProtocolCaptureStore
{
    public async Task SaveAsync(
        HttpRequest request,
        byte[] requestBody,
        HttpResponseMessage response,
        byte[] responseBody,
        ReferenceProxyOptions options,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.CaptureDirectory);
        string id = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var metadata = new
        {
            request.Method,
            Path = request.Path.Value,
            Query = request.QueryString.Value,
            RequestHeaders = FilterHeaders(request.Headers),
            SensitiveRequestHeaderNames =
                request.Headers
                    .Where(header => IsSensitive(header.Key))
                    .Select(header => header.Key)
                    .ToArray(),
            StatusCode = (int)response.StatusCode,
            ResponseHeaders = FilterHeaders(response.Headers, response.Content.Headers),
            SensitiveResponseHeaderNames =
                response.Headers
                    .Concat(response.Content.Headers)
                    .Where(header => IsSensitive(header.Key))
                    .Select(header => header.Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            TrailingHeaders = FilterHeaders(response.TrailingHeaders)
        };

        await File.WriteAllTextAsync(
            Path.Combine(options.CaptureDirectory, $"{id}.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
        await File.WriteAllBytesAsync(
            Path.Combine(options.CaptureDirectory, $"{id}.request.bin"),
            requestBody,
            cancellationToken);
        await File.WriteAllBytesAsync(
            Path.Combine(options.CaptureDirectory, $"{id}.response.bin"),
            responseBody,
            cancellationToken);
    }

    private static Dictionary<string, string[]> FilterHeaders(IHeaderDictionary headers)
    {
        return headers
            .Where(header => !IsSensitive(header.Key))
            .ToDictionary(
                header => header.Key,
                header => header.Value.Select(value => value ?? string.Empty).ToArray());
    }

    private static Dictionary<string, string[]> FilterHeaders(params HttpHeaders[] sources)
    {
        return sources
            .SelectMany(source => source)
            .Where(header => !IsSensitive(header.Key))
            .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(header => header.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSensitive(string name)
    {
        return name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase);
    }
}
