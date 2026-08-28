namespace Xylab.Management.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Management.Models;
public interface IHostSystem
{
    Task<SystemInformation> GetSystemStatusAsync();

    Task<List<InstalledPackage>> GetPackagesAsync(string root = "/");

    Task<List<CpuInformation>> GetCpusAsync();

    Task<KernelInformation> GetKernelAsync();

    Task<List<DriveInformation>> GetDrivesAsync(bool fixedOnly = true);

    Task<List<ProcessInformation>> GetProcessesAsync();

    Task<List<ServiceInformation>> GetServicesAsync();

    public static IHostSystem CreateDefault()
    {
        if (OperatingSystem.IsLinux())
        {
            return new LinuxSystem();
        }
        else if (OperatingSystem.IsWindows())
        {
            return new WindowsSystem();
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }
}
