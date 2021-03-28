using System;

namespace JetHub.Models
{
    public class IndexModel
    {
        public TimeSpan Uptime { get; set; }

        public string LoadAverage { get; set; }
    }
}
