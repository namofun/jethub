namespace Xylab.Management.WebDeploy;

using System.IO.Compression;
using System.Text;

public static class ChangeSummaryEncoder
{
    private const string LibraryName =
        "Microsoft.web.deployment, Version=7.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
    private const string TypeName =
        "Microsoft.Web.Deployment.DeploymentChangeSummary";

    private static readonly string[] MemberNames =
    [
        "errors",
        "warnings",
        "bytesCopied",
        "objectsAdded",
        "objectsDeleted",
        "objectsUpdated",
        "parameterChanges"
    ];

    public static string Encode(
        long bytesCopied,
        int objectsAdded,
        int objectsDeleted,
        int objectsUpdated = 0,
        int errors = 0,
        int warnings = 0,
        int parameterChanges = 0)
    {
        using var nrbf = new MemoryStream();
        using (var writer = new BinaryWriter(
                   nrbf,
                   new UTF8Encoding(false),
                   leaveOpen: true))
        {
            writer.Write((byte)0);
            writer.Write(1);
            writer.Write(-1);
            writer.Write(1);
            writer.Write(0);

            writer.Write((byte)12);
            writer.Write(2);
            writer.Write(LibraryName);

            writer.Write((byte)5);
            writer.Write(1);
            writer.Write(TypeName);
            writer.Write(MemberNames.Length);
            foreach (var memberName in MemberNames)
            {
                writer.Write(memberName);
            }

            foreach (var _ in MemberNames)
            {
                writer.Write((byte)0);
            }

            writer.Write((byte)8);
            writer.Write((byte)8);
            writer.Write((byte)9);
            writer.Write((byte)8);
            writer.Write((byte)8);
            writer.Write((byte)8);
            writer.Write((byte)8);
            writer.Write(2);

            writer.Write(errors);
            writer.Write(warnings);
            writer.Write(bytesCopied);
            writer.Write(objectsAdded);
            writer.Write(objectsDeleted);
            writer.Write(objectsUpdated);
            writer.Write(parameterChanges);
            writer.Write((byte)11);
        }

        nrbf.Position = 0;
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            nrbf.CopyTo(gzip);
        }

        return Convert.ToBase64String(compressed.ToArray());
    }
}
