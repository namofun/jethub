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
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

/// <summary>
/// A Virtual File System controller which exposes GET, PUT, and DELETE for the entire Kudu file system.
/// </summary>
public class VfsEndpoint(ILogger<VfsEndpoint> logger, string rootPath, IFileSystemV2 fileSystem)
    : VfsEndpointBase(logger, rootPath, fileSystem)
{
    protected override async Task<IResult> CreateDirectoryPutResponse(VfsRequest request, IDirectoryInfo info, string localFilePath)
    {
        if (info != null && info.Exists)
        {
            // Return a conflict result
            return await base.CreateDirectoryPutResponse(request, info, localFilePath);
        }

        try
        {
            info.Create();
        }
        catch (IOException ex)
        {
            Logger.LogError(ex, "Error during create directory: {Message}", ex.Message);
            return Results.ConflictReason("Cannot delete directory. It is either not empty or access is not allowed.");
        }

        // Return 201 Created response
        return Results.Created();
    }

    protected override Task<IResult> CreateItemGetResponse(VfsRequest request, IFileSystemInfo info, string localFilePath)
    {
        return Task.FromResult(
            Results.File(
                GetFileReadStream(localFilePath),
                contentType: MediaTypeMap.GetMediaType(info.Extension).ToString(),
                lastModified: info.LastWriteTime,
                entityTag: CreateEntityTag(info)));
    }

    protected override async Task<IResult> CreateItemPutResponse(VfsRequest request, IFileSystemInfo info, string localFilePath, bool itemExists)
    {
        // Check that we have a matching conditional If-Match request for existing resources
        if (itemExists)
        {
            // Get current etag
            EntityTagHeaderValue currentEtag = CreateEntityTag(info);

            // Existing resources require an etag to be updated.
            if (request.Headers.IfMatch == null || request.Headers.IfMatch.Count == 0)
            {
                return Results.PreconditionFailedReason("Updating an existing resource requires an If-Match header carrying a single, strong ETag.");
            }

            bool isMatch = false;
            foreach (EntityTagHeaderValue etag in request.Headers.IfMatch)
            {
                if (currentEtag.Compare(etag, false) || etag == EntityTagHeaderValue.Any)
                {
                    isMatch = true;
                    break;
                }
            }

            if (!isMatch)
            {
                return Results.PreconditionFailedReason("ETag does not represent the latest state of the resource.").WithETag(currentEtag);
            }
        }

        // Save file
        try
        {
            using (Stream fileStream = GetFileWriteStream(localFilePath, fileExists: itemExists))
            {
                try
                {
                    await request.Body.CopyToAsync(fileStream);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error during copying file content: {Message}", ex.Message);
                    return Results.ConflictReason($"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
                }
            }

            // Set updated etag for the file
            info.Refresh();

            // Return either 204 No Content or 201 Created response
            return Results.StatusCode(itemExists ? 204 : 201).WithETag(CreateEntityTag(info), info.LastWriteTimeUtc);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during returning result: {Message}", ex.Message);
            return Results.ConflictReason($"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
        }
    }

    protected override async Task<IResult> CreateFileDeleteResponse(VfsRequest request, IFileInfo info)
    {
        // Existing resources require an etag to be updated.
        // CORE TODO double check semantics of what you get from GetTypedHeaders() (empty strings vs null, etc.)
        if (request.Headers.IfMatch == null)
        {
            return Results.PreconditionFailedReason("Updating an existing resource requires an If-Match header carrying a single, strong ETag.");
        }

        // Get current etag
        EntityTagHeaderValue currentEtag = CreateEntityTag(info);
        bool isMatch = request.Headers.IfMatch.Any(etag => etag == EntityTagHeaderValue.Any || currentEtag.Equals(etag));

        if (!isMatch)
        {
            return Results.ConflictReason("ETag does not represent the latest state of the resource.").WithETag(currentEtag);
        }

        return await base.CreateFileDeleteResponse(request, info);
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
