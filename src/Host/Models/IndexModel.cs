using System;
using System.Collections.Generic;

namespace JetHub.Models
{
    public class IndexModel
    {
        public TimeSpan Uptime { get; set; }

        public string LoadAverage { get; set; }

        public string Kernel { get; set; }

        public string Cmdline { get; set; }

        public Dictionary<string, int> Processors { get; set; }

        public List<string> Judgehosts { get; set; }

        public string JudgehostCommitId { get; set; }

        public string JudgehostBranch { get; set; }
    }
}
