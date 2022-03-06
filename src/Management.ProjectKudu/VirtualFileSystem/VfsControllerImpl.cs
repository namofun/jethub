using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Xylab.Management.VirtualFileSystem
{
    /// <summary>
    /// A Virtual File System controller which exposes GET, PUT, and DELETE for the entire Kudu file system.
    /// </summary>
    public abstract class VfsControllerImpl : VfsControllerBase
    {
        protected VfsControllerImpl(ILogger logger, string rootPath, IFileSystemV2 fileSystem)
            : base(logger, rootPath, fileSystem)
        {
        }

        protected override Task<IActionResult> CreateDirectoryPutResponse(IDirectoryInfo info, string localFilePath)
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

        protected override Task<IActionResult> CreateItemGetResponse(IFileSystemInfo info, string localFilePath)
        {
            return Task.FromResult<IActionResult>(File(
                GetFileReadStream(localFilePath),
                MediaTypeMap.GetMediaType(info.Extension).ToString(),
                info.LastWriteTime,
                CreateEntityTag(info)));
        }

        protected override async Task<IActionResult> CreateItemPutResponse(IFileSystemInfo info, string localFilePath, bool itemExists)
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
                    return StatusCode(412, "Updating an existing resource requires an If-Match header carrying a single, strong ETag.");
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
                    return StatusCode(412, "ETag does not represent the latest state of the resource.");
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
                        return StatusCode(409, $"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
                    }
                }

                // Set updated etag for the file
                info.Refresh();
                ResponseHeaders headers = Response.GetTypedHeaders();
                headers.ETag = CreateEntityTag(info);
                headers.LastModified = info.LastWriteTimeUtc;

                // Return either 204 No Content or 201 Created response
                return StatusCode(itemExists ? 204 : 201);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during returning result: {Message}", ex.Message);
                return StatusCode(409, $"Could not write to local resource '{localFilePath}' due to error '{ex.Message}'.");
            }
        }

        protected override Task<IActionResult> CreateFileDeleteResponse(IFileInfo info)
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

        private static EntityTagHeaderValue CreateEntityTag(IFileSystemInfo sysInfo)
        {
            Contract.Assert(sysInfo != null);

            const string etag_charmap = "0123456789abcdef";
            Span<byte> etag = stackalloc byte[8];
            Span<char> result = stackalloc char[2 + sizeof(long) * 2];
            result[0] = result[17] = '"';
            for (int i = 0; i < 8; i++)
            {
                result[2 * i + 1] = etag_charmap[etag[i] >> 4];
                result[2 * i + 2] = etag_charmap[etag[i] & 15];
            }

            return new EntityTagHeaderValue(new string(result));
        }
    }
}
