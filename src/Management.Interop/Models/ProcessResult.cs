namespace Xylab.Management.Models
{
    public class ProcessResult
    {
        public string FileName { get; set; }

        public string CommandLine { get; set; }

        public string StandardOutput { get; set; }

        public string StandardError { get; set; }

        public int ExitCode { get; set; }

        public long ElapsedTimeMilliseconds { get; set; }
    }
}
