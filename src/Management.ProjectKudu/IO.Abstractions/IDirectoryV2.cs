namespace System.IO.Abstractions;

using System.Collections.Generic;

public interface IDirectoryV2 : IDirectory
{
    string EnsureDirectory(string path);

    void SafeDelete(string path, bool ignoreErrors = true);

    void SafeMove(string src, string dest);

    void RecursiveCopy(string src, string dest, bool overwrite = true);

    IEnumerable<string> ListFiles(string path, SearchOption searchOption, params string[] lookupList);
}
