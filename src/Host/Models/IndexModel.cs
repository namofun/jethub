using System;

namespace JetHub.Models
{
    public class IndexModel
    {
        public TimeSpan Uptime { get; set; }

        public string LoadAverage { get; set; }

        public string JudgehostCommitId { get; set; }

        public string JudgehostBranch { get; set; }
    }
}
