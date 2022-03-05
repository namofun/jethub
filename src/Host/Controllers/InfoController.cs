using JetHub.Models;
using JetHub.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JetHub.Controllers
{
    [Route("/api/[controller]/[action]")]
    public class InfoController : ControllerBase
    {
        private readonly IHostSystem hostSystem;

        public InfoController(IHostSystem hostSystem)
        {
            this.hostSystem = hostSystem;
        }

        [HttpGet]
        public Task<SystemInformation> System()
        {
            return hostSystem.GetSystemInformationAsync();
        }

        [HttpGet]
        public Task<List<InstalledPackage>> Dpkg([FromQuery] bool isChroot)
        {
            return hostSystem.GetInstalledPackagesAsync(isChroot ? "/chroot/domjudge/" : "/");
        }

        [HttpGet]
        public Task<List<CpuInformation>> Cpu()
        {
            return hostSystem.GetCpuInformationAsync();
        }

        [HttpGet]
        public Task<KernelInformation> Kernel()
        {
            return hostSystem.GetKernelInformationAsync();
        }

        [HttpGet]
        public Task<List<DriveInformation>> Disk([FromQuery] bool fixedOnly = true)
        {
            return hostSystem.GetDriveInformationAsync(fixedOnly);
        }
    }
}
