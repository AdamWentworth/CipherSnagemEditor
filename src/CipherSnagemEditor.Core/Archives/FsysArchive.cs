using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Compression;
using CipherSnagemEditor.Core.Files;
using System.Text;

namespace CipherSnagemEditor.Core.Archives;

public sealed class FsysArchive
{
    public const uint Magic = 0x46535953;
    private const int GroupIdOffset = 0x08;
    private const int NumberOfEntriesOffset = 0x0c;
    private const int UsesFileExtensionsOffset = 0x13;
    private const int FileSizeOffset = 0x20;
    private const int DetailsPointersListOffset = 0x40;
    private const int FirstFileNamePointerOffset = 0x44;
    private const int FirstFileOffset = 0x48;
    private const int FirstFileDetailsPointerOffset = 0x60;
    private const int FileIdentifierOffset = 0x00;
    private const int FileStartPointerOffset = 0x04;
    private const int UncompressedSizeOffset = 0x08;
    private const int CompressedSizeOffset = 0x14;
    private const int FullFilenameOffset = 0x1c;
    private const int FileFormatIndexOffset = 0x20;
    private const int FilenameOffset = 0x24;
    private const int ColosseumEntryDetailsSize = 0x50;

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
        var pointerListOffset = PointerListOffset(data.ToArray());
        var entries = new List<FsysEntry>(count);

        for (var index = 0; index < count; index++)
        {
            var detailsPointerOffset = pointerListOffset + (index * 4);
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

    public FsysAddFileResult AddFile(string sourcePath, ushort shortIdentifier, bool compress = true)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("FSYS add-file source does not exist.", sourcePath);
        }

        if (shortIdentifier == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shortIdentifier), "FSYS file identifier must be greater than zero.");
        }

        if (Entries.Count == 0)
        {
            throw new NotSupportedException("Adding files to an empty FSYS archive is unsupported.");
        }

        if (Entries.Any(entry => (entry.Identifier >> 16) == shortIdentifier))
        {
            throw new InvalidDataException($"FSYS archive already contains identifier 0x{shortIdentifier:x4}.");
        }

        var fileType = GameFileTypes.FromExtension(sourcePath);
        if (fileType == GameFileType.Unknown || fileType >= GameFileType.Fsys)
        {
            throw new InvalidDataException($"Unsupported FSYS inner file type: {System.IO.Path.GetExtension(sourcePath)}");
        }

        var sourceBytes = File.ReadAllBytes(sourcePath);
        var archiveBytes = compress && !LzssCodec.HasHeader(sourceBytes)
            ? LzssCodec.EncodeFile(sourceBytes)
            : sourceBytes.ToArray();
        var fullFileName = System.IO.Path.GetFileName(sourcePath);
        var shortFileName = System.IO.Path.GetFileNameWithoutExtension(fullFileName);
        var entryName = NormalizeName(shortFileName, fileType);
        if (Entries.Any(entry => string.Equals(entry.Name, entryName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException($"FSYS archive already contains a file named {entryName}.");
        }

        var data = _data.ToArray();
        var count = checked((int)BigEndian.ReadUInt32(data, NumberOfEntriesOffset));
        var usesFileExtensions = data[UsesFileExtensionsOffset] == 1;
        var pointerListOffset = PointerListOffset(data);
        var entryDetailsSize = EntryDetailsSize(data, count);
        var originalFirstNameOffset = FirstFilenameOffset(data, count);
        var originalFirstFileOffset = FirstFileDataOffset(data, count);
        var originalLastEntry = Entries[^1];
        var rawNames = RawNames(data, count, FilenameOffset);
        var rawFullNames = usesFileExtensions ? RawNames(data, count, FullFilenameOffset) : [];

        var pointerExpansion = 0;
        var bytesAdded = 0;
        var newPointerOffset = checked(pointerListOffset + (4 * count));
        if (count % 4 == 0)
        {
            data = InsertZeros(data, newPointerOffset, 16);
            pointerExpansion = 16;
            bytesAdded += pointerExpansion;
        }

        var firstNameOffset = originalFirstNameOffset + pointerExpansion;
        BigEndian.WriteUInt32(data, FirstFileNamePointerOffset, checked((uint)firstNameOffset));

        var shortFileNameBytes = NullTerminatedAscii(shortFileName);
        var fullFileNameBytes = NullTerminatedAscii(fullFileName);
        var allExistingNames = usesFileExtensions ? rawNames.Concat(rawFullNames) : rawNames;
        var filenamesSize = allExistingNames.Sum(name => NullTerminatedAscii(name).Length);
        var padding = Align16(filenamesSize) - filenamesSize;
        var extraNameBytes = usesFileExtensions
            ? shortFileNameBytes.Length + fullFileNameBytes.Length
            : shortFileNameBytes.Length;
        if (extraNameBytes <= padding)
        {
            extraNameBytes = 0;
        }
        else
        {
            extraNameBytes -= padding;
            while ((filenamesSize + padding + extraNameBytes) % 16 != 0)
            {
                extraNameBytes++;
            }
        }

        if (extraNameBytes > 0)
        {
            data = InsertZeros(data, firstNameOffset, extraNameBytes);
            bytesAdded += extraNameBytes;
        }

        var currentOffset = firstNameOffset;
        var filenameStart = currentOffset;
        foreach (var name in rawNames.Append(shortFileName))
        {
            var bytes = NullTerminatedAscii(name);
            bytes.CopyTo(data.AsSpan(currentOffset));
            currentOffset += bytes.Length;
        }

        filenameStart = currentOffset - shortFileNameBytes.Length;
        var fullFilenameStart = 0;
        if (usesFileExtensions)
        {
            foreach (var name in rawFullNames.Append(fullFileName))
            {
                var bytes = NullTerminatedAscii(name);
                bytes.CopyTo(data.AsSpan(currentOffset));
                currentOffset += bytes.Length;
            }

            fullFilenameStart = currentOffset - fullFileNameBytes.Length;
        }

        while (currentOffset % 16 != 0)
        {
            data[currentOffset++] = 0;
        }

        var entryShift = bytesAdded;
        for (var index = 0; index < count; index++)
        {
            var detailsPointer = checked(pointerListOffset + (4 * index));
            var shiftedPointer = BigEndian.ReadUInt32(data, detailsPointer) + checked((uint)entryShift);
            BigEndian.WriteUInt32(data, detailsPointer, shiftedPointer);
        }

        var firstEntryDetailOffset = checked((int)BigEndian.ReadUInt32(data, pointerListOffset));
        var entryStart = checked(firstEntryDetailOffset + (entryDetailsSize * count));
        data = InsertZeros(data, entryStart, entryDetailsSize);
        bytesAdded += entryDetailsSize;
        var fileStartShift = bytesAdded;

        BigEndian.WriteUInt32(data, FirstFileOffset, checked((uint)(originalFirstFileOffset + fileStartShift)));
        var fileStart = Align16(checked((int)originalLastEntry.StartOffset + (int)originalLastEntry.CompressedSize + bytesAdded));

        var filenamesShift = pointerExpansion;
        var fullFilenamesShift = filenamesShift + shortFileNameBytes.Length;
        for (var index = 0; index < count; index++)
        {
            var detailsOffset = checked((int)BigEndian.ReadUInt32(data, pointerListOffset + (4 * index)));
            BigEndian.WriteUInt32(data, detailsOffset + FileStartPointerOffset, checked(BigEndian.ReadUInt32(data, detailsOffset + FileStartPointerOffset) + (uint)fileStartShift));
            BigEndian.WriteUInt32(data, detailsOffset + FilenameOffset, checked(BigEndian.ReadUInt32(data, detailsOffset + FilenameOffset) + (uint)filenamesShift));
            if (usesFileExtensions)
            {
                BigEndian.WriteUInt32(data, detailsOffset + FullFilenameOffset, checked(BigEndian.ReadUInt32(data, detailsOffset + FullFilenameOffset) + (uint)fullFilenamesShift));
            }
        }

        var paddedArchiveLength = Align16(archiveBytes.Length);
        var paddedArchiveBytes = new byte[paddedArchiveLength];
        archiveBytes.CopyTo(paddedArchiveBytes.AsSpan());
        data = InsertBytes(data, fileStart, paddedArchiveBytes);

        BigEndian.WriteUInt32(data, newPointerOffset, checked((uint)entryStart));
        BigEndian.WriteUInt32(data, FileSizeOffset, checked((uint)data.Length));
        BigEndian.WriteUInt32(data, NumberOfEntriesOffset, checked((uint)(count + 1)));
        BigEndian.WriteUInt32(data, DetailsPointersListOffset, checked((uint)pointerListOffset));
        BigEndian.WriteUInt32(data, FileIdentifierOffset + entryStart, checked(((uint)shortIdentifier << 16) | ((uint)fileType << 8)));
        BigEndian.WriteUInt32(data, entryStart + FileStartPointerOffset, checked((uint)fileStart));
        BigEndian.WriteUInt32(data, entryStart + UncompressedSizeOffset, checked((uint)UncompressedSizeForReplacement(sourceBytes, archiveBytes)));
        BigEndian.WriteUInt32(data, entryStart + 0x0c, 0x80000000);
        BigEndian.WriteUInt32(data, entryStart + CompressedSizeOffset, checked((uint)archiveBytes.Length));
        BigEndian.WriteUInt32(data, entryStart + FileFormatIndexOffset, checked((uint)((int)fileType / 2)));
        BigEndian.WriteUInt32(data, entryStart + FilenameOffset, checked((uint)filenameStart));
        if (usesFileExtensions)
        {
            BigEndian.WriteUInt32(data, entryStart + FullFilenameOffset, checked((uint)fullFilenameStart));
        }

        BigEndian.WriteUInt32(data, FullFilenameOffset, BigEndian.ReadUInt32(data, FirstFileOffset));

        return new FsysAddFileResult(
            data,
            entryName,
            sourcePath,
            shortIdentifier,
            sourceBytes.Length,
            archiveBytes.Length,
            compress || LzssCodec.HasHeader(archiveBytes));
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
        => checked((int)BigEndian.ReadUInt32(data, PointerListOffset(data) + (index * 4)));

    private static int PointerListOffset(byte[] data)
    {
        var offset = data.Length >= DetailsPointersListOffset + 4
            ? checked((int)BigEndian.ReadUInt32(data, DetailsPointersListOffset))
            : 0;

        return offset >= FirstFileDetailsPointerOffset
            && offset + 4 <= data.Length
            ? offset
            : FirstFileDetailsPointerOffset;
    }

    private static int FirstFilenameOffset(byte[] data, int count)
    {
        var offset = data.Length >= FirstFileNamePointerOffset + 4
            ? checked((int)BigEndian.ReadUInt32(data, FirstFileNamePointerOffset))
            : 0;

        if (offset > 0 && offset < data.Length)
        {
            return offset;
        }

        return Enumerable.Range(0, count)
            .Select(index => checked((int)BigEndian.ReadUInt32(data, EntryDetailsOffset(data, index) + FilenameOffset)))
            .Where(value => value > 0 && value < data.Length)
            .DefaultIfEmpty(0x70)
            .Min();
    }

    private static int FirstFileDataOffset(byte[] data, int count)
    {
        var offset = data.Length >= FirstFileOffset + 4
            ? checked((int)BigEndian.ReadUInt32(data, FirstFileOffset))
            : 0;

        if (offset > 0 && offset < data.Length)
        {
            return offset;
        }

        return Enumerable.Range(0, count)
            .Select(index => checked((int)BigEndian.ReadUInt32(data, EntryDetailsOffset(data, index) + FileStartPointerOffset)))
            .Where(value => value > 0 && value < data.Length)
            .DefaultIfEmpty(data.Length - 4)
            .Min();
    }

    private static int EntryDetailsSize(byte[] data, int count)
    {
        if (count > 1)
        {
            var first = EntryDetailsOffset(data, 0);
            var second = EntryDetailsOffset(data, 1);
            var size = second - first;
            if (size >= 0x30 && size <= 0x80 && size % 0x10 == 0)
            {
                return size;
            }
        }

        return ColosseumEntryDetailsSize;
    }

    private static IReadOnlyList<string> RawNames(byte[] data, int count, int fieldOffset)
        => Enumerable.Range(0, count)
            .Select(index => ReadNullTerminatedAscii(data, checked((int)BigEndian.ReadUInt32(data, EntryDetailsOffset(data, index) + fieldOffset))))
            .ToArray();

    private static string ReadNullTerminatedAscii(byte[] data, int offset)
    {
        var end = offset;
        while (end < data.Length && data[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(data, offset, end - offset);
    }

    private static byte[] NullTerminatedAscii(string value)
        => Encoding.ASCII.GetBytes(value + "\0");

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private static byte[] InsertZeros(byte[] data, int offset, int count)
    {
        var expanded = new byte[data.Length + count];
        Buffer.BlockCopy(data, 0, expanded, 0, offset);
        Buffer.BlockCopy(data, offset, expanded, offset + count, data.Length - offset);
        return expanded;
    }

    private static byte[] InsertBytes(byte[] data, int offset, byte[] inserted)
    {
        var expanded = new byte[data.Length + inserted.Length];
        Buffer.BlockCopy(data, 0, expanded, 0, offset);
        inserted.CopyTo(expanded.AsSpan(offset));
        Buffer.BlockCopy(data, offset, expanded, offset + inserted.Length, data.Length - offset);
        return expanded;
    }
}

public sealed record FsysAddFileResult(
    byte[] ArchiveBytes,
    string EntryName,
    string SourcePath,
    ushort ShortIdentifier,
    int SourceSize,
    int ArchiveSize,
    bool Compressed);

public sealed record FsysReplaceResult(
    byte[] ArchiveBytes,
    IReadOnlyList<FsysFileReplacement> ReplacedFiles);

public sealed record FsysFileReplacement(
    string EntryName,
    string SourcePath,
    int SourceSize,
    int ArchiveSize,
    bool Compressed);
