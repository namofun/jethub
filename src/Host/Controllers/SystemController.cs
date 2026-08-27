namespace Xylab.Management.Controllers;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xylab.Management.Models;
using Xylab.Management.Services;

[Route("system")]
public class SystemController(IHostSystem hostSystem) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SystemInformation>> Status()
    {
        return await hostSystem.GetSystemStatusAsync();
    }

    [HttpGet("dpkg")]
    public async Task<ActionResult<List<InstalledPackage>>> Dpkg([FromQuery] bool isChroot)
    {
        if (OperatingSystem.IsLinux())
        {
            return await hostSystem.GetPackagesAsync(isChroot ? "/chroot/domjudge/" : "/");
        }
        else
        {
            return BadRequest(new { error = "DPKG is only supported on Ubuntu." });
        }
    }

    [HttpGet("cpu")]
    public async Task<ActionResult<List<CpuInformation>>> Cpu()
    {
        return await hostSystem.GetCpusAsync();
    }

    [HttpGet("kernel")]
    public async Task<ActionResult<KernelInformation>> Kernel()
    {
        return await hostSystem.GetKernelAsync();
    }

    [HttpGet("disks")]
    public async Task<ActionResult<List<DriveInformation>>> Disks([FromQuery] bool fixedOnly = true)
    {
        return await hostSystem.GetDrivesAsync(fixedOnly);
    }

    [HttpGet("processes")]
    public async Task<ActionResult<List<ProcessInformation>>> Processes()
    {
        return await hostSystem.GetProcessesAsync();
    }

    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceInformation>>> Services()
    {
        return await hostSystem.GetServicesAsync();
    }
}
