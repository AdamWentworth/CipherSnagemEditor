using System.Text;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public static class GameCubeIsoReader
{
    private const int DolStartOffsetLocation = 0x420;
    private const int TocStartOffsetLocation = 0x424;
    private const int TocFileSizeLocation = 0x428;
    private const int TocEntrySize = 0x0c;
    private const int DolSectionSizesStart = 0x90;
    private const int DolSectionSizesCount = 18;
    private const int DolHeaderSize = 0x100;

    public static GameCubeIso Open(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("ISO file does not exist.", path);
        }

        if (path.EndsWith(".nkit.iso", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("nkit ISO files are not supported. Convert to a regular ISO first.");
        }

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var gameIdBytes = ReadExactAt(stream, 0, 4);
        var gameId = Encoding.ASCII.GetString(gameIdBytes);
        var region = RegionFromGameId(gameId);
        var files = ReadFileSystemTable(stream);

        return new GameCubeIso(path, gameId, region, files);
    }

    public static byte[] ReadFile(GameCubeIso iso, GameCubeIsoFileEntry entry)
    {
        ArgumentNullException.ThrowIfNull(iso);
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Size > int.MaxValue)
        {
            throw new InvalidDataException($"ISO file {entry.Name} is too large to load into memory.");
        }

        using var stream = File.Open(iso.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var endOffset = (long)entry.Offset + entry.Size;
        if (endOffset > stream.Length)
        {
            throw new EndOfStreamException(
                $"ISO file {entry.Name} extends past the end of the image: 0x{entry.Offset:x8}+0x{entry.Size:x}.");
        }

        return ReadExactAt(stream, entry.Offset, checked((int)entry.Size));
    }

    private static IReadOnlyList<GameCubeIsoFileEntry> ReadFileSystemTable(FileStream stream)
    {
        if (stream.Length < TocFileSizeLocation + 4)
        {
            return Array.Empty<GameCubeIsoFileEntry>();
        }

        var dolStart = ReadUInt32At(stream, DolStartOffsetLocation);
        var tocStart = ReadUInt32At(stream, TocStartOffsetLocation);
        var tocSize = ReadUInt32At(stream, TocFileSizeLocation);

        var entries = new List<GameCubeIsoFileEntry>
        {
            new("Start.dol", dolStart, ComputeDolSize(stream, dolStart)),
            new("Game.toc", tocStart, tocSize)
        };

        if (tocStart == 0 || tocSize < TocEntrySize || tocStart + tocSize > stream.Length || tocSize > int.MaxValue)
        {
            return entries;
        }

        var toc = ReadExactAt(stream, tocStart, (int)tocSize);
        var entryCount = BigEndian.ReadUInt32(toc, 8);
        var firstStringOffset = checked((int)entryCount * TocEntrySize);
        if (entryCount == 0 || firstStringOffset > toc.Length)
        {
            return entries;
        }

        for (var index = 1; index < entryCount; index++)
        {
            var offset = checked(index * TocEntrySize);
            if (offset + TocEntrySize > toc.Length)
            {
                break;
            }

            var entryType = toc[offset];
            if (entryType == 1)
            {
                continue;
            }

            var nameOffset = (int)(BigEndian.ReadUInt32(toc, offset) & 0x00ff_ffff);
            var fileOffset = BigEndian.ReadUInt32(toc, offset + 4);
            var fileSize = BigEndian.ReadUInt32(toc, offset + 8);
            var name = ReadNullTerminatedAscii(toc, firstStringOffset + nameOffset);
            if (!string.IsNullOrWhiteSpace(name))
            {
                entries.Add(new GameCubeIsoFileEntry(name, fileOffset, fileSize, tocStart + (uint)offset));
            }
        }

        return entries.OrderBy(entry => entry.Offset).ToArray();
    }

    private static uint ComputeDolSize(FileStream stream, uint dolStart)
    {
        if (dolStart == 0 || dolStart + DolSectionSizesStart + (DolSectionSizesCount * 4) > stream.Length)
        {
            return 0;
        }

        var size = DolHeaderSize;
        for (var index = 0; index < DolSectionSizesCount; index++)
        {
            size += checked((int)ReadUInt32At(stream, dolStart + DolSectionSizesStart + (uint)(index * 4)));
        }

        return (uint)size;
    }

    private static GameCubeRegion RegionFromGameId(string gameId) => gameId switch
    {
        "GC6E" or "GXXE" => GameCubeRegion.UnitedStates,
        "GC6P" or "GXXP" => GameCubeRegion.Europe,
        "GC6J" or "GXXJ" => GameCubeRegion.Japan,
        _ => GameCubeRegion.OtherGame
    };

    private static uint ReadUInt32At(FileStream stream, uint offset)
        => BigEndian.ReadUInt32(ReadExactAt(stream, offset, 4), 0);

    private static byte[] ReadExactAt(FileStream stream, uint offset, int length)
    {
        if (offset + length > stream.Length)
        {
            throw new EndOfStreamException($"Cannot read {length} bytes at 0x{offset:x} from {stream.Length}-byte stream.");
        }

        var bytes = new byte[length];
        stream.Position = offset;
        var read = stream.Read(bytes, 0, length);
        if (read != length)
        {
            throw new EndOfStreamException($"Expected {length} bytes at 0x{offset:x}, read {read}.");
        }

        return bytes;
    }

    private static string ReadNullTerminatedAscii(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset >= data.Length)
        {
            return string.Empty;
        }

        var end = offset;
        while (end < data.Length && data[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(data[offset..end]);
    }
}
