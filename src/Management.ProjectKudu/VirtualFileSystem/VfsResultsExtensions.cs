namespace Xylab.Management.VirtualFileSystem;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

internal static class VfsResultsExtensions
{
    extension(Results)
    {
        public static IResult RedirectPreserveMethod(Uri uri) => Results.Redirect(uri.AbsolutePath, preserveMethod: true);

        public static IResult InternalServerErrorReason(string reason) => Results.Text(reason, statusCode: StatusCodes.Status500InternalServerError);

        public static IResult NotFoundReason(string reason) => Results.Text(reason, statusCode: StatusCodes.Status404NotFound);

        public static IResult ConflictReason(string reason) => Results.Text(reason, statusCode: StatusCodes.Status409Conflict);

        public static IResult PreconditionFailedReason(string reason) => Results.Text(reason, statusCode: StatusCodes.Status412PreconditionFailed);
    }

    public static IResult WithETag(this IResult inner, EntityTagHeaderValue etag, DateTimeOffset? lastModified = null)
    {
        return new HeaderEnrichedResult(inner, header =>
        {
            header.ETag = etag;
            if (lastModified.HasValue)
            {
                header.LastModified = lastModified.Value;
            }
        });
    }

    private class HeaderEnrichedResult(IResult inner, Action<ResponseHeaders> headerEnrich) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            headerEnrich.Invoke(httpContext.Response.GetTypedHeaders());
            return inner.ExecuteAsync(httpContext);
        }
    }
}
