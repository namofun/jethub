#nullable enable
namespace Xylab.Management.Interop;

using System.Threading.Tasks;

public interface IProcessFactory
{
    public Task<ProcessResult> StartAsync(string fileName, string? cmdline = null, ProcessStartupOptions? options = null);
}
