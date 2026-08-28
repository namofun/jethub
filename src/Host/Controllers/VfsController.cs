namespace Xylab.Management.Controllers;

using System.IO.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xylab.Management.VirtualFileSystem;

[Route("files")]
public class VfsController : VfsControllerImpl
{
    public VfsController(ILogger<VfsController> logger, IFileSystemV2 fileSystem)
        : base(logger, "/", fileSystem)
    {
    }
}
