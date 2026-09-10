/// <summary>
///   Copied from https://github.com/Azure-App-Service/KuduLite/blob/dev/Kudu.Core/Infrastructure/IFileSystemWatcher.cs
/// </summary>

namespace Xylab.Management.LogStream;

using System.IO;

public interface IFileSystemWatcher
{
    void Start();
    void Stop();

    string Path { get; }

    event FileSystemEventHandler Deleted;
    event FileSystemEventHandler Changed;
    event RenamedEventHandler Renamed;
    event ErrorEventHandler Error;
}
