using System.Text;
using OrreForge.Core.Archives;
using OrreForge.Core.Binary;
using OrreForge.Core.Files;

namespace OrreForge.Tests;

public sealed class FsysArchiveTests
{
    [Fact]
    public void ParsesSingleUncompressedEntry()
    {
        var bytes = new byte[0x200];
        BigEndian.WriteUInt32(bytes, 0x00, FsysArchive.Magic);
        BigEndian.WriteUInt32(bytes, 0x0c, 1);
        BigEndian.WriteUInt32(bytes, 0x60, 0x80);
        BigEndian.WriteUInt32(bytes, 0x80, 0x00000a00);
        BigEndian.WriteUInt32(bytes, 0x84, 0x120);
        BigEndian.WriteUInt32(bytes, 0x88, 3);
        BigEndian.WriteUInt32(bytes, 0x94, 3);
        BigEndian.WriteUInt32(bytes, 0xa4, 0x100);
        Encoding.ASCII.GetBytes("sample").CopyTo(bytes, 0x100);
        bytes[0x120] = 1;
        bytes[0x121] = 2;
        bytes[0x122] = 3;

        var archive = FsysArchive.Parse(bytes);

        var entry = Assert.Single(archive.Entries);
        Assert.Equal("sample.msg", entry.Name);
        Assert.Equal(GameFileType.Message, entry.FileType);
        Assert.False(entry.IsCompressed);
        Assert.Equal([1, 2, 3], archive.Extract(entry));
    }
}
