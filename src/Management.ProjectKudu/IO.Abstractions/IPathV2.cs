namespace System.IO.Abstractions;

public interface IPathV2 : IPath
{
    bool IsSubfolder(string parent, string child);
}
