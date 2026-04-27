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
}
