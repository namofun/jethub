namespace Xylab.Management.WebDeploy;

using System.Formats.Nrbf;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;

public sealed record ProviderOptions(string ProviderName, string Path);

public sealed record SyncOptions(bool WhatIf, bool DeleteDestination);

public static class MSDeployHeaderDecoder
{
    private const int MaximumCompressedBytes = 1024 * 1024;
    private const int MaximumDecompressedBytes = 4 * 1024 * 1024;

    public static ProviderOptions DecodeProviderOptions(string encoded)
    {
        var record = DecodeClassRecord(encoded);
        EnsureType(record, "Microsoft.Web.Deployment.DeploymentProviderOptions");
        return new ProviderOptions(RequireString(record, "providerName"), RequireString(record, "path"));
    }

    private static string RequireString(ClassRecord record, string memberName)
    {
        return record.GetString(memberName) ?? throw new InvalidDataException($"MSDeploy option member '{memberName}' cannot be null.");
    }

    public static SyncOptions DecodeSyncOptions(string encoded)
    {
        var record = DecodeClassRecord(encoded);
        EnsureType(record, "Microsoft.Web.Deployment.DeploymentSyncOptions");
        return new SyncOptions(record.GetBoolean("whatIf"), record.GetBoolean("deleteDestination"));
    }

    private static ClassRecord DecodeClassRecord(string encoded)
    {
        byte[] compressed;
        try
        {
            compressed = Convert.FromBase64String(encoded);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The MSDeploy option header is not valid Base64.", exception);
        }

        if (compressed.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("The compressed MSDeploy option header is too large.");
        }

        try
        {
            using var compressedStream = new MemoryStream(compressed, writable: false);
            using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: false);
            using var decoded = new MemoryStream();
            CopyBounded(gzip, decoded, MaximumDecompressedBytes);
            decoded.Position = 0;
            return NrbfDecoder.DecodeClassRecord(decoded);
        }
        catch (Exception exception) when (
            exception is SerializationException or
                EndOfStreamException or
                NotSupportedException or
                DecoderFallbackException or
                IOException)
        {
            throw new InvalidDataException(
                "The MSDeploy option header is not valid gzip-compressed NRBF.",
                exception);
        }
    }

    private static void EnsureType(ClassRecord record, string expectedTypeName)
    {
        if (!string.Equals(record.TypeName.FullName, expectedTypeName, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unexpected MSDeploy option type '{record.TypeName.FullName}'.");
        }
    }

    private static void CopyBounded(Stream source, Stream destination, int maximumBytes)
    {
        var buffer = new byte[81920];
        while (true)
        {
            var read = source.Read(buffer);
            if (read == 0)
            {
                return;
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException("The decompressed MSDeploy option header is too large.");
            }

            destination.Write(buffer, 0, read);
        }
    }
}
