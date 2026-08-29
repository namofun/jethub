namespace Xylab.Management.WebDeploy;

using Microsoft.AspNetCore.Http;
using System.Net;

public static class MSDeployProtocol
{
    public const string MediaType = "application/msdeploy";
    public const string ResponseHeader = "MSDeploy.Response";
    public const string MethodHeader = "MSDeploy.Method";
    public const string RequestIdHeader = "MSDeploy.RequestId";
    public const string PassIdHeader = "MSDeploy.PassId";
    public const string ProviderOptionsHeader = "MSDeploy.ProviderOptions";
    public const string SyncOptionsHeader = "MSDeploy.SyncOptions";
    public const string ChangeSummaryHeader = "MSDeploy.ChangeSummary";
    public const string VersionMinimumHeader = "MSDeploy.VersionMin";
    public const string VersionMaximumHeader = "MSDeploy.VersionMax";
    public const string ServerVersionHeader = "ServerVersion";

    public const string ResponseVersion = "v1";
    public const string VersionMinimum = "7.1.600.0";
    public const string VersionMaximum = "9.0.1987.0";

    public static void ApplyCapabilityHeaders(IHeaderDictionary headers)
    {
        headers[ResponseHeader] = ResponseVersion;
        headers[VersionMinimumHeader] = VersionMinimum;
        headers[VersionMaximumHeader] = VersionMaximum;
    }

    public static Task WriteSuccessAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return WriteResponseAsync(context, StatusCodes.Status200OK, "<results/>", cancellationToken);
    }

    public static Task WriteNotImplementedAsync(HttpContext context, CancellationToken cancellationToken)
    {
        const string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><error code=\"WEBDEPLOY_METHOD_NOT_IMPLEMENTED\" />";
        return WriteResponseAsync(context, StatusCodes.Status501NotImplemented, body, cancellationToken);
    }

    public static Task WriteErrorAsync(HttpContext context, int statusCode, string code, CancellationToken cancellationToken)
    {
        string results = $"<results><error code=\"{WebUtility.HtmlEncode(code)}\" /></results>";
        return WriteResponseAsync(context, statusCode, results, cancellationToken);
    }

    public static async Task WriteResponseAsync(HttpContext context, int statusCode, string results, CancellationToken cancellationToken)
    {
        ApplyCapabilityHeaders(context.Response.Headers);
        context.Response.ContentType = MediaType;
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(results, cancellationToken);
    }
}
