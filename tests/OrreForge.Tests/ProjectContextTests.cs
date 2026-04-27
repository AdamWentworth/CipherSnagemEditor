using OrreForge.Colosseum;
using OrreForge.Core.Binary;

namespace OrreForge.Tests;

public sealed class ProjectContextTests
{
    [Fact]
    public void OpensMinimalColosseumIsoAndCreatesLegacyWorkspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), "OrreForgeTests", Guid.NewGuid().ToString("N"));
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
        var temp = Path.Combine(Path.GetTempPath(), "OrreForgeTests", Guid.NewGuid().ToString("N"));
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
}
