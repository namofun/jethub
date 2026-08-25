namespace System.IO.Abstractions;

using System.Threading.Tasks;

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
