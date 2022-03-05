using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Management.Models;
using Xylab.Management.Services;

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
            return hostSystem.GetSystemStatusAsync();
        }

        [HttpGet]
        public Task<List<InstalledPackage>> Dpkg([FromQuery] bool isChroot)
        {
            return hostSystem.GetPackagesAsync(isChroot ? "/chroot/domjudge/" : "/");
        }

        [HttpGet]
        public Task<List<CpuInformation>> Cpu()
        {
            return hostSystem.GetCpusAsync();
        }

        [HttpGet]
        public Task<KernelInformation> Kernel()
        {
            return hostSystem.GetKernelAsync();
        }

        [HttpGet]
        public Task<List<DriveInformation>> Disk([FromQuery] bool fixedOnly = true)
        {
            return hostSystem.GetDrivesAsync(fixedOnly);
        }

        [HttpGet]
        public Task<List<ProcessInformation>> Process()
        {
            return hostSystem.GetProcessesAsync();
        }

        [HttpGet]
        public Task<List<ServiceInformation>> Service()
        {
            return hostSystem.GetServicesAsync();
        }
    }
}
