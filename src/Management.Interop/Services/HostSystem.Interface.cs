using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemStatusAsync();

        Task<List<InstalledPackage>> GetPackagesAsync(string root = "/");

        Task<List<CpuInformation>> GetCpusAsync();

        Task<KernelInformation> GetKernelAsync();

        Task<List<DriveInformation>> GetDrivesAsync(bool fixedOnly = true);

        Task<List<ProcessInformation>> GetProcessesAsync();

        Task<List<ServiceInformation>> GetServicesAsync();
    }
}
