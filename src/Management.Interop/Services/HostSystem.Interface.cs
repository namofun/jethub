using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public interface IHostSystem
    {
        Task<SystemInformation> GetSystemInformationAsync();

        Task<List<InstalledPackage>> GetInstalledPackagesAsync(string root = "/");

        Task<List<CpuInformation>> GetCpuInformationAsync();

        Task<KernelInformation> GetKernelInformationAsync();

        Task<List<DriveInformation>> GetDriveInformationAsync(bool fixedOnly = true);
    }
}
