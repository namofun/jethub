/// <summary>
///   Modified from https://github.com/Azure-App-Service/KuduLite/blob/dev/Kudu.Services/Infrastructure/VfsControllerBase.cs
/// </summary>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Xylab.Management.VirtualFileSystem;

/// <summary>
/// Provides common functionality for Virtual File System controllers.
/// </summary>
public abstract class VfsEndpointBase
{
    public const char UriSegmentSeparator = '/';
    protected const int BufferSize = 32 * 1024;

    protected IFileSystem FileSystem { get; }

    protected ILogger Logger { get; }

    protected string RootPath { get; }

    protected MediaTypeMap MediaTypeMap { get; }

    protected VfsEndpointBase(ILogger logger, string rootPath, IFileSystemV2 fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));

        Logger = logger;
        RootPath = Path.GetFullPath(rootPath.TrimEnd(Path.DirectorySeparatorChar));
        MediaTypeMap = MediaTypeMap.Default;
        FileSystem = fileSystem;
    }

    public virtual async Task<IResult> GetItem(string path, VfsRequest request)
    {
        string localFilePath = GetLocalFilePath(path, request);
        IDirectoryInfo info = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);

        if (info.Attributes < 0)
        {
            return Results.NotFoundReason($"'{info.FullName}' not found.");
        }
        else if ((info.Attributes & FileAttributes.Directory) != 0)
        {
            // If request URI does NOT end in a "/" then redirect to one that does
            if (!localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
            {
                UriBuilder location = new(request.Uri);
                location.Path += "/";
                return Results.RedirectPreserveMethod(location.Uri);
            }
            else
            {
                return await CreateDirectoryGetResponse(request, info, localFilePath);
            }
        }
        else
        {
            // If request URI ends in a "/" then redirect to one that does not
            if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
            {
                UriBuilder location = new(request.Uri);
                location.Path = location.Path.TrimEnd(UriSegmentSeparator);
                return Results.RedirectPreserveMethod(location.Uri);
            }
            else
            {
                // We are ready to get the file
                return await CreateItemGetResponse(request, info, localFilePath);
            }
        }
    }

    public virtual Task<IResult> PutItem(string path, VfsRequest request)
    {
        string localFilePath = GetLocalFilePath(path, request);
        IDirectoryInfo info = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);
        bool itemExists = info.Attributes >= 0;

        if (itemExists && (info.Attributes & FileAttributes.Directory) != 0)
        {
            return CreateDirectoryPutResponse(request, info, localFilePath);
        }
        else if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
        {
            // If request URI ends in a "/" then attempt to create the directory.
            return CreateDirectoryPutResponse(request, info, localFilePath);
        }
        else
        {
            // We are ready to update the file
            return CreateItemPutResponse(request, info, localFilePath, itemExists);
        }
    }

    public virtual async Task<IResult> DeleteItem(string path, VfsRequest request, bool recursive = false)
    {
        string localFilePath = GetLocalFilePath(path, request);
        IDirectoryInfo dirInfo = FileSystem.DirectoryInfo.FromDirectoryName(localFilePath);

        if (dirInfo.Attributes < 0)
        {
            return Results.NotFoundReason($"'{dirInfo.FullName}' not found.");
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
                return Results.ConflictReason("Cannot delete directory. It is either not empty or access is not allowed.");
            }

            // Delete directory succeeded.
            return Results.Ok();
        }
        else if (localFilePath.EndsWith(FileSystem.Path.DirectorySeparatorChar))
        {
            // If request URI ends in a "/" then redirect to one that does not
            UriBuilder location = new(request.Uri);
            location.Path = location.Path.TrimEnd(UriSegmentSeparator);
            return Results.RedirectPreserveMethod(location.Uri);
        }
        else
        {
            // We are ready to delete the file
            IFileInfo fileInfo = FileSystem.FileInfo.FromFileName(localFilePath);
            return await CreateFileDeleteResponse(request, fileInfo);
        }
    }

    protected virtual Task<IResult> CreateDirectoryGetResponse(VfsRequest request, IDirectoryInfo info, string localFilePath)
    {
        Contract.Assert(info != null);
        try
        {
            // Enumerate directory
            return Task.FromResult(Results.Ok(GetDirectoryResponse(request, info.GetFileSystemInfos())));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during create directory: {Message}", ex.Message);
            return Task.FromResult(Results.InternalServerErrorReason(ex.Message));
        }
    }

    protected abstract Task<IResult> CreateItemGetResponse(VfsRequest request, IFileSystemInfo info, string localFilePath);

    protected virtual Task<IResult> CreateDirectoryPutResponse(VfsRequest request, IDirectoryInfo info, string localFilePath)
    {
        return Task.FromResult(Results.ConflictReason("The resource represents a directory which can not be updated."));
    }

    protected abstract Task<IResult> CreateItemPutResponse(VfsRequest request, IFileSystemInfo info, string localFilePath, bool itemExists);

    protected virtual Task<IResult> CreateFileDeleteResponse(VfsRequest request, IFileInfo info)
    {
        // Generate file response
        try
        {
            using (Stream fileStream = GetFileDeleteStream(info))
            {
                info.Delete();
            }

            return Task.FromResult(Results.Ok());
        }
        catch (Exception ex)
        {
            // Could not delete the file
            Logger.LogError(ex, "Error during delete files: {Message}", ex.Message);
            return Task.FromResult(Results.NotFoundReason(ex.Message));
        }
    }

    /// <summary>
    /// Indicates whether this is a conditional range request containing an
    /// If-Range header with a matching etag and a Range header indicating the 
    /// desired ranges
    /// </summary>
    protected bool IsRangeRequest(EntityTagHeaderValue currentEtag, VfsRequest request)
    {
        if (request.Headers.Range == null)
        {
            return false;
        }

        if (request.Headers.IfRange != null)
        {
            return request.Headers.IfRange.EntityTag.Compare(currentEtag, false);
        }

        return true;
    }

    /// <summary>
    /// Indicates whether this is a If-None-Match request with a matching etag.
    /// </summary>
    protected bool IsIfNoneMatchRequest(EntityTagHeaderValue currentEtag, VfsRequest request)
    {
        return currentEtag != null
            && request.Headers.IfNoneMatch != null
            && request.Headers.IfNoneMatch.Any(entityTag => currentEtag.Compare(entityTag, false));
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

    private string GetLocalFilePath(string path, VfsRequest request)
    {
        if (!string.IsNullOrEmpty(path))
        {
            return FileSystem.Path.GetFullPath(FileSystem.Path.Combine(RootPath, path));
        }

        string reqUri = request.Uri.AbsoluteUri.Split('?').First();
        if (reqUri.EndsWith(UriSegmentSeparator))
        {
            return FileSystem.Path.GetFullPath(RootPath + FileSystem.Path.DirectorySeparatorChar);
        }
        else
        {
            return RootPath;
        }
    }

    private IEnumerable<VfsStatEntry> GetDirectoryResponse(VfsRequest request, IFileSystemInfo[] infos)
    {
        string baseAddress = request.Uri.AbsoluteUri.Split('?').First();
        string query = request.Uri.Query;

        if (!baseAddress.EndsWith(UriSegmentSeparator)) baseAddress += UriSegmentSeparator;
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
}
