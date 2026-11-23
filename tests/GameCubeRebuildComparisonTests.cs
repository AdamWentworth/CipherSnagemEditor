using System.Text;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.Tests;

public sealed class GameCubeRebuildComparisonTests : IDisposable
{
    private readonly DirectoryInfo _work = Directory.CreateTempSubdirectory("cipher-rebuild-");

    [Fact]
    public void AllowsFileRelocationWithoutChangingDiscContents()
    {
        var archive = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var original = WriteIso("Expected.iso", archive, fileOffset: 0x4000);
        var rebuilt = WriteIso("Rebuilt.iso", archive, fileOffset: 0x5000);
        var result = GameCubeRebuildComparison.Compare(original, rebuilt);

        Assert.Equal(1, result.ComparedFiles);
        Assert.Empty(result.Differences);
    }

    [Fact]
    public void ReportsChangedBattleMenuMemberByPathAndIdentifier()
    {
        var expected = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var changed = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 8, 3], GameFileType.Gsw, 7));
        var result = GameCubeRebuildComparison.Compare(WriteIso("Expected.iso", expected), WriteIso("Rebuilt.iso", changed));

        Assert.Equal("battle/menu.fsys/test_icons.gsw (0x00073000): member contents differ.", Assert.Single(result.Differences));
    }

    [Fact]
    public void ReportsAChangedArchiveIdentifierEvenWithIdenticalPayload()
    {
        var expected = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var changed = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 8));
        var result = GameCubeRebuildComparison.Compare(WriteIso("Expected.iso", expected), WriteIso("Rebuilt.iso", changed));

        Assert.Contains("battle/menu.fsys: member 0 changed identity", Assert.Single(result.Differences));
    }

    [Fact]
    public void ReportsArchivePackagingChangesWithIdenticalMembers()
    {
        var archive = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var expected = WriteIso("Expected.iso", archive);
        archive[0x30] = 0x12;
        var rebuilt = WriteIso("Rebuilt.iso", archive);
        var result = GameCubeRebuildComparison.Compare(expected, rebuilt);

        Assert.Contains("archive bytes differ", Assert.Single(result.Differences));
    }

    [Fact]
    public void ChecksExecutableSectionsAfterPadding()
    {
        var archive = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var expected = WriteIso("Expected.iso", archive);
        var rebuilt = WriteIso("Rebuilt.iso", archive);
        foreach (var path in new[] { expected, rebuilt })
        {
            var bytes = File.ReadAllBytes(path);
            BigEndian.WriteUInt32(bytes, 0x2600, 0x220);
            BigEndian.WriteUInt32(bytes, 0x2690, 1);
            bytes[0x2820] = path == expected ? (byte)1 : (byte)2;
            File.WriteAllBytes(path, bytes);
        }

        Assert.Contains("Start.dol", Assert.Single(GameCubeRebuildComparison.Compare(expected, rebuilt).Differences));
    }

    [Fact]
    public void RejectsFileTableEntriesOverlappingSystemData()
    {
        var archive = FsysArchiveTests.CreateColosseumStyleFsys(("test_icons", [1, 2, 3], GameFileType.Gsw, 7));
        var original = WriteIso("Expected.iso", archive);
        var invalid = WriteIso("Rebuilt.iso", archive);
        var bytes = File.ReadAllBytes(invalid);
        BigEndian.WriteUInt32(bytes, 0x301c, 0x2600);
        File.WriteAllBytes(invalid, bytes);

        Assert.Contains("Overlapping", Assert.Throws<InvalidDataException>(
            () => GameCubeRebuildComparison.Compare(original, invalid)).Message);
    }

    [Fact]
    public void ReimportingUneditedBattleTextPreservesTheRebuiltDisc()
    {
        var message = GameStringTable.FromStrings([new GameString(7, "", 0)]).ToArray();
        var archive = FsysArchiveTests.CreateColosseumStyleFsys(("battle", message, GameFileType.Message, 7));
        var expected = WriteIso("Expected.iso", archive);
        var rebuilt = WriteIso("Rebuilt.iso", archive);
        var workspace = new GameCubeIsoWorkspace(GameCubeIsoReader.Open(rebuilt),
            Path.Combine(_work.FullName, "workspace"), allowStringGrowth: false);
        var entry = Assert.Single(workspace.Iso.Files, file => file.DiscPath == "battle/menu.fsys");

        var exported = workspace.ExportIsoFile(entry);
        Assert.EndsWith("battle.msg.json", Assert.Single(exported.DecodedFiles));
        workspace.ImportIsoFile(entry, encode: true);

        Assert.Empty(GameCubeRebuildComparison.Compare(expected, rebuilt).Differences);
    }

    private string WriteIso(string fileName, byte[] file, uint fileOffset = 0x4000)
    {
        var image = new byte[fileOffset + file.Length + 0x100];
        Encoding.ASCII.GetBytes("GC6E01").CopyTo(image, 0);
        BigEndian.WriteUInt32(image, 0x1c, 0xc2339f3d);
        BigEndian.WriteUInt32(image, 0x420, 0x2600);
        BigEndian.WriteUInt32(image, 0x424, 0x3000);
        BigEndian.WriteUInt32(image, 0x434, fileOffset);
        BigEndian.WriteUInt32(image, 0x438, (uint)image.Length - fileOffset);

        var names = Encoding.ASCII.GetBytes("battle\0menu.fsys\0");
        const int fstEntriesSize = 3 * 12;
        BigEndian.WriteUInt32(image, 0x428, (uint)(fstEntriesSize + names.Length));
        BigEndian.WriteUInt32(image, 0x42c, (uint)(fstEntriesSize + names.Length));
        BigEndian.WriteUInt32(image, 0x3000, 0x01000000);
        BigEndian.WriteUInt32(image, 0x3008, 3);
        BigEndian.WriteUInt32(image, 0x300c, 0x01000000);
        BigEndian.WriteUInt32(image, 0x3014, 3);
        BigEndian.WriteUInt32(image, 0x3018, 7);
        BigEndian.WriteUInt32(image, 0x301c, fileOffset);
        BigEndian.WriteUInt32(image, 0x3020, (uint)file.Length);
        names.CopyTo(image, 0x3000 + fstEntriesSize);
        file.CopyTo(image, fileOffset);
        var path = Path.Combine(_work.FullName, fileName);
        File.WriteAllBytes(path, image);
        return path;
    }

    public void Dispose() => _work.Delete(recursive: true);
}
