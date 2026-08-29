namespace Xylab.Management.WebDeploy;

using System.Buffers.Binary;
using System.Text;

public sealed record DeploymentDirectory(int Id, int ParentId, string Name);

public sealed record DeploymentFile(
    int Id,
    int ParentId,
    string Name,
    long Length,
    ReadOnlyMemory<byte>? Content);

public sealed record DeploymentPayload(
    IReadOnlyList<DeploymentDirectory> Directories,
    IReadOnlyList<DeploymentFile> Files)
{
    public IReadOnlyDictionary<int, string> ResolveDirectories()
    {
        var byId = Directories.ToDictionary(directory => directory.Id);
        var resolved = new Dictionary<int, string>();

        foreach (var directory in Directories)
        {
            if (resolved.ContainsKey(directory.Id))
            {
                continue;
            }

            var chain = new List<DeploymentDirectory>();
            var visiting = new HashSet<int>();
            var current = directory;
            string parentPath;
            while (true)
            {
                if (resolved.TryGetValue(current.Id, out parentPath!))
                {
                    break;
                }

                if (!visiting.Add(current.Id))
                {
                    throw new InvalidDataException("The deployment directory graph contains a cycle.");
                }

                chain.Add(current);
                if (current.ParentId <= 2)
                {
                    parentPath = string.Empty;
                    break;
                }

                if (!byId.TryGetValue(current.ParentId, out current!))
                {
                    throw new InvalidDataException(
                        $"Directory object {chain[^1].Id} references unknown parent {chain[^1].ParentId}.");
                }
            }

            for (var index = chain.Count - 1; index >= 0; index--)
            {
                var item = chain[index];
                parentPath = ConcatFilePath(parentPath, item.Name);
                resolved[item.Id] = parentPath;
            }
        }

        return resolved;
    }

    public string ResolveFilePath(DeploymentFile file, IReadOnlyDictionary<int, string> directories)
    {
        return ConcatFilePath(directories.GetValueOrDefault(file.ParentId), file.Name);
    }

    private string ConcatFilePath(string? parentPath, string item)
    {
        return string.IsNullOrEmpty(parentPath) ? item : Path.Combine(parentPath, item);
    }
}

public static class DeploymentPayloadParser
{
    private const ushort DirectoryRecord = 8;
    private const ushort FileRecord = 9;
    private const int MaximumNameBytes = 1024;
    private const int MaximumFileBytes = 64 * 1024 * 1024;
    private const int MaximumMetadataBytes = 512;
    private const int MaximumRecords = 100_000;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static DeploymentPayload Parse(ReadOnlyMemory<byte> payload)
    {
        var span = payload.Span;
        ValidateEnvelope(span);
        var directories = new Dictionary<int, DeploymentDirectory>();
        var files = new Dictionary<int, DeploymentFile>();
        var skipUntil = 0;

        for (var offset = 0; offset <= span.Length - 14; offset++)
        {
            if (offset < skipUntil)
            {
                continue;
            }

            var recordType = BinaryPrimitives.ReadUInt16LittleEndian(span[offset..]);
            if (recordType is not DirectoryRecord and not FileRecord)
            {
                continue;
            }

            if (!TryReadRecordHeader(
                span,
                offset,
                out var id,
                out var parentId,
                out var name,
                out var metadataOffset))
            {
                continue;
            }

            if (recordType == DirectoryRecord)
            {
                AddUnique(directories, files, id, new DeploymentDirectory(id, parentId, name));
                EnsureRecordLimit(directories.Count, files.Count);
                continue;
            }

            if (!TryReadFile(
                payload,
                id,
                parentId,
                name,
                metadataOffset,
                out var file,
                out var contentEnd))
            {
                continue;
            }

            AddUnique(files, directories, id, file);
            skipUntil = Math.Max(skipUntil, contentEnd);

            EnsureRecordLimit(directories.Count, files.Count);
        }

        if (directories.Count == 0 && files.Count == 0)
        {
            throw new InvalidDataException("The deployment payload contains no filesystem records.");
        }

        var result = new DeploymentPayload(
            directories.Values.OrderBy(directory => directory.Id).ToArray(),
            files.Values.OrderBy(file => file.Id).ToArray());

        ValidateGraph(result);
        return result;
    }

    private static void ValidateEnvelope(ReadOnlySpan<byte> payload)
    {
        ReadOnlySpan<byte> expectedTerminator = [3, 0, 2, 0, 0, 0];
        if (payload.Length < 64
            || payload[0] != 4
            || payload[1] != 0
            || !ContainsAscii(payload, "<systemInfo ")
            || !ContainsAscii(payload, "<parameters")
            || !ContainsAscii(payload, "MSDeploy.contentPath")
            || !payload.EndsWith(expectedTerminator))
        {
            throw new InvalidDataException(
                "The request body is not a supported MSDeploy contentPath payload.");
        }
    }

    private static bool ContainsAscii(ReadOnlySpan<byte> payload, string value) =>
        payload.IndexOf(Encoding.ASCII.GetBytes(value)) >= 0;

    private static void EnsureRecordLimit(int directoryCount, int fileCount)
    {
        if (directoryCount + fileCount > MaximumRecords)
        {
            throw new InvalidDataException("The deployment contains too many records.");
        }
    }

    private static void AddUnique<TValue, TOther>(
        IDictionary<int, TValue> target,
        IReadOnlyDictionary<int, TOther> other,
        int id,
        TValue value)
    {
        if (target.ContainsKey(id) || other.ContainsKey(id))
        {
            throw new InvalidDataException($"The deployment contains duplicate object ID {id}.");
        }

        target.Add(id, value);
    }

    private static void ValidateGraph(DeploymentPayload payload)
    {
        var directoryIds = payload.Directories.Select(directory => directory.Id).ToHashSet();
        foreach (var directory in payload.Directories)
        {
            if (directory.ParentId > 2 && !directoryIds.Contains(directory.ParentId))
            {
                throw new InvalidDataException($"Directory object {directory.Id} references unknown parent {directory.ParentId}.");
            }
        }

        foreach (var file in payload.Files)
        {
            if (file.ParentId > 2 && !directoryIds.Contains(file.ParentId))
            {
                throw new InvalidDataException($"File object {file.Id} references unknown parent {file.ParentId}.");
            }
        }

        var directories = payload.ResolveDirectories();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in directories.Values)
        {
            if (!paths.Add(NormalizePath(directory)))
            {
                throw new InvalidDataException($"The deployment contains a duplicate path '{directory}'.");
            }
        }

        foreach (var file in payload.Files)
        {
            var path = payload.ResolveFilePath(file, directories);
            if (!paths.Add(NormalizePath(path)))
            {
                throw new InvalidDataException($"The deployment contains a duplicate path '{path}'.");
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static bool TryReadRecordHeader(
        ReadOnlySpan<byte> payload,
        int offset,
        out int id,
        out int parentId,
        out string name,
        out int metadataOffset)
    {
        id = BinaryPrimitives.ReadInt32LittleEndian(payload[(offset + 2)..]);
        parentId = BinaryPrimitives.ReadInt32LittleEndian(payload[(offset + 6)..]);
        var nameLength = BinaryPrimitives.ReadInt32LittleEndian(payload[(offset + 10)..]);
        name = string.Empty;
        metadataOffset = 0;

        if (id <= 0
            || parentId < 0
            || nameLength <= 0
            || nameLength > MaximumNameBytes
            || offset + 14 + nameLength > payload.Length)
        {
            return false;
        }

        try
        {
            name = StrictUtf8.GetString(payload.Slice(offset + 14, nameLength));
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        if (!IsSafeSegment(name))
        {
            return false;
        }

        metadataOffset = offset + 14 + nameLength;
        return true;
    }

    private static bool TryReadFile(
        ReadOnlyMemory<byte> payload,
        int id,
        int parentId,
        string name,
        int metadataOffset,
        out DeploymentFile file,
        out int contentEnd)
    {
        file = default!;
        contentEnd = 0;
        var span = payload.Span;
        if (metadataOffset + 12 > span.Length
            || BinaryPrimitives.ReadInt32LittleEndian(span[metadataOffset..]) != 8)
        {
            return false;
        }

        var length = BinaryPrimitives.ReadInt64LittleEndian(span[(metadataOffset + 4)..]);
        if (length < 0 || length > MaximumFileBytes)
        {
            return false;
        }

        ReadOnlyMemory<byte>? content = null;
        var searchEnd = Math.Min(span.Length - 18, metadataOffset + MaximumMetadataBytes);
        for (var marker = metadataOffset + 12; marker <= searchEnd; marker++)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(span[marker..]) != 3
                || BinaryPrimitives.ReadInt32LittleEndian(span[(marker + 2)..]) != id
                || BinaryPrimitives.ReadUInt16LittleEndian(span[(marker + 6)..]) != 5
                || BinaryPrimitives.ReadInt32LittleEndian(span[(marker + 8)..]) != id
                || BinaryPrimitives.ReadUInt16LittleEndian(span[(marker + 12)..]) != 6)
            {
                continue;
            }

            var contentLength = BinaryPrimitives.ReadInt32LittleEndian(span[(marker + 14)..]);
            var contentOffset = marker + 18;
            if (contentLength < 0
                || contentLength > MaximumFileBytes
                || contentLength != length
                || contentOffset + contentLength > span.Length)
            {
                continue;
            }

            content = payload.Slice(contentOffset, contentLength);
            contentEnd = contentOffset + contentLength;
            break;
        }

        file = new DeploymentFile(id, parentId, name, length, content);
        return true;
    }

    private static bool IsSafeSegment(string name)
    {
        return name is not "." and not ".."
            && name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, ':', '\0']) < 0;
    }
}
