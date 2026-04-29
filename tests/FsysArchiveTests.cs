using System.Text;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Compression;
using CipherSnagemEditor.Core.Files;

namespace CipherSnagemEditor.Tests;

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

    [Fact]
    public void AddsCompressedFileAndUpdatesArchivePointers()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var sourcePath = Path.Combine(temp, "added.msg");
        File.WriteAllBytes(sourcePath, [9, 8, 7, 6]);

        var archive = FsysArchive.Parse(CreateColosseumStyleFsys(("sample", [1, 2, 3], GameFileType.Message, 0x000a)));

        var result = archive.AddFile(sourcePath, 0x1234);
        var updated = FsysArchive.Parse(result.ArchiveBytes);

        Assert.Equal(2, updated.Entries.Count);
        Assert.Equal("sample.msg", updated.Entries[0].Name);
        Assert.Equal([1, 2, 3], updated.Extract(updated.Entries[0]));
        var added = updated.Entries[1];
        Assert.Equal("added.msg", added.Name);
        Assert.Equal(0x12340a00u, added.Identifier);
        Assert.True(added.IsCompressed);
        Assert.True(LzssCodec.HasHeader(result.ArchiveBytes.AsSpan((int)added.StartOffset, (int)added.CompressedSize)));
        Assert.Equal([9, 8, 7, 6], updated.Extract(added));
    }

    [Fact]
    public void AddsFileWhenPointerTableMustExpand()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var sourcePath = Path.Combine(temp, "added.msg");
        File.WriteAllBytes(sourcePath, [9, 8, 7, 6]);

        var archive = FsysArchive.Parse(CreateColosseumStyleFsys(
            ("one", [1], GameFileType.Message, 0x0001),
            ("two", [2], GameFileType.Message, 0x0002),
            ("three", [3], GameFileType.Message, 0x0003),
            ("four", [4], GameFileType.Message, 0x0004)));

        var result = archive.AddFile(sourcePath, 0x1234);
        var updated = FsysArchive.Parse(result.ArchiveBytes);

        Assert.Equal(5, updated.Entries.Count);
        Assert.Equal([1], updated.Extract(updated.Entries[0]));
        Assert.Equal([2], updated.Extract(updated.Entries[1]));
        Assert.Equal([3], updated.Extract(updated.Entries[2]));
        Assert.Equal([4], updated.Extract(updated.Entries[3]));
        Assert.Equal([9, 8, 7, 6], updated.Extract(updated.Entries[4]));
    }

    [Fact]
    public void RejectsDuplicateShortIdentifierWhenAddingFile()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var sourcePath = Path.Combine(temp, "added.msg");
        File.WriteAllBytes(sourcePath, [9, 8, 7, 6]);
        var archive = FsysArchive.Parse(CreateColosseumStyleFsys(("sample", [1, 2, 3], GameFileType.Message, 0x1234)));

        Assert.Throws<InvalidDataException>(() => archive.AddFile(sourcePath, 0x1234));
    }

    private static byte[] CreateColosseumStyleFsys(params (string RawName, byte[] FileBytes, GameFileType FileType, ushort ShortIdentifier)[] entries)
    {
        const int pointerListStart = 0x60;
        const int namesStart = 0x70;
        const int detailsSize = 0x50;
        var nameOffsets = new int[entries.Length];
        var cursor = namesStart;
        for (var index = 0; index < entries.Length; index++)
        {
            nameOffsets[index] = cursor;
            cursor += Encoding.ASCII.GetByteCount(entries[index].RawName) + 1;
        }

        var detailsStart = Align16(cursor);
        var dataStart = Align16(detailsStart + (entries.Length * detailsSize));
        var dataOffsets = new int[entries.Length];
        cursor = dataStart;
        for (var index = 0; index < entries.Length; index++)
        {
            dataOffsets[index] = cursor;
            cursor = Align16(cursor + entries[index].FileBytes.Length);
        }

        var bytes = new byte[Align16(cursor) + 4];
        BigEndian.WriteUInt32(bytes, 0x00, FsysArchive.Magic);
        BigEndian.WriteUInt32(bytes, 0x0c, checked((uint)entries.Length));
        BigEndian.WriteUInt32(bytes, 0x20, checked((uint)bytes.Length));
        BigEndian.WriteUInt32(bytes, 0x40, pointerListStart);
        BigEndian.WriteUInt32(bytes, 0x44, namesStart);
        BigEndian.WriteUInt32(bytes, 0x48, checked((uint)dataStart));
        BigEndian.WriteUInt32(bytes, 0x1c, checked((uint)dataStart));
        BigEndian.WriteUInt32(bytes, bytes.Length - 4, FsysArchive.Magic);

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var detailsOffset = detailsStart + (index * detailsSize);
            BigEndian.WriteUInt32(bytes, pointerListStart + (index * 4), checked((uint)detailsOffset));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x00, checked(((uint)entry.ShortIdentifier << 16) | ((uint)entry.FileType << 8)));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x04, checked((uint)dataOffsets[index]));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x08, checked((uint)entry.FileBytes.Length));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x14, checked((uint)entry.FileBytes.Length));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x20, checked((uint)((int)entry.FileType / 2)));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x24, checked((uint)nameOffsets[index]));

            Encoding.ASCII.GetBytes(entry.RawName + "\0").CopyTo(bytes, nameOffsets[index]);
            entry.FileBytes.CopyTo(bytes.AsSpan(dataOffsets[index]));
        }

        return bytes;
    }

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;
}
