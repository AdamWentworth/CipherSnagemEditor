using OrreForge.Core.Binary;
using OrreForge.Core.Compression;
using OrreForge.Core.Files;

namespace OrreForge.Core.Archives;

public sealed class FsysArchive
{
    public const uint Magic = 0x46535953;
    private const int NumberOfEntriesOffset = 0x0c;
    private const int UsesFileExtensionsOffset = 0x13;
    private const int FirstFileDetailsPointerOffset = 0x60;
    private const int FileIdentifierOffset = 0x00;
    private const int FileStartPointerOffset = 0x04;
    private const int UncompressedSizeOffset = 0x08;
    private const int CompressedSizeOffset = 0x14;
    private const int FullFilenameOffset = 0x1c;
    private const int FilenameOffset = 0x24;

    private readonly BinaryData _data;

    private FsysArchive(string? path, BinaryData data, IReadOnlyList<FsysEntry> entries)
    {
        Path = path;
        _data = data;
        Entries = entries;
    }

    public string? Path { get; }

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

        return new FsysArchive(path, data, entries);
    }

    public byte[] Extract(FsysEntry entry)
    {
        var raw = _data.ReadBytes(checked((int)entry.StartOffset), checked((int)entry.CompressedSize));
        return entry.IsCompressed ? LzssCodec.DecodeFile(raw) : raw;
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
}
