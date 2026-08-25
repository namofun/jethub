namespace System.IO.Abstractions;

public interface IFileSystemV2 : IFileSystem
{
    new IDirectoryV2 Directory { get; }

    new IFileV2 File { get; }

    bool IsFileSystemReadOnly();
}
