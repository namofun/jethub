namespace JetHub.Models
{
    public class MemoryUsageEntry
    {
        public long MemoryUsed { get; set; }

        public long MemoryTotal { get; set; }

        public long SwapUsed { get; set; }

        public long SwapTotal { get; set; }
    }
}
