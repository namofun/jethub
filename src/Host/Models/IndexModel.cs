using System.Collections.Generic;

namespace JetHub.Models
{
    public class IndexModel
    {
        public SystemInformation System { get; set; }

        public KernelInformation Kernel { get; set; }

        public List<CpuInformation> Cpus { get; set; }

        public string JudgehostCommitId { get; set; }

        public string JudgehostBranch { get; set; }
    }
}
