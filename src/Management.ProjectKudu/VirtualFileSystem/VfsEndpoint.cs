/// <summary>
///   Modified from https://github.com/Azure-App-Service/KuduLite/blob/dev/Kudu.Services/Editor/VfsController.cs
/// </summary>

namespace Xylab.Management.VirtualFileSystem;

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

/// <summary>
/// A Virtual File System controller which exposes GET, PUT, and DELETE for the entire Kudu file system.
/// </summary>
public class VfsEndpoint(ILogger<VfsEndpoint> logger, string rootPath, IFileSystemV2 fileSystem, IHttpContextAccessor httpContextAccessor)
    : VfsEndpointBase(logger, rootPath, fileSystem, httpContextAccessor)
{
    protected override Task<IResult> CreateDirectoryPutResponse(IDirectoryInfo info, string localFilePath)
    {
        if (info != null && info.Exists)
        {
            // Return a conflict result
            return base.CreateDirectoryPutResponse(info, localFilePath);
        }

        try
        {
            info.Create();
        }
        catch (IOException ex)
        {
            Logger.LogError(ex, "Error during create directory: {Message}", ex.Message);
            return Conflict("Cannot delete directory. It is either not empty or access is not allowed.");
        }

        // Return 201 Created response
        return Created();
    }

    protected override Task<IResult> CreateItemGetResponse(IFileSystemInfo info, string localFilePath)
    {
        return Task.FromResult(
            Results.File(
                GetFileReadStream(localFilePath),
                contentType: MediaTypeMap.GetMediaType(info.Extension).ToString(),
                lastModified: info.LastWriteTime,
                entityTag: CreateEntityTag(info)));
    }

    protected override async Task<IResult> CreateItemPutResponse(IFileSystemInfo info, string localFilePath, bool itemExists)
    {
        // Check that we have a matching conditional If-Match request for existing resources
        if (itemExists)
        {
            var requestHeaders = Request.GetTypedHeaders();
            var responseHeaders = Response.GetTypedHeaders();

            // Get current etag
            EntityTagHeaderValue currentEtag = CreateEntityTag(info);

            // Existing resources require an etag to be updated.
            if (requestHeaders.IfMatch == null)
            {
                return await PreconditionFailed("Updating an existing resource requires an If-Match header carrying a single, strong ETag.");
            }

            bool isMatch = false;
            foreach (EntityTagHeaderValue etag in requestHeaders.IfMatch)
            {
                if (currentEtag.Compare(etag, false) || etag == EntityTagHeaderValue.Any)
                {
                    isMatch = true;
                    break;
                }
            }

            if (!isMatch)
            {
                responseHeaders.ETag = currentEtag;
                return await PreconditionFailed("ETag does not represent the latest state of the resource.");
            }
        }

        // Save file
        try
        {
            using (Stream fileStream = GetFileWriteStream(localFilePath, fileExists: itemExists))
            {
                try
                {
                    await Request.Body.CopyToAsync(fileStream);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error during copying file content: {Message}", ex.Message);
                    return await Conflict($"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
                }
            }

            // Set updated etag for the file
            info.Refresh();
            ResponseHeaders headers = Response.GetTypedHeaders();
            headers.ETag = CreateEntityTag(info);
            headers.LastModified = info.LastWriteTimeUtc;

            // Return either 204 No Content or 201 Created response
            return Results.StatusCode(itemExists ? 204 : 201);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during returning result: {Message}", ex.Message);
            return await Conflict($"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
        }
    }

    protected override Task<IResult> CreateFileDeleteResponse(IFileInfo info)
    {
        // Existing resources require an etag to be updated.
        var requestHeaders = Request.GetTypedHeaders();

        // CORE TODO double check semantics of what you get from GetTypedHeaders() (empty strings vs null, etc.)
        if (requestHeaders.IfMatch == null)
        {
            return PreconditionFailed("Updating an existing resource requires an If-Match header carrying a single, strong ETag.");
        }

        // Get current etag
        EntityTagHeaderValue currentEtag = CreateEntityTag(info);
        bool isMatch = requestHeaders.IfMatch.Any(etag => etag == EntityTagHeaderValue.Any || currentEtag.Equals(etag));

        if (!isMatch)
        {
            Response.GetTypedHeaders().ETag = currentEtag;
            return Conflict("ETag does not represent the latest state of the resource.");
        }

        return base.CreateFileDeleteResponse(info);
    }

    /// <summary>
    /// Create unique etag based on the last modified UTC time
    /// </summary>
    private static EntityTagHeaderValue CreateEntityTag(IFileSystemInfo sysInfo)
    {
        Contract.Assert(sysInfo != null);

        const string etag_charmap = "0123456789ABCDEF";
        Span<byte> etag = stackalloc byte[8];
        BitConverter.TryWriteBytes(etag, sysInfo.LastWriteTimeUtc.Ticks);
        Span<char> result = stackalloc char[4 + sizeof(long) * 2];
        result[0] = result[19] = '"'; result[1] = '0'; result[2] = 'x';
        for (int i = 0; i < 8; i++)
        {
            result[2 * i + 3] = etag_charmap[etag[i] >> 4];
            result[2 * i + 4] = etag_charmap[etag[i] & 15];
        }

        return new EntityTagHeaderValue(new string(result));
    }
}
