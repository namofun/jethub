namespace Xylab.Management.SystemInformation;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xylab.Management.Services;

public class SysInfoEndpoint(IHostSystem hostSystem)
{
    public async Task<IResult> Status()
    {
        return Results.Ok(await hostSystem.GetSystemStatusAsync());
    }

    public async Task<IResult> Packages()
    {
        if (OperatingSystem.IsLinux())
        {
            return Results.Ok(await hostSystem.GetPackagesAsync("/"));
        }
        else
        {
            return Results.BadRequest(new { error = "DPKG is only supported on Ubuntu." });
        }
    }

    public async Task<IResult> Cpu()
    {
        return Results.Ok(await hostSystem.GetCpusAsync());
    }

    public async Task<IResult> Kernel()
    {
        return Results.Ok(await hostSystem.GetKernelAsync());
    }

    public async Task<IResult> Disks([FromQuery] bool fixedOnly = true)
    {
        return Results.Ok(await hostSystem.GetDrivesAsync(fixedOnly));
    }

    public async Task<IResult> Processes()
    {
        return Results.Ok(await hostSystem.GetProcessesAsync());
    }

    public async Task<IResult> Services()
    {
        return Results.Ok(await hostSystem.GetServicesAsync());
    }
}
