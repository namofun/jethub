using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.IO.Abstractions
{
    public interface IFileSystemV2 : IFileSystem
    {
        new IDirectoryV2 Directory { get; }

        new IFileV2 File { get; }

        bool IsFileSystemReadOnly();
    }

    public interface IDirectoryV2 : IDirectory
    {
        string EnsureDirectory(string path);

        void SafeDelete(string path, bool ignoreErrors = true);

        void SafeMove(string src, string dest);

        void RecursiveCopy(string src, string dest, bool overwrite = true);

        IEnumerable<string> ListFiles(string path, SearchOption searchOption, params string[] lookupList);
    }

    public interface IFileV2 : IFile
    {
        Stream SafeCreate(string path);

        void SafeDelete(string path);

        void SafeMove(string src, string dest);

        void SafeAppendAllText(string path, string content);

        void SafeWriteAllText(string path, string content);

        Task SafeWriteAllTextAsync(string path, string content);

        string SafeReadAllText(string path);

        Task<string> SafeReadAllTextAsync(string path);
    }

    public interface IPathV2 : IPath
    {
        bool IsSubfolder(string parent, string child);
    }

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

    public class FileSystemV2 : IFileSystemV2, IDriveInfoFactory, IDirectoryInfoFactory, IFileInfoFactory
    {
        public FileSystemV2()
        {
            Path = new PathWrapper(this);
            File = new FileWrapperV2(this);
            Directory = new DirectoryWrapperV2(this);
            FileStream = new FileSystem().FileStream;
            FileSystemWatcher = new FileSystemWatcherFactory();
        }

        public IFileInfoFactory FileInfo => this;

        public IFileStreamFactory FileStream { get; }

        public IPath Path { get; }

        public IDirectoryInfoFactory DirectoryInfo => this;

        public IDriveInfoFactory DriveInfo => this;

        public IFileSystemWatcherFactory FileSystemWatcher { get; }

        public IDirectoryV2 Directory { get; }

        public IFileV2 File { get; }

        IFile IFileSystem.File => File;

        IDirectory IFileSystem.Directory => Directory;

        public bool IsFileSystemReadOnly()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            {
                // if not azure, it should be writable
                return false;
            }

            try
            {
                string tmpFolder = Environment.ExpandEnvironmentVariables(@"%WEBROOT_PATH%\data\Temp");
                string folder = Path.Combine(tmpFolder, Guid.NewGuid().ToString());
                Directory.CreateDirectory(folder);
                Directory.SafeDelete(folder, ignoreErrors: false);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private static void DeleteDirectoryContentsSafe(IDirectoryInfo directoryInfo, bool ignoreErrors)
        {
            try
            {
                if (directoryInfo.Exists)
                {
                    foreach (var fsi in directoryInfo.GetFileSystemInfos())
                    {
                        DeleteFileSystemInfo(fsi, ignoreErrors);
                    }
                }
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }

        private static void DeleteFileSystemInfo(IFileSystemInfo fileSystemInfo, bool ignoreErrors)
        {
            if (!fileSystemInfo.Exists)
            {
                return;
            }

            try
            {
                fileSystemInfo.Attributes = FileAttributes.Normal;
            }
            catch
            {
                if (!ignoreErrors) throw;
            }

            if (fileSystemInfo is IDirectoryInfo directoryInfo)
            {
                DeleteDirectoryContentsSafe(directoryInfo, ignoreErrors);
            }

            DoSafeAction(fileSystemInfo.Delete, ignoreErrors);
        }

        private static void DoSafeAction(Action action, bool ignoreErrors)
        {
            int retries = 3;
            int delayBeforeRetry = 250; // 250 ms

            try
            {
                while (retries > 0)
                {
                    try
                    {
                        action();
                        break;
                    }
                    catch
                    {
                        retries--;
                        if (retries == 0)
                        {
                            throw;
                        }
                    }

                    Threading.Thread.Sleep(delayBeforeRetry);
                }
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }

        IDirectoryInfo IDirectoryInfoFactory.FromDirectoryName(string directoryName)
        {
            return new DirectoryInfoWrapper(this, new(directoryName));
        }

        IDriveInfo IDriveInfoFactory.FromDriveName(string driveName)
        {
            return new DriveInfoWrapper(this, new(driveName));
        }

        IFileInfo IFileInfoFactory.FromFileName(string fileName)
        {
            return new FileInfoWrapper(this, new(fileName));
        }

        IDriveInfo[] IDriveInfoFactory.GetDrives()
        {
            DriveInfo[] drives = IO.DriveInfo.GetDrives();
            DriveInfoBase[] array = new DriveInfoBase[drives.Length];
            for (int i = 0; i < drives.Length; i++)
            {
                DriveInfo instance = drives[i];
                array[i] = new DriveInfoWrapper(this, instance);
            }

            return array;
        }

        private class FileWrapperV2 : FileWrapper, IFileV2
        {
            public FileWrapperV2(FileSystemV2 fileSystem) : base(fileSystem)
            {
            }

            public void SafeAppendAllText(string path, string content)
            {
                using Stream fileStream = Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using StreamWriter streamWriter = new(fileStream);
                streamWriter.Write(content);
                streamWriter.Flush();
            }

            public Stream SafeCreate(string path)
            {
                try
                {
                    FileSystem.Directory.EnsureDirectory(FileSystem.Path.GetDirectoryName(path));
                }
                catch
                {
                    // File create should throw
                }

                return Create(path);
            }

            public void SafeDelete(string path)
            {
                IFileInfo info = FileSystem.FileInfo.FromFileName(path);
                DeleteFileSystemInfo(info, ignoreErrors: true);
            }

            public void SafeMove(string sourceFileName, string destFileName)
            {
                SafeDelete(destFileName);
                Move(sourceFileName, destFileName);
            }

            public string SafeReadAllText(string path)
            {
                using Stream fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader streamReader = new(fileStream);
                return streamReader.ReadToEnd();
            }

            public async Task<string> SafeReadAllTextAsync(string path)
            {
                using Stream fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader streamReader = new(fileStream);
                return await streamReader.ReadToEndAsync();
            }

            public void SafeWriteAllText(string path, string content)
            {
                using Stream fileStream = Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using StreamWriter streamWriter = new(fileStream);
                streamWriter.Write(content);
                streamWriter.Flush();
            }

            public async Task SafeWriteAllTextAsync(string path, string content)
            {
                using Stream fileStream = Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using StreamWriter streamWriter = new(fileStream);
                await streamWriter.WriteAsync(content);
                await streamWriter.FlushAsync();
            }
        }

        private class DirectoryWrapperV2 : DirectoryWrapper, IDirectoryV2
        {
            public DirectoryWrapperV2(FileSystemV2 fileSystem) : base(fileSystem)
            {
            }

            public string EnsureDirectory(string path)
            {
                if (!Exists(path))
                {
                    CreateDirectory(path);
                }

                return path;
            }

            // From MSDN: http://msdn.microsoft.com/en-us/library/bb762914.aspx
            public void RecursiveCopy(string sourceDirPath, string destinationDirPath, bool overwrite = true)
            {
                // Get the subdirectories for the specified directory.
                var sourceDir = FileSystem.DirectoryInfo.FromDirectoryName(sourceDirPath);

                if (!sourceDir.Exists)
                {
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: "
                        + sourceDirPath);
                }

                // If the destination directory doesn't exist, create it.
                EnsureDirectory(destinationDirPath);

                // Get the files in the directory and copy them to the new location.
                foreach (IFileSystemInfo sourceFileSystemInfo in sourceDir.EnumerateFileSystemInfos())
                {
                    if (sourceFileSystemInfo is IFileInfo sourceFile)
                    {
                        string destinationFilePath = FileSystem.Path.Combine(destinationDirPath, sourceFile.Name);
                        FileSystem.File.Copy(sourceFile.FullName, destinationFilePath, overwrite);
                    }
                    else if (sourceFileSystemInfo is IDirectoryInfo sourceSubDir)
                    {
                        // Copy sub-directories and their contents to new location.
                        string destinationSubDirPath = FileSystem.Path.Combine(destinationDirPath, sourceSubDir.Name);
                        RecursiveCopy(sourceSubDir.FullName, destinationSubDirPath, overwrite);
                    }
                }
            }

            public void SafeDelete(string path, bool ignoreErrors = true)
            {
                DeleteFileSystemInfo(FileSystem.DirectoryInfo.FromDirectoryName(path), ignoreErrors);
            }

            public void SafeMove(string sourceDirName, string destDirName)
            {
                // Instance.Directory.Move will result in access denied sometime. Do it ourself!

                EnsureDirectory(destDirName);

                string[] files = GetFiles(sourceDirName);
                string[] dirs = GetDirectories(sourceDirName);

                foreach (string filePath in files)
                {
                    IFileInfo fi = FileSystem.FileInfo.FromFileName(filePath);
                    FileSystem.File.SafeMove(filePath, FileSystem.Path.Combine(destDirName, fi.Name));
                }

                foreach (var dirPath in dirs)
                {
                    IDirectoryInfo di = FileSystem.DirectoryInfo.FromDirectoryName(dirPath);
                    SafeMove(dirPath, FileSystem.Path.Combine(destDirName, di.Name));
                    Delete(dirPath, false);
                }

                Delete(sourceDirName, false);
            }

            public void DeleteContentsSafe(string path, bool ignoreErrors = true)
            {
                DeleteDirectoryContentsSafe(FileSystem.DirectoryInfo.FromDirectoryName(path), ignoreErrors);
            }

            public IEnumerable<string> ListFiles(string path, SearchOption searchOption, params string[] lookupList)
            {
                if (!Exists(path))
                {
                    return Enumerable.Empty<string>();
                }

                // Only lookup of type *.extension or path\file (no *) is supported
                if (lookupList.Any(lookup => lookup.LastIndexOf('*') > 0))
                {
                    throw new NotSupportedException("lookup with a '*' that is not the first character is not supported");
                }

                lookupList = lookupList.Select(lookup => lookup.TrimStart('*')).ToArray();

                return EnumerateFiles(path, "*.*", searchOption)
                    .Where(filePath => lookupList.Any(lookup => filePath.EndsWith(lookup, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private class PathWrapperV2 : PathWrapper, IPathV2
        {
            public PathWrapperV2(FileSystemV2 fileSystem) : base(fileSystem)
            {
            }

            public bool IsSubfolder(string parent, string child)
            {
                // normalize
                string parentPath = GetFullPath(parent).TrimEnd(DirectorySeparatorChar) + DirectorySeparatorChar;
                string childPath = GetFullPath(child).TrimEnd(DirectorySeparatorChar) + DirectorySeparatorChar;
                return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
