using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Text;
using System.Text.Json;

namespace CipherSnagemEditor.Tests;

public sealed class ProjectContextTests
{
    [Fact]
    public void OpensMinimalColosseumIsoAndCreatesLegacyWorkspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = new byte[0x500];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';
        BigEndian.WriteUInt32(bytes, 0x420, 0x440);
        BigEndian.WriteUInt32(bytes, 0x424, 0x460);
        BigEndian.WriteUInt32(bytes, 0x428, 0x0c);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);

        Assert.Equal(ColosseumSourceKind.Iso, context.SourceKind);
        Assert.Equal("GC6E", context.Iso?.GameId);
        Assert.Equal(Path.Combine(temp, "Sample CM Tool"), context.WorkspaceDirectory);
        Assert.True(Directory.Exists(context.WorkspaceDirectory));
        Assert.True(File.Exists(Path.Combine(context.WorkspaceDirectory!, "Settings.json")));
    }

    [Fact]
    public void ExtractsIsoFileToLegacyGameFilesFolder()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = new byte[0x700];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = "sample.bin\0"u8.ToArray();
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, 3);
        fileNameBytes.CopyTo(bytes, 0x318);
        bytes[0x500] = 1;
        bytes[0x501] = 2;
        bytes[0x502] = 3;
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");

        var extractedPath = context.ExtractIsoFile(entry);

        Assert.Equal(Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.bin"), extractedPath);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(extractedPath));
    }

    [Fact]
    public void ExportsFsysIsoFileAndExtractsInnerFiles()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var fsysBytes = CreateSingleEntryFsys();
        var bytes = new byte[0x900];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = "sample.fsys\0"u8.ToArray();
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, (uint)fsysBytes.Length);
        fileNameBytes.CopyTo(bytes, 0x318);
        fsysBytes.CopyTo(bytes, 0x500);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.fsys");

        var result = context.ExportIsoFile(entry, extractFsysContents: true, decode: false);

        var rawPath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.fsys");
        var innerPath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.msg");
        Assert.Equal(rawPath, result.FilePath);
        Assert.Contains(innerPath, result.ExtractedFiles);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(innerPath));
    }

    [Fact]
    public void ImportsWorkspaceFileIntoIsoAndUpdatesTocSize()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = CreateSingleFileIso("sample.bin", [1, 2, 3, 4, 5, 6, 7, 8], fileSlotSize: 0x40);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");
        var workspacePath = context.ExtractIsoFile(entry);
        File.WriteAllBytes(workspacePath, [9, 8, 7]);

        var result = context.ImportIsoFile(entry, encode: false);

        Assert.Equal(3, result.WrittenBytes);
        var updatedIso = File.ReadAllBytes(isoPath);
        Assert.Equal([9, 8, 7, 0, 0, 0, 0, 0], updatedIso[0x500..0x508]);
        Assert.Equal((uint)3, ColosseumProjectContext.Open(isoPath).Iso!.Files.Single(file => file.Name == "sample.bin").Size);
    }

    [Fact]
    public void DeletesIsoFileWithLegacyPreservedMarker()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = CreateSingleFileIso("sample.bin", Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), fileSlotSize: 0x40);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");

        var result = context.DeleteIsoFile(entry);

        Assert.Equal(16, result.WrittenBytes);
        Assert.True(File.Exists(result.BackupPath));
        var updatedIso = File.ReadAllBytes(isoPath);
        Assert.Equal("DELETED DELETED\0"u8.ToArray(), updatedIso[0x500..0x510]);
        Assert.Equal((uint)16, ColosseumProjectContext.Open(isoPath).Iso!.Files.Single(file => file.Name == "sample.bin").Size);
    }

    [Fact]
    public void EncodesMessageJsonAndPacksFsysWorkspaceFile()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var originalTable = GameStringTable.FromStrings([new GameString(7, "Old", 0)]).ToArray();
        var fsysBytes = CreateSingleEntryFsys(originalTable);
        var bytes = CreateSingleFileIso("sample.fsys", fsysBytes, fileSlotSize: 0x400);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.fsys");
        var export = context.ExportIsoFile(entry, extractFsysContents: true, decode: true);
        var messagePath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.msg");
        var jsonPath = messagePath + ".json";
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { new GameString(7, "New", 0) }));

        var result = context.EncodeIsoFile(entry);

        Assert.Equal(export.FilePath, result.FilePath);
        Assert.Contains(jsonPath, result.EncodedFiles);
        Assert.Contains(messagePath, result.PackedFiles);
        var archive = FsysArchive.Load(export.FilePath);
        var message = Assert.Single(archive.Entries);
        var table = GameStringTable.Parse(archive.Extract(message));
        Assert.Equal("New", table.StringWithId(7));
    }

    private static byte[] CreateSingleFileIso(string fileName, byte[] fileBytes, int fileSlotSize)
    {
        var bytes = new byte[0x500 + fileSlotSize + 0x100];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = System.Text.Encoding.ASCII.GetBytes(fileName + "\0");
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, (uint)fileBytes.Length);
        fileNameBytes.CopyTo(bytes, 0x318);
        fileBytes.CopyTo(bytes, 0x500);
        return bytes;
    }

    private static byte[] CreateSingleEntryFsys(byte[]? fileBytes = null)
    {
        fileBytes ??= [1, 2, 3];
        var bytes = new byte[0x200];
        BigEndian.WriteUInt32(bytes, 0x00, 0x46535953);
        BigEndian.WriteUInt32(bytes, 0x0c, 1);
        BigEndian.WriteUInt32(bytes, 0x20, (uint)bytes.Length);
        BigEndian.WriteUInt32(bytes, 0x60, 0x80);
        BigEndian.WriteUInt32(bytes, 0x80, 0x00000a00);
        BigEndian.WriteUInt32(bytes, 0x84, 0x120);
        BigEndian.WriteUInt32(bytes, 0x88, (uint)fileBytes.Length);
        BigEndian.WriteUInt32(bytes, 0x94, (uint)fileBytes.Length);
        BigEndian.WriteUInt32(bytes, 0xa4, 0x100);
        "sample"u8.CopyTo(bytes.AsSpan(0x100));
        fileBytes.CopyTo(bytes, 0x120);
        return bytes;
    }
}
