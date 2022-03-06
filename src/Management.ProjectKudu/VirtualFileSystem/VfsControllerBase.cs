using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Xylab.Management.VirtualFileSystem
{
    /// <summary>
    /// Provides common functionality for Virtual File System controllers.
    /// </summary>
    public abstract class VfsControllerBase : ControllerBase
    {
        public const char UriSegmentSeparator = '/';
        protected const int BufferSize = 32 * 1024;

        protected readonly IFileSystem FileSystem;
        protected readonly ILogger Logger;
        protected readonly string RootPath;
        protected readonly MediaTypeMap MediaTypeMap;

        protected VfsControllerBase(ILogger logger, string rootPath, IFileSystemV2 fileSystem)
        {
            ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));

            Logger = logger;
            RootPath = Path.GetFullPath(rootPath.TrimEnd(Path.DirectorySeparatorChar));
            MediaTypeMap = MediaTypeMap.Default;
            FileSystem = fileSystem;
        }

        [AcceptVerbs("GET", "HEAD")]
        public virtual Task<IActionResult> GetItem()
        {
            string localFilePath = GetLocalFilePath();
            IDirectoryInfo info = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);

            if (info.Attributes < 0)
            {
                return NotFound($"'{info.FullName}' not found.");
            }
            else if ((info.Attributes & FileAttributes.Directory) != 0)
            {
                // If request URI does NOT end in a "/" then redirect to one that does
                if (!localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
                {
                    UriBuilder location = new(UriHelper.GetRequestUri(Request));
                    location.Path += "/";
                    return RedirectPreserveMethod(location.Uri);
                }
                else
                {
                    return CreateDirectoryGetResponse(info, localFilePath);
                }
            }
            else
            {
                // If request URI ends in a "/" then redirect to one that does not
                if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
                {
                    UriBuilder location = new(UriHelper.GetRequestUri(Request));
                    location.Path = location.Path.TrimEnd(UriSegmentSeparator);
                    return RedirectPreserveMethod(location.Uri);
                }
                else
                {
                    // We are ready to get the file
                    return CreateItemGetResponse(info, localFilePath);
                }
            }
        }

        [HttpPut]
        public virtual Task<IActionResult> PutItem()
        {
            string localFilePath = GetLocalFilePath();
            IDirectoryInfo info = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);
            bool itemExists = info.Attributes >= 0;

            if (itemExists && (info.Attributes & FileAttributes.Directory) != 0)
            {
                return CreateDirectoryPutResponse(info, localFilePath);
            }
            else if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
            {
                // If request URI ends in a "/" then attempt to create the directory.
                return CreateDirectoryPutResponse(info, localFilePath);
            }
            else
            {
                // We are ready to update the file
                return CreateItemPutResponse(info, localFilePath, itemExists);
            }
        }

        [HttpDelete]
        public virtual Task<IActionResult> DeleteItem(bool recursive = false)
        {
            string localFilePath = GetLocalFilePath();
            IDirectoryInfo dirInfo = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);

            if (dirInfo.Attributes < 0)
            {
                return NotFound($"'{dirInfo.FullName}' not found.");
            }
            else if ((dirInfo.Attributes & FileAttributes.Directory) != 0)
            {
                try
                {
                    dirInfo.Delete(recursive);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error during delete item: {Message}", ex.Message);
                    return Conflict("Cannot delete directory. It is either not empty or access is not allowed.");
                }

                // Delete directory succeeded.
                return Okay();
            }
            else if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
            {
                // If request URI ends in a "/" then redirect to one that does not
                UriBuilder location = new(UriHelper.GetRequestUri(Request));
                location.Path = location.Path.TrimEnd(UriSegmentSeparator);
                return RedirectPreserveMethod(location.Uri);
            }
            else
            {
                // We are ready to delete the file
                IFileInfo fileInfo = FileSystem.FileInfo.FromFileName(localFilePath);
                return CreateFileDeleteResponse(fileInfo);
            }
        }

        protected virtual Task<IActionResult> CreateDirectoryGetResponse(IDirectoryInfo info, string localFilePath)
        {
            Contract.Assert(info != null);
            try
            {
                // Enumerate directory
                return Okay(GetDirectoryResponse(info.GetFileSystemInfos()));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during create directory: {Message}", ex.Message);
                return InternalServerError(ex.Message);
            }
        }

        protected abstract Task<IActionResult> CreateItemGetResponse(IFileSystemInfo info, string localFilePath);

        protected virtual Task<IActionResult> CreateDirectoryPutResponse(IDirectoryInfo info, string localFilePath)
        {
            return Conflict("The resource represents a directory which can not be updated.");
        }

        protected abstract Task<IActionResult> CreateItemPutResponse(IFileSystemInfo info, string localFilePath, bool itemExists);

        protected virtual Task<IActionResult> CreateFileDeleteResponse(IFileInfo info)
        {
            // Generate file response
            try
            {
                using (Stream fileStream = GetFileDeleteStream(info))
                {
                    info.Delete();
                }

                return Okay();
            }
            catch (Exception ex)
            {
                // Could not delete the file
                Logger.LogError(ex, "Error during delete files: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Indicates whether this is a conditional range request containing an
        /// If-Range header with a matching etag and a Range header indicating the 
        /// desired ranges
        /// </summary>
        protected bool IsRangeRequest(EntityTagHeaderValue currentEtag)
        {
            RequestHeaders headers = Request.GetTypedHeaders();

            if (headers.Range == null)
            {
                return false;
            }

            if (headers.IfRange != null)
            {
                return headers.IfRange.EntityTag.Compare(currentEtag, false);
            }

            return true;
        }

        /// <summary>
        /// Indicates whether this is a If-None-Match request with a matching etag.
        /// </summary>
        protected bool IsIfNoneMatchRequest(EntityTagHeaderValue currentEtag)
        {
            var headers = Request.GetTypedHeaders();
            return currentEtag != null
                && headers.IfNoneMatch != null
                && headers.IfNoneMatch.Any(entityTag => currentEtag.Compare(entityTag, false));
        }

        /// <summary>
        /// Provides a common way for opening a file stream for shared reading from a file.
        /// </summary>
        protected Stream GetFileReadStream(string localFilePath)
        {
            Contract.Assert(localFilePath != null);

            // Open file exclusively for read-sharing
            return FileSystem.FileStream.Create(localFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufferSize, useAsync: true);
        }

        /// <summary>
        /// Provides a common way for opening a file stream for writing exclusively to a file. 
        /// </summary>
        protected Stream GetFileWriteStream(string localFilePath, bool fileExists)
        {
            Contract.Assert(localFilePath != null);

            // Create path if item doesn't already exist
            if (!fileExists)
            {
                FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
            }

            // Open file exclusively for write without any sharing
            return FileSystem.FileStream.Create(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        }

        /// <summary>
        /// Provides a common way for opening a file stream for exclusively deleting the file. 
        /// </summary>
        private static Stream GetFileDeleteStream(IFileInfo file)
        {
            Contract.Assert(file != null);

            // Open file exclusively for delete sharing only
            return file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }

        private string GetLocalFilePath()
        {
            string path = RouteData.Values["path"] as string;
            if (!string.IsNullOrEmpty(path))
            {
                return FileSystem.Path.GetFullPath(FileSystem.Path.Combine(RootPath, path));
            }

            string reqUri = UriHelper.GetRequestUri(Request).AbsoluteUri.Split('?').First();
            if (reqUri.EndsWith(UriSegmentSeparator))
            {
                return FileSystem.Path.GetFullPath(RootPath + FileSystem.Path.DirectorySeparatorChar);
            }
            else
            {
                return RootPath;
            }
        }

        private IEnumerable<VfsStatEntry> GetDirectoryResponse(IFileSystemInfo[] infos)
        {
            Uri requestUri = UriHelper.GetRequestUri(Request);
            string baseAddress = requestUri.AbsoluteUri.Split('?').First();
            string query = requestUri.Query;

            foreach (IFileSystemInfo fileSysInfo in infos)
            {
                bool isDirectory = (fileSysInfo.Attributes & FileAttributes.Directory) != 0;
                string unescapedHref = isDirectory ? fileSysInfo.Name + UriSegmentSeparator : fileSysInfo.Name;

                yield return new VfsStatEntry
                {
                    Name = fileSysInfo.Name,
                    ModifyTime = fileSysInfo.LastWriteTimeUtc,
                    CreateTime = fileSysInfo.CreationTimeUtc,
                    Mime = (isDirectory ? MediaTypeMap.InodeDirectory : MediaTypeMap.GetMediaType(fileSysInfo.Extension)).ToString(),
                    Size = isDirectory ? 0 : ((IFileInfo)fileSysInfo).Length,
                    Href = (baseAddress + Uri.EscapeUriString(unescapedHref) + query).Replace("#", Uri.EscapeDataString("#")),
                    Path = fileSysInfo.FullName
                };
            }
        }

        protected Task<IActionResult> Okay()
            => Task.FromResult<IActionResult>(Ok());

        protected Task<IActionResult> Okay(object result)
            => Task.FromResult<IActionResult>(Ok(result));

        protected Task<IActionResult> Created()
            => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status201Created));

        protected Task<IActionResult> InternalServerError(object value = null)
            => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status500InternalServerError, value));

        protected Task<IActionResult> NotFound(string reason)
            => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status404NotFound, reason));

        protected Task<IActionResult> Conflict(string reason)
            => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status409Conflict, reason));

        protected Task<IActionResult> PreconditionFailed(string reason)
            => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status412PreconditionFailed, reason));

        protected Task<IActionResult> RedirectPreserveMethod(Uri uri)
            => Task.FromResult<IActionResult>(RedirectPreserveMethod(uri.ToString()));
    }
}
