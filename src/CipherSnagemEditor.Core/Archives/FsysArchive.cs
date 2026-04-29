using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Compression;
using CipherSnagemEditor.Core.Files;

namespace CipherSnagemEditor.Core.Archives;

public sealed class FsysArchive
{
    public const uint Magic = 0x46535953;
    private const int GroupIdOffset = 0x08;
    private const int NumberOfEntriesOffset = 0x0c;
    private const int UsesFileExtensionsOffset = 0x13;
    private const int FileSizeOffset = 0x20;
    private const int FirstFileDetailsPointerOffset = 0x60;
    private const int FileIdentifierOffset = 0x00;
    private const int FileStartPointerOffset = 0x04;
    private const int UncompressedSizeOffset = 0x08;
    private const int CompressedSizeOffset = 0x14;
    private const int FullFilenameOffset = 0x1c;
    private const int FilenameOffset = 0x24;

    private readonly BinaryData _data;

    private FsysArchive(string? path, BinaryData data, uint groupId, IReadOnlyList<FsysEntry> entries)
    {
        Path = path;
        _data = data;
        GroupId = groupId;
        Entries = entries;
    }

    public string? Path { get; }

    public uint GroupId { get; }

    public IReadOnlyList<FsysEntry> Entries { get; }

    public static FsysArchive Load(string path) => Parse(path, File.ReadAllBytes(path));

    public static FsysArchive Parse(byte[] bytes) => Parse(null, bytes);

    public static FsysArchive Parse(string? path, byte[] bytes)
    {
        var data = new BinaryData(bytes);
        if (data.ReadUInt32(0) != Magic)
        {
            throw new InvalidDataException("File is not an FSYS archive.");
        }

        var groupId = data.ReadUInt32(GroupIdOffset);
        var count = checked((int)data.ReadUInt32(NumberOfEntriesOffset));
        var usesFileExtensions = data.ReadByte(UsesFileExtensionsOffset) == 1;
        var entries = new List<FsysEntry>(count);

        for (var index = 0; index < count; index++)
        {
            var detailsPointerOffset = FirstFileDetailsPointerOffset + (index * 4);
            var detailsOffset = checked((int)data.ReadUInt32(detailsPointerOffset));
            var identifier = data.ReadUInt32(detailsOffset + FileIdentifierOffset);
            var start = data.ReadUInt32(detailsOffset + FileStartPointerOffset);
            var uncompressedSize = data.ReadUInt32(detailsOffset + UncompressedSizeOffset);
            var compressedSize = data.ReadUInt32(detailsOffset + CompressedSizeOffset);
            var namePointerOffset = usesFileExtensions ? FullFilenameOffset : FilenameOffset;
            var nameOffset = checked((int)data.ReadUInt32(detailsOffset + namePointerOffset));
            var fileType = GameFileTypes.FromFsysIdentifier(identifier);
            var name = NormalizeName(data.ReadNullTerminatedAscii(nameOffset), fileType);
            var isCompressed = start + 4 <= data.Length && data.ReadUInt32((int)start) == LzssCodec.Magic;

            entries.Add(new FsysEntry(
                index,
                name,
                identifier,
                fileType,
                start,
                compressedSize,
                uncompressedSize,
                isCompressed));
        }

        return new FsysArchive(path, data, groupId, entries);
    }

    public byte[] Extract(FsysEntry entry)
    {
        var raw = _data.ReadBytes(checked((int)entry.StartOffset), checked((int)entry.CompressedSize));
        return entry.IsCompressed ? LzssCodec.DecodeFile(raw) : raw;
    }

    public FsysReplaceResult ReplaceFilesFromDirectory(string folder, bool encodeCompressed = true)
    {
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"FSYS import folder does not exist: {folder}");
        }

        var data = _data.ToArray();
        var replacements = new List<FsysFileReplacement>();

        foreach (var entry in Entries)
        {
            var sourcePath = System.IO.Path.Combine(folder, entry.Name);
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var sourceBytes = File.ReadAllBytes(sourcePath);
            var archiveBytes = sourceBytes;
            if (entry.IsCompressed && encodeCompressed && !LzssCodec.HasHeader(sourceBytes))
            {
                archiveBytes = LzssCodec.EncodeFile(sourceBytes);
            }

            data = ReplaceEntryBytes(data, entry.Index, archiveBytes, UncompressedSizeForReplacement(sourceBytes, archiveBytes));
            replacements.Add(new FsysFileReplacement(
                entry.Name,
                sourcePath,
                sourceBytes.Length,
                archiveBytes.Length,
                entry.IsCompressed));
        }

        return new FsysReplaceResult(data, replacements);
    }

    private static string NormalizeName(string name, GameFileType fileType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "unknown";
        }

        name = name.Replace("_rel", ".rel", StringComparison.OrdinalIgnoreCase);
        var extension = System.IO.Path.GetExtension(name);
        if (string.IsNullOrWhiteSpace(extension))
        {
            name += GameFileTypes.ExtensionFor(fileType);
        }

        return name;
    }

    private static int UncompressedSizeForReplacement(byte[] sourceBytes, byte[] archiveBytes)
    {
        if (LzssCodec.HasHeader(archiveBytes))
        {
            return checked((int)BigEndian.ReadUInt32(archiveBytes, 4));
        }

        return sourceBytes.Length;
    }

    private static byte[] ReplaceEntryBytes(byte[] data, int index, byte[] replacement, int uncompressedSize)
    {
        var count = checked((int)BigEndian.ReadUInt32(data, NumberOfEntriesOffset));
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"FSYS entry index {index} is outside a {count}-entry archive.");
        }

        var detailsOffset = EntryDetailsOffset(data, index);
        var fileStart = checked((int)BigEndian.ReadUInt32(data, detailsOffset + FileStartPointerOffset));
        var oldSize = checked((int)BigEndian.ReadUInt32(data, detailsOffset + CompressedSizeOffset));
        var nextStart = index < count - 1
            ? checked((int)BigEndian.ReadUInt32(data, EntryDetailsOffset(data, index + 1) + FileStartPointerOffset))
            : data.Length - 4;

        if (fileStart < 0 || fileStart > data.Length || nextStart < fileStart || nextStart > data.Length)
        {
            throw new InvalidDataException($"FSYS entry {index} has invalid file bounds.");
        }

        if (fileStart + replacement.Length > nextStart)
        {
            var expansion = Align16(fileStart + replacement.Length - nextStart + 0x80);
            data = InsertZeros(data, nextStart, expansion);

            for (var entryIndex = index + 1; entryIndex < count; entryIndex++)
            {
                var nextDetailsOffset = EntryDetailsOffset(data, entryIndex);
                var start = BigEndian.ReadUInt32(data, nextDetailsOffset + FileStartPointerOffset);
                BigEndian.WriteUInt32(data, nextDetailsOffset + FileStartPointerOffset, checked(start + (uint)expansion));
            }

            BigEndian.WriteUInt32(data, FileSizeOffset, checked((uint)data.Length));
            nextStart += expansion;
        }

        if (fileStart + replacement.Length > nextStart)
        {
            throw new InvalidDataException($"Replacement for FSYS entry {index} is too large for the archive.");
        }

        var clearLength = Math.Min(Math.Max(oldSize, replacement.Length), data.Length - fileStart);
        Array.Clear(data, fileStart, clearLength);
        replacement.CopyTo(data.AsSpan(fileStart));
        BigEndian.WriteUInt32(data, detailsOffset + CompressedSizeOffset, checked((uint)replacement.Length));
        BigEndian.WriteUInt32(data, detailsOffset + UncompressedSizeOffset, checked((uint)uncompressedSize));

        return data;
    }

    private static int EntryDetailsOffset(byte[] data, int index)
        => checked((int)BigEndian.ReadUInt32(data, FirstFileDetailsPointerOffset + (index * 4)));

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private static byte[] InsertZeros(byte[] data, int offset, int count)
    {
        var expanded = new byte[data.Length + count];
        Buffer.BlockCopy(data, 0, expanded, 0, offset);
        Buffer.BlockCopy(data, offset, expanded, offset + count, data.Length - offset);
        return expanded;
    }
}

public sealed record FsysReplaceResult(
    byte[] ArchiveBytes,
    IReadOnlyList<FsysFileReplacement> ReplacedFiles);

public sealed record FsysFileReplacement(
    string EntryName,
    string SourcePath,
    int SourceSize,
    int ArchiveSize,
    bool Compressed);
