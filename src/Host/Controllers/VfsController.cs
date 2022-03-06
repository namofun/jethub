using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Xylab.Management.VirtualFileSystem;

namespace JetHub.Controllers
{
    /// https://github.com/Azure-App-Service/KuduLite
    [Route("/api/vfs/{**path}")]
    public class VfsController : VfsControllerImpl
    {
        public VfsController(ILogger<VfsController> logger, IFileSystemV2 fileSystem)
            : base(logger, "/", fileSystem)
        {
        }
    }
}
