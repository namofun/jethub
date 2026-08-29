namespace Xylab.Management.WebDeploy.UnitTests;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DeploymentPayloadParserTests
{
    [TestMethod]
    public void ParsesNestedFileContent()
    {
        var content = Encoding.UTF8.GetBytes("body {}\n");
        var payload = CreateEnvelope();
        AddDirectory(payload, id: 4, parentId: 2, "assets");
        AddFile(payload, id: 5, parentId: 4, "site.css", content);

        var parsed = DeploymentPayloadParser.Parse(payload.ToArray());
        var directories = parsed.ResolveDirectories();
        Assert.AreEqual(1, parsed.Files.Count);
        var file = parsed.Files[0];

        Assert.AreEqual(1, parsed.Directories.Count);
        Assert.AreEqual("assets", parsed.Directories[0].Name);
        Assert.AreEqual(Path.Combine("assets", "site.css"), parsed.ResolveFilePath(file, directories));
        CollectionAssert.AreEqual(content, file.Content!.Value.ToArray());
    }

    [TestMethod]
    public void RejectsTraversalRecordNames()
    {
        var payload = new List<byte>();
        payload.AddRange(CreateEnvelope());
        AddDirectory(payload, id: 4, parentId: 2, "..");

        Assert.ThrowsExactly<InvalidDataException>(() => DeploymentPayloadParser.Parse(payload.ToArray()));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-msdeploy")]
    public void RejectsEmptyAndTruncatedPayloads(string value)
    {
        Assert.ThrowsExactly<InvalidDataException>(() => DeploymentPayloadParser.Parse(Encoding.UTF8.GetBytes(value)));
    }

    [TestMethod]
    public void RejectsDuplicateObjectIds()
    {
        var payload = CreateEnvelope();
        AddDirectory(payload, id: 4, parentId: 2, "one");
        AddDirectory(payload, id: 4, parentId: 2, "two");

        Assert.ThrowsExactly<InvalidDataException>(() => DeploymentPayloadParser.Parse(payload.ToArray()));
    }

    [TestMethod]
    public void RejectsOrphanedDirectories()
    {
        var payload = CreateEnvelope();
        AddDirectory(payload, id: 4, parentId: 999, "orphan");

        Assert.ThrowsExactly<InvalidDataException>(() => DeploymentPayloadParser.Parse(payload.ToArray()));
    }

    [TestMethod]
    public void RejectsDirectoryCyclesWithoutRecursion()
    {
        var payload = CreateEnvelope();
        AddDirectory(payload, id: 4, parentId: 5, "one");
        AddDirectory(payload, id: 5, parentId: 4, "two");

        Assert.ThrowsExactly<InvalidDataException>(() => DeploymentPayloadParser.Parse(payload.ToArray()));
    }

    [TestMethod]
    public void DoesNotInterpretFileContentAsProtocolRecords()
    {
        var embeddedRecord = new List<byte>();
        AddDirectory(embeddedRecord, id: 99, parentId: 2, "injected");
        var payload = CreateEnvelope();
        AddFile(payload, id: 4, parentId: 2, "data.bin", embeddedRecord.ToArray());

        var parsed = DeploymentPayloadParser.Parse(payload.ToArray());

        Assert.AreEqual(0, parsed.Directories.Count);
        Assert.AreEqual(1, parsed.Files.Count);
    }

    [TestMethod]
    public void RandomMalformedPayloadsFailClosed()
    {
        var random = new Random(0x5D3E10);
        for (var iteration = 0; iteration < 2_000; iteration++)
        {
            var payload = CreateEnvelope();
            var randomBytes = new byte[random.Next(0, 4096)];
            random.NextBytes(randomBytes);
            payload.AddRange(randomBytes);

            try
            {
                _ = DeploymentPayloadParser.Parse(payload.ToArray());
            }
            catch (InvalidDataException)
            {
                continue;
            }
        }
    }

    private static List<byte> CreateEnvelope()
    {
        var payload = new List<byte> { 4, 0 };
        payload.AddRange(Encoding.ASCII.GetBytes("<systemInfo osVersion=\"test\" /><parameters />MSDeploy.contentPath"));
        payload.AddRange(new byte[16]);
        return payload;
    }

    private static void AddDirectory(List<byte> payload, int id, int parentId, string name)
    {
        AddUInt16(payload, 8);
        AddInt32(payload, id);
        AddInt32(payload, parentId);
        AddString(payload, name);
        payload.AddRange(new byte[24]);
        AddTerminator(payload);
    }

    private static void AddFile(List<byte> payload, int id, int parentId, string name, byte[] content)
    {
        AddUInt16(payload, 9);
        AddInt32(payload, id);
        AddInt32(payload, parentId);
        AddString(payload, name);
        AddInt32(payload, 8);
        AddInt64(payload, content.Length);
        payload.AddRange(new byte[32]);
        AddUInt16(payload, 3);
        AddInt32(payload, id);
        AddUInt16(payload, 5);
        AddInt32(payload, id);
        AddUInt16(payload, 6);
        AddInt32(payload, content.Length);
        payload.AddRange(content);
        AddTerminator(payload);
    }

    private static void AddString(List<byte> payload, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        AddInt32(payload, bytes.Length);
        payload.AddRange(bytes);
    }

    private static void AddUInt16(List<byte> payload, ushort value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        payload.AddRange(bytes.ToArray());
    }

    private static void AddInt32(List<byte> payload, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        payload.AddRange(bytes.ToArray());
    }

    private static void AddInt64(List<byte> payload, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        payload.AddRange(bytes.ToArray());
    }

    private static void AddTerminator(List<byte> payload)
    {
        payload.AddRange([3, 0, 2, 0, 0, 0]);
    }
}
