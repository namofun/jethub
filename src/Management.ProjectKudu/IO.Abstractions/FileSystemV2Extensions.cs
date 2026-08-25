namespace System.IO.Abstractions;

using System.Collections.Generic;
using System.Threading.Tasks;

public static class FileSystemV2Extensions
{
    public static bool IsSubfolder(this IPath path, string parent, string child)
        => ((IPathV2)path).IsSubfolder(parent, child);

    public static Stream SafeCreate(this IFile file, string path)
        => ((IFileV2)file).SafeCreate(path);

    public static void SafeDelete(this IFile file, string path)
        => ((IFileV2)file).SafeDelete(path);

    public static void SafeMove(this IFile file, string src, string dest)
        => ((IFileV2)file).SafeMove(src, dest);

    public static void SafeAppendAllText(this IFile file, string path, string content)
        => ((IFileV2)file).SafeAppendAllText(path, content);

    public static void SafeWriteAllText(this IFile file, string path, string content)
        => ((IFileV2)file).SafeAppendAllText(path, content);

    public static Task SafeWriteAllTextAsync(this IFile file, string path, string content)
        => ((IFileV2)file).SafeWriteAllTextAsync(path, content);

    public static string SafeReadAllText(this IFile file, string path)
        => ((IFileV2)file).SafeReadAllText(path);

    public static Task<string> SafeReadAllTextAsync(this IFile file, string path)
        => ((IFileV2)file).SafeReadAllTextAsync(path);

    public static string EnsureDirectory(this IDirectory directory, string path)
        => ((IDirectoryV2)directory).EnsureDirectory(path);

    public static void SafeDelete(this IDirectory directory, string path, bool ignoreErrors = true)
        => ((IDirectoryV2)directory).SafeDelete(path, ignoreErrors);

    public static void SafeMove(this IDirectory directory, string src, string dest)
        => ((IDirectoryV2)directory).SafeMove(src, dest);

    public static void RecursiveCopy(this IDirectory directory, string src, string dest, bool overwrite = true)
        => ((IDirectoryV2)directory).RecursiveCopy(src, dest, overwrite);

    public static bool IsFileSystemReadOnly(this IFileSystem fileSystem)
        => ((IFileSystemV2)fileSystem).IsFileSystemReadOnly();

    public static IEnumerable<string> ListFiles(this IDirectory directory, string path, SearchOption searchOption, params string[] lookupList)
        => ((IDirectoryV2)directory).ListFiles(path, searchOption, lookupList);
}
