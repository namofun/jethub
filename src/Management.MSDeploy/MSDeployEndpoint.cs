namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Xylab.Management.WebDeploy.Deployment;

public sealed class MSDeployEndpoint(
    IOptions<WebDeployOptions> options,
    IWebDeployAuthenticationHandler authenticationHandler,
    StaticSiteReconciler reconciler,
    TraceSessionCoordinator traceSessions,
    ILogger<MSDeployEndpoint> logger)
{
    private readonly WebDeployOptions _options = options.Value;
    private readonly SemaphoreSlim _syncSlots = new(
        options.Value.MaximumConcurrentSyncRequests,
        options.Value.MaximumConcurrentSyncRequests);

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!await authenticationHandler.AuthenticateAsync(context.Request, cancellationToken))
        {
            await authenticationHandler.ChallengeAsync(context.Response, cancellationToken);
            return;
        }

        if (HttpMethods.IsHead(context.Request.Method))
        {
            MSDeployProtocol.ApplyCapabilityHeaders(context.Response.Headers);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        var method = context.Request.Headers[MSDeployProtocol.MethodHeader].ToString();
        if (string.Equals(method, "GetTraceStatus", StringComparison.Ordinal))
        {
            await HandleTraceStatusAsync(context, cancellationToken);
            return;
        }

        if (string.Equals(method, "Sync", StringComparison.Ordinal))
        {
            await HandleSyncAsync(context, cancellationToken);
            return;
        }

        logger.LogInformation(
            "Received unsupported MSDeploy method {Method} with request ID {RequestId}",
            method,
            context.Request.Headers[MSDeployProtocol.RequestIdHeader].ToString());

        await MSDeployProtocol.WriteNotImplementedAsync(context, cancellationToken);
    }

    private async Task HandleSyncAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = _options.MaximumRequestBytes;
        }

        await _syncSlots.WaitAsync(cancellationToken);
        try
        {
            await HandleSyncCoreAsync(context, cancellationToken);
        }
        finally
        {
            _syncSlots.Release();
        }
    }

    private async Task HandleSyncCoreAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var requestId = context.Request.Headers[MSDeployProtocol.RequestIdHeader].ToString();
        var traceCompleted = false;
        try
        {
            requestId = RequireHeader(context, MSDeployProtocol.RequestIdHeader);
            var provider = MSDeployHeaderDecoder.DecodeProviderOptions(RequireHeader(context, MSDeployProtocol.ProviderOptionsHeader));
            var syncOptions = MSDeployHeaderDecoder.DecodeSyncOptions(RequireHeader(context, MSDeployProtocol.SyncOptionsHeader));
            if (!string.Equals(provider.ProviderName, "contentPath", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Provider '{provider.ProviderName}' is not supported.");
            }

            var requestBody = await ReadBodyAsync(context.Request.Body, _options.MaximumRequestBytes, cancellationToken);
            var payload = DeploymentPayloadParser.Parse(requestBody);

            var result = await reconciler.ReconcileAsync(
                provider.Path,
                payload,
                writeContent: string.Equals(context.Request.Headers[MSDeployProtocol.PassIdHeader], "2", StringComparison.Ordinal),
                syncOptions.WhatIf,
                cancellationToken);

            logger.LogInformation(
                "Processed contentPath sync pass {PassId}: {FilesWritten} files written, {FilesDeleted} deleted, {BytesWritten} bytes",
                context.Request.Headers[MSDeployProtocol.PassIdHeader].ToString(),
                result.FilesWritten,
                result.FilesDeleted,
                result.BytesWritten);

            traceSessions.Complete(
                requestId,
                new TraceResult(
                    payload.Files
                        .Where(file => file.Content is null)
                        .Select(file => file.Id)
                        .ToArray()));

            traceCompleted = true;
            context.Response.Headers[MSDeployProtocol.ChangeSummaryHeader] =
                ChangeSummaryEncoder.Encode(
                    result.BytesWritten,
                    result.FilesWritten + result.DirectoriesCreated,
                    result.FilesDeleted);

            await MSDeployProtocol.WriteSuccessAsync(context, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Rejected malformed MSDeploy request {RequestId}", requestId);
            await MSDeployProtocol.WriteErrorAsync(context,400, "WEBDEPLOY_INVALID_REQUEST", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "MSDeploy request {RequestId} failed without changing the active site", requestId);
            await MSDeployProtocol.WriteErrorAsync(context, 500, "WEBDEPLOY_DEPLOYMENT_FAILED", cancellationToken);
        }
        finally
        {
            if (!traceCompleted && !string.IsNullOrEmpty(requestId))
            {
                traceSessions.Complete(requestId, new TraceResult([]));
            }
        }
    }

    private async Task HandleTraceStatusAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var requestId = RequireHeader(context, MSDeployProtocol.RequestIdHeader);
        context.Response.Headers[MSDeployProtocol.ResponseHeader] = MSDeployProtocol.ResponseVersion;
        context.Response.ContentType = "text/html";
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.StartAsync(cancellationToken);

        await context.Response.Body.WriteAsync(Encoding.UTF8.GetPreamble(), cancellationToken);
        await context.Response.WriteAsync("<results>", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        try
        {
            var result = await traceSessions.WaitAsync(requestId, cancellationToken);
            foreach (var objectId in result.IncompleteObjectIds)
            {
                await context.Response.WriteAsync($"<incomplete objectId=\"{objectId}\" />", cancellationToken);
            }

            await context.Response.WriteAsync("</results>", cancellationToken);
        }
        finally
        {
            traceSessions.Remove(requestId);
        }
    }

    private static string RequireHeader(HttpContext context, string name)
    {
        var value = context.Request.Headers[name].ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Required header '{name}' is missing.");
        }

        return value;
    }

    private static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(
        Stream request,
        int maximumRequestBytes,
        CancellationToken cancellationToken)
    {
        using var body = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await request.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return body.ToArray();
            }

            if (body.Length + read > maximumRequestBytes)
            {
                throw new InvalidDataException("The MSDeploy request body is too large.");
            }

            await body.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }
}
