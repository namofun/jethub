namespace Xylab.Management.Services;

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Xylab.Management.Models;
public interface IHostSystem
{
    Task<SystemInformation> GetSystemStatusAsync();

    [SupportedOSPlatform("linux")]
    Task<List<InstalledPackage>> GetPackagesAsync(string root = "/");

    Task<List<CpuInformation>> GetCpusAsync();

    Task<KernelInformation> GetKernelAsync();

    Task<List<DriveInformation>> GetDrivesAsync(bool fixedOnly = true);

    Task<List<ProcessInformation>> GetProcessesAsync();

    Task<List<ServiceInformation>> GetServicesAsync();
}
