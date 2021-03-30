using System.Collections.Generic;

namespace JetHub.Models
{
    public class InstalledPackage
    {
        public string Name { get; set; }

        public string Attach { get; set; }

        public string Version { get; set; }

        public string Architect { get; set; }

        public IReadOnlyList<string> Status { get; set; }
    }
}
