namespace Xylab.Management.Services;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Management.Infrastructure;
using Xylab.Management.Models;

[SupportedOSPlatform("windows")]
public sealed class WindowsSystem : IHostSystem
{
    private const string CimNamespace = @"root\cimv2";
    private const int ErrorInsufficientBuffer = 122;

    private readonly object _processQueryLock = new();
    private Task<List<ProcessInformation>> _processQuery;

    public Task<List<CpuInformation>> GetCpusAsync()
    {
        List<CpuInformation> cpus = new();
        int processorId = 0;
        int physicalId = 0;

        using CimSession session = CimSession.Create(null);
        foreach (CimInstance processor in session.QueryInstances(
            CimNamespace,
            "WQL",
            "SELECT Name, CurrentClockSpeed, L2CacheSize, L3CacheSize, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
        {
            using (processor)
            {
                int coreCount = Math.Max(1, GetInt32(processor, "NumberOfCores"));
                int logicalProcessorCount = Math.Max(coreCount, GetInt32(processor, "NumberOfLogicalProcessors"));
                uint cacheSize = GetUInt32(processor, "L3CacheSize");
                if (cacheSize == 0)
                {
                    cacheSize = GetUInt32(processor, "L2CacheSize");
                }

                for (int logicalProcessor = 0; logicalProcessor < logicalProcessorCount; logicalProcessor++)
                {
                    cpus.Add(new CpuInformation
                    {
                        ProcessorId = processorId++,
                        ModelName = GetString(processor, "Name"),
                        PhysicalId = physicalId,
                        CoreId = logicalProcessor % coreCount,
                        ClockSpeed = $"{GetUInt32(processor, "CurrentClockSpeed")} MHz",
                        CacheSize = $"{cacheSize} KB",
                    });
                }
            }

            physicalId++;
        }

        return Task.FromResult(cpus);
    }

    public Task<List<DriveInformation>> GetDrivesAsync(bool fixedOnly = true)
    {
        List<DriveInformation> drives = new();
        string condition = fixedOnly ? " WHERE DriveType = 3" : string.Empty;

        using CimSession session = CimSession.Create(null);
        foreach (CimInstance disk in session.QueryInstances(
            CimNamespace,
            "WQL",
            "SELECT DeviceID, DriveType, FileSystem, FreeSpace, Size FROM Win32_LogicalDisk" + condition))
        {
            using (disk)
            {
                ulong totalSize = GetUInt64(disk, "Size");
                ulong freeSpace = GetUInt64(disk, "FreeSpace");
                drives.Add(new DriveInformation
                {
                    Category = GetDriveType(GetUInt32(disk, "DriveType")),
                    Type = GetString(disk, "FileSystem"),
                    FileSystem = GetString(disk, "DeviceID"),
                    MountPoint = GetString(disk, "DeviceID") + Path.DirectorySeparatorChar,
                    TotalSizeBytes = totalSize,
                    UsedSizeBytes = totalSize >= freeSpace ? totalSize - freeSpace : 0,
                });
            }
        }

        return Task.FromResult(drives);
    }

    public Task<List<InstalledPackage>> GetPackagesAsync(string root = "/")
    {
        return Task.FromResult(new List<InstalledPackage>());
    }

    public Task<KernelInformation> GetKernelAsync()
    {
        return Task.FromResult(new KernelInformation
        {
            Version = Environment.OSVersion.Version.ToString(),
        });
    }

    public Task<List<ProcessInformation>> GetProcessesAsync()
    {
        lock (_processQueryLock)
        {
            if (_processQuery == null || _processQuery.IsCompleted)
            {
                _processQuery = Task.Run(GetProcesses);
            }

            return _processQuery;
        }
    }

    private static List<ProcessInformation> GetProcesses()
    {
        List<ProcessInformation> processes = new();

        using CimSession session = CimSession.Create(null);
        foreach (CimInstance process in session.QueryInstances(
            CimNamespace,
            "WQL",
            "SELECT Name, ProcessId, ThreadCount, WorkingSetSize, KernelModeTime, UserModeTime, CommandLine FROM Win32_Process"))
        {
            using (process)
            {
                int processId = GetInt32(process, "ProcessId");
                ulong cpuTime = GetUInt64(process, "KernelModeTime") + GetUInt64(process, "UserModeTime");
                processes.Add(new ProcessInformation
                {
                    Id = processId,
                    Name = GetString(process, "Name"),
                    ThreadCount = GetInt32(process, "ThreadCount"),
                    WorkingSet = GetInt64(process, "WorkingSetSize"),
                    TotalCpuTime = TimeSpan.FromTicks(cpuTime > long.MaxValue ? long.MaxValue : (long)cpuTime),
                    User = GetProcessOwner(processId),
                    CommandLine = GetString(process, "CommandLine"),
                });
            }
        }

        return processes;
    }

    public Task<List<ServiceInformation>> GetServicesAsync()
    {
        List<ServiceInformation> services = new();

        using CimSession session = CimSession.Create(null);
        foreach (CimInstance service in session.QueryInstances(
            CimNamespace,
            "WQL",
            "SELECT Name, DisplayName, Started, State, Status FROM Win32_Service"))
        {
            using (service)
            {
                services.Add(new ServiceInformation
                {
                    Name = GetString(service, "Name"),
                    Description = GetString(service, "DisplayName"),
                    LoadState = GetString(service, "Status").ToLowerInvariant(),
                    ActiveState = GetBoolean(service, "Started") ? "active" : "inactive",
                    SubState = GetString(service, "State").ToLowerInvariant(),
                });
            }
        }

        return Task.FromResult(services);
    }

    public Task<SystemInformation> GetSystemStatusAsync()
    {
        using CimSession session = CimSession.Create(null);
        foreach (CimInstance operatingSystem in session.QueryInstances(
            CimNamespace,
            "WQL",
            "SELECT LastBootUpTime, TotalVisibleMemorySize, FreePhysicalMemory, TotalVirtualMemorySize, FreeVirtualMemory FROM Win32_OperatingSystem"))
        {
            using (operatingSystem)
            {
                ulong totalMemory = GetUInt64(operatingSystem, "TotalVisibleMemorySize") * 1024;
                ulong freeMemory = GetUInt64(operatingSystem, "FreePhysicalMemory") * 1024;
                ulong totalVirtualMemory = GetUInt64(operatingSystem, "TotalVirtualMemorySize") * 1024;
                ulong freeVirtualMemory = GetUInt64(operatingSystem, "FreeVirtualMemory") * 1024;
                ulong totalSwap = totalVirtualMemory >= totalMemory ? totalVirtualMemory - totalMemory : 0;
                ulong freeSwap = freeVirtualMemory >= freeMemory ? freeVirtualMemory - freeMemory : 0;
                DateTime bootTime = GetDateTime(operatingSystem, "LastBootUpTime");

                return Task.FromResult(new SystemInformation
                {
                    Uptime = DateTime.UtcNow - bootTime.ToUniversalTime(),
                    LoadAverages = null,
                    TotalMemoryBytes = totalMemory,
                    UsedMemoryBytes = totalMemory >= freeMemory ? totalMemory - freeMemory : 0,
                    TotalSwapBytes = totalSwap,
                    UsedSwapBytes = totalSwap >= freeSwap ? totalSwap - freeSwap : 0,
                });
            }
        }

        throw new InvalidOperationException("CIM did not return a Win32_OperatingSystem instance.");
    }

    private static string GetProcessOwner(int processId)
    {
        if (processId == 0 || processId == 4)
        {
            return "NT AUTHORITY\\SYSTEM";
        }

        using var processHandle = Interop.Winapi.OpenProcess(
            Interop.Winapi.ProcessQueryLimitedInformation,
            false,
            processId);
        if (processHandle.IsInvalid)
        {
            return GetUnavailableOwner();
        }

        if (!Interop.Winapi.OpenProcessToken(processHandle, Interop.Winapi.TokenQuery, out var tokenHandle))
        {
            tokenHandle?.Dispose();
            return GetUnavailableOwner();
        }

        using (tokenHandle)
        {
            Interop.Winapi.GetTokenInformation(
                tokenHandle,
                Interop.Winapi.TokenInformationClass.TokenUser,
                IntPtr.Zero,
                0,
                out uint tokenInformationLength);
            int error = Marshal.GetLastWin32Error();
            if (error != ErrorInsufficientBuffer)
            {
                return GetUnavailableOwner(error);
            }

            IntPtr tokenInformation = Marshal.AllocHGlobal(checked((int)tokenInformationLength));
            try
            {
                if (!Interop.Winapi.GetTokenInformation(
                    tokenHandle,
                    Interop.Winapi.TokenInformationClass.TokenUser,
                    tokenInformation,
                    tokenInformationLength,
                    out _))
                {
                    return GetUnavailableOwner();
                }

                Interop.Winapi.TokenUser tokenUser =
                    Marshal.PtrToStructure<Interop.Winapi.TokenUser>(tokenInformation);
                return LookupAccountName(tokenUser.User.Sid);
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }
    }

    private static string LookupAccountName(IntPtr sid)
    {
        uint accountNameLength = 0;
        uint domainNameLength = 0;
        Interop.Winapi.LookupAccountSid(
            null,
            sid,
            null,
            ref accountNameLength,
            null,
            ref domainNameLength,
            out _);
        int error = Marshal.GetLastWin32Error();
        if (error != ErrorInsufficientBuffer)
        {
            return GetUnavailableOwner(error);
        }

        StringBuilder accountName = new(checked((int)accountNameLength));
        StringBuilder domainName = new(checked((int)domainNameLength));
        if (!Interop.Winapi.LookupAccountSid(
            null,
            sid,
            accountName,
            ref accountNameLength,
            domainName,
            ref domainNameLength,
            out _))
        {
            return GetUnavailableOwner();
        }

        return domainName.Length == 0 ? accountName.ToString() : $"{domainName}\\{accountName}";
    }

    private static string GetUnavailableOwner()
    {
        return GetUnavailableOwner(Marshal.GetLastWin32Error());
    }

    private static string GetUnavailableOwner(int error)
    {
        return $"Unavailable ({error}: {new Win32Exception(error).Message})";
    }

    private static object GetValue(CimInstance instance, string propertyName)
    {
        return instance.CimInstanceProperties[propertyName]?.Value;
    }

    private static string GetString(CimInstance instance, string propertyName)
    {
        return Convert.ToString(GetValue(instance, propertyName), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool GetBoolean(CimInstance instance, string propertyName)
    {
        return Convert.ToBoolean(GetValue(instance, propertyName), CultureInfo.InvariantCulture);
    }

    private static int GetInt32(CimInstance instance, string propertyName)
    {
        return Convert.ToInt32(GetValue(instance, propertyName), CultureInfo.InvariantCulture);
    }

    private static long GetInt64(CimInstance instance, string propertyName)
    {
        return Convert.ToInt64(GetValue(instance, propertyName), CultureInfo.InvariantCulture);
    }

    private static uint GetUInt32(CimInstance instance, string propertyName)
    {
        return Convert.ToUInt32(GetValue(instance, propertyName), CultureInfo.InvariantCulture);
    }

    private static ulong GetUInt64(CimInstance instance, string propertyName)
    {
        return Convert.ToUInt64(GetValue(instance, propertyName), CultureInfo.InvariantCulture);
    }

    private static DateTime GetDateTime(CimInstance instance, string propertyName)
    {
        object value = GetValue(instance, propertyName);
        if (value is DateTime dateTime)
        {
            return dateTime;
        }

        return DateTime.Parse(
            Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException($"CIM property '{propertyName}' is null."),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    private static DriveType GetDriveType(uint driveType)
    {
        return driveType <= (uint)DriveType.Ram ? (DriveType)driveType : DriveType.Unknown;
    }
}
