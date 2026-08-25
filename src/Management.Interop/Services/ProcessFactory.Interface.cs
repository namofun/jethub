#nullable enable
namespace Xylab.Management.Services;

using System.Threading.Tasks;
using Xylab.Management.Models;

public interface IProcessFactory
{
    public Task<ProcessResult> StartAsync(string fileName, string? cmdline = null, ProcessStartupOptions? options = null);
}
