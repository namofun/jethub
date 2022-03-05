using System.Collections.Generic;
using Xylab.Management.Models;

namespace JetHub.Models
{
    public class IndexModel
    {
        public SystemInformation System { get; set; }

        public KernelInformation Kernel { get; set; }

        public List<CpuInformation> Cpus { get; set; }

        public List<DriveInformation> Drives { get; set; }

        public string JudgehostCommitId { get; set; }

        public string JudgehostBranch { get; set; }
    }
}
