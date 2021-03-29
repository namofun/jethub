using JetHub.Models;
using JetHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace JetHub.Controllers
{
    /// <remarks>
    /// REF: https://github.com/Azure-App-Service/KuduLite
    /// </remarks>
    [Route("/api/vfs/{**path}")]
    public class VfsController : ControllerBase
    {
        internal string GetLocalFilePath(string path)
        {
            var sysInfo = HttpContext.RequestServices.GetRequiredService<ISystemInfo>();
            string result = sysInfo.GetVfsRoot();

            if (!string.IsNullOrEmpty(path))
            {
                result = Path.GetFullPath(Path.Combine(result, path));
            }
            else if (!Request.Path.Value.EndsWith('/'))
            {
                result = Path.GetFullPath(result + Path.DirectorySeparatorChar);
            }

            return result;
        }


        [AcceptVerbs("GET", "HEAD")]
        public virtual IActionResult GetItem(string path)
        {
            if (path?.StartsWith('/') ?? false) return BadRequest();
            string localFilePath = GetLocalFilePath(path);
            IDirectoryInfo info = new FileSystem().DirectoryInfo.FromDirectoryName(localFilePath);

            if (info.Attributes < 0)
            {
                return NotFound(new { error = string.Format("'{0}' not found.", info.FullName) });
            }
            else if ((info.Attributes & FileAttributes.Directory) != 0)
            {
                // If request URI does NOT end in a "/" then redirect to one that does
                if (!Request.Path.Value.EndsWith('/'))
                {
                    return RedirectPreserveMethod(Request.Path.Value + "/");
                }
                else
                {
                    return Ok(info.EnumerateFileSystemInfos().Select(fileSysInfo =>
                    {
                        bool isDirectory = (fileSysInfo.Attributes & FileAttributes.Directory) != 0;
                        string mime = isDirectory ? "inode/directory" : MediaTypeMap.Default.GetMediaType(fileSysInfo.Extension).ToString();
                        string unescapedHref = isDirectory ? fileSysInfo.Name + '/' : fileSysInfo.Name;
                        long size = isDirectory ? 0 : ((IFileInfo)fileSysInfo).Length;

                        return new VfsStatEntry
                        {
                            Name = fileSysInfo.Name,
                            ModifyTime = fileSysInfo.LastWriteTimeUtc,
                            CreateTime = fileSysInfo.CreationTimeUtc,
                            Mime = mime,
                            Size = size,
                            Href = Url.Action(new { path = path + unescapedHref }),
                            Path = fileSysInfo.FullName
                        };
                    }));
                }
            }
            else
            {
                // If request URI ends in a "/" then redirect to one that does not
                if (Request.Path.Value.EndsWith('/'))
                {
                    return RedirectPreserveMethod(Request.Path.Value[0..^1]);
                }

                // We are ready to get the file
                return File(
                    new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 32 * 1024, useAsync: true),
                    MediaTypeMap.Default.GetMediaType(info.Extension).ToString(),
                    info.LastWriteTime,
                    info.CreateEntityTag());
            }
        }


        /*
        [HttpPut]
        public virtual Task<IActionResult> PutItem()
        {
            string localFilePath = GetLocalFilePath();

            DirectoryInfoBase info = FileSystemHelpers.DirectoryInfoFromDirectoryName(localFilePath);
            bool itemExists = info.Attributes >= 0;

            if (itemExists && (info.Attributes & FileAttributes.Directory) != 0)
            {
                return CreateDirectoryPutResponse(info, localFilePath);
            }
            else
            {
                // If request URI ends in a "/" then attempt to create the directory.
                if (localFilePath[localFilePath.Length - 1] == Path.DirectorySeparatorChar)
                {
                    return CreateDirectoryPutResponse(info, localFilePath);
                }

                // We are ready to update the file
                return CreateItemPutResponse(info, localFilePath, itemExists);
            }
        }


        [HttpDelete]
        public virtual Task<IActionResult> DeleteItem(bool recursive = false)
        {
            string localFilePath = GetLocalFilePath();

            DirectoryInfoBase dirInfo = FileSystemHelpers.DirectoryInfoFromDirectoryName(localFilePath);

            if (dirInfo.Attributes < 0)
            {
                return Task.FromResult((IActionResult)NotFound(String.Format("'{0}' not found.", dirInfo.FullName)));
            }
            else if ((dirInfo.Attributes & FileAttributes.Directory) != 0)
            {
                try
                {
                    dirInfo.Delete(recursive);
                }
                catch (Exception ex)
                {
                    Tracer.TraceError(ex);
                    return Task.FromResult((IActionResult)StatusCode(StatusCodes.Status409Conflict, Resources.VfsControllerBase_CannotDeleteDirectory));
                }

                // Delete directory succeeded.
                return Task.FromResult((IActionResult)Ok());
            }
            else
            {
                // If request URI ends in a "/" then redirect to one that does not
                if (localFilePath[localFilePath.Length - 1] == Path.DirectorySeparatorChar)
                {
                    UriBuilder location = new UriBuilder(UriHelper.GetRequestUri(Request));
                    location.Path = location.Path.TrimEnd(_uriSegmentSeparator);
                    return Task.FromResult((IActionResult)RedirectPreserveMethod(location.Uri.ToString()));
                }

                // We are ready to delete the file
                var fileInfo = FileSystemHelpers.FileInfoFromFileName(localFilePath);
                return CreateFileDeleteResponse(fileInfo);
            }
        }
        */
    }
}
