namespace Xylab.Management.WebDeploy.UnitTests;

using System;
using System.Formats.Nrbf;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ChangeSummaryEncoderTests
{
    [TestMethod]
    public void EncodesMSDeployCompatibleNrbfSummary()
    {
        var encoded = ChangeSummaryEncoder.Encode(
            bytesCopied: 123,
            objectsAdded: 4,
            objectsDeleted: 2,
            objectsUpdated: 1);
        using var compressed = new MemoryStream(Convert.FromBase64String(encoded));
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        var record = NrbfDecoder.DecodeClassRecord(gzip);

        Assert.AreEqual("Microsoft.Web.Deployment.DeploymentChangeSummary", record.TypeName.FullName);
        Assert.AreEqual(123L, record.GetInt64("bytesCopied"));
        Assert.AreEqual(4, record.GetInt32("objectsAdded"));
        Assert.AreEqual(2, record.GetInt32("objectsDeleted"));
        Assert.AreEqual(1, record.GetInt32("objectsUpdated"));
    }
}
