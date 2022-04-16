#nullable enable
using System.Threading.Tasks;
using Xylab.Management.Models;

namespace Xylab.Management.Services
{
    public interface IProcessFactory
    {
        public Task<ProcessResult> StartAsync(string fileName, string? cmdline = null, ProcessStartupOptions? options = null);
    }
}
