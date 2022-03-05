using System.Collections.Generic;
using Xylab.Management.Models;

namespace Xylab.Management.Interop
{
    internal partial class Parser
    {
        public static List<InstalledPackage> DpkgStatus(string[] contents)
        {
            const string PackagePrefix = "Package: ";
            const string ArchitecturePrefix = "Architecture: ";
            const string VersionPrefix = "Version: ";

            List<InstalledPackage> installedPackages = new();
            InstalledPackage package = null;
            foreach (string line in contents)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (package?.Name != null)
                    {
                        installedPackages.Add(package);
                        package = null;
                    }

                    continue;
                }

                package ??= new InstalledPackage();
                if (line.StartsWith(PackagePrefix))
                {
                    package.Name = line[PackagePrefix.Length..];
                }
                else if (line.StartsWith(ArchitecturePrefix))
                {
                    package.Architect = line[ArchitecturePrefix.Length..];
                }
                else if (line.StartsWith(VersionPrefix))
                {
                    package.Version = line[VersionPrefix.Length..];
                }
            }

            return installedPackages;
        }
    }
}
