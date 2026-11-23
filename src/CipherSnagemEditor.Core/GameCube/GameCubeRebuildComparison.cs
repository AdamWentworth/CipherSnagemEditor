using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeRebuildResult(int ComparedFiles, IReadOnlyList<string> Differences);

public static class GameCubeRebuildComparison
{
    private const int BootSize = 0x2440;
    private const int ApploaderHeaderSize = 0x20;

    public static GameCubeRebuildResult Compare(string expectedPath, string rebuiltPath)
    {
        using var expected = File.OpenRead(expectedPath);
        using var rebuilt = File.OpenRead(rebuiltPath);
        var left = Inspect(GameCubeIsoReader.Open(expectedPath), expected);
        var right = Inspect(GameCubeIsoReader.Open(rebuiltPath), rebuilt);
        var differences = new List<string>();
        var leftBuffer = new byte[128 * 1024];
        var rightBuffer = new byte[leftBuffer.Length];

        if (!left.Boot.AsSpan().SequenceEqual(right.Boot))
        {
            differences.Add("Boot header or BI2 differs.");
        }

        CompareSystemFile(left.Apploader, right.Apploader);
        CompareSystemFile(left.Dol, right.Dol);

        var compared = 0;
        foreach (var (path, file) in left.Files.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (!right.Files.TryGetValue(path, out var other))
            {
                differences.Add($"Missing file: {path}");
                continue;
            }

            compared++;
            if (SameBytes(file, other))
            {
                continue;
            }

            if (path.EndsWith(".fsys", StringComparison.OrdinalIgnoreCase))
            {
                DescribeArchiveDifference(path, expected, rebuilt, file, other, differences);
            }
            else
            {
                differences.Add($"{path}: file contents differ ({file.Size} vs {other.Size} bytes).");
            }
        }

        foreach (var path in right.Files.Keys.Except(left.Files.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            differences.Add($"Unexpected file: {path}");
        }

        return new GameCubeRebuildResult(compared, differences);

        void CompareSystemFile(GameCubeIsoFileEntry leftFile, GameCubeIsoFileEntry rightFile)
        {
            if (!SameBytes(leftFile, rightFile))
            {
                differences.Add($"{leftFile.Name}: system file contents differ.");
            }
        }

        bool SameBytes(GameCubeIsoFileEntry leftFile, GameCubeIsoFileEntry rightFile)
        {
            if (leftFile.Size != rightFile.Size)
            {
                return false;
            }

            expected.Position = leftFile.Offset;
            rebuilt.Position = rightFile.Offset;
            var remaining = leftFile.Size;
            while (remaining > 0)
            {
                var count = (int)Math.Min(remaining, (uint)leftBuffer.Length);
                expected.ReadExactly(leftBuffer.AsSpan(0, count));
                rebuilt.ReadExactly(rightBuffer.AsSpan(0, count));
                if (!leftBuffer.AsSpan(0, count).SequenceEqual(rightBuffer.AsSpan(0, count)))
                {
                    return false;
                }

                remaining -= (uint)count;
            }

            return true;
        }
    }

    private static DiscLayout Inspect(GameCubeIso iso, FileStream stream)
    {
        if (!iso.IsSupportedPokemonGame)
        {
            throw new InvalidDataException($"Expected a Colosseum or XD ISO: {iso.FileName}");
        }

        var boot = Read(stream, 0, BootSize);
        if (BigEndian.ReadUInt32(boot, 0x1c) != 0xc2339f3d)
        {
            throw new InvalidDataException($"Invalid GameCube disc header in {iso.FileName}.");
        }

        var appHeader = Read(stream, BootSize, ApploaderHeaderSize);
        var appSize = checked((uint)ApploaderHeaderSize + BigEndian.ReadUInt32(appHeader, 0x14)
                               + BigEndian.ReadUInt32(appHeader, 0x18));
        var apploader = new GameCubeIsoFileEntry("Apploader", BootSize, appSize);
        var dol = iso.Files.Single(file => file.TocEntryOffset is null && file.Name == "Start.dol");
        var toc = iso.Files.Single(file => file.TocEntryOffset is null && file.Name == "Game.toc");
        var dolHeader = Read(stream, dol.Offset, 0x100);
        var dolSize = 0x100u;
        for (var section = 0; section < 18; section++)
        {
            var size = BigEndian.ReadUInt32(dolHeader, 0x90 + 4 * section);
            if (size != 0)
            {
                var offset = BigEndian.ReadUInt32(dolHeader, 4 * section);
                if (offset < 0x100)
                {
                    throw new InvalidDataException($"Invalid DOL section {section} in {iso.FileName}.");
                }

                dolSize = Math.Max(dolSize, checked(offset + size));
            }
        }

        dol = dol with { Size = dolSize };
        var tocBytes = Read(stream, toc.Offset, toc.Size);
        if (tocBytes.Length < 12 || BigEndian.ReadUInt32(tocBytes, 0) != 0x01000000
            || BigEndian.ReadUInt32(tocBytes, 4) != 0)
        {
            throw new InvalidDataException($"Invalid file table in {iso.FileName}.");
        }

        var entries = BigEndian.ReadUInt32(tocBytes, 8);
        if (entries == 0 || entries * 12L > toc.Size)
        {
            throw new InvalidDataException($"Invalid file table entry count in {iso.FileName}.");
        }

        var files = new Dictionary<string, GameCubeIsoFileEntry>(StringComparer.Ordinal);
        foreach (var file in iso.Files.Where(file => file.TocEntryOffset is not null))
        {
            if (!files.TryAdd(file.DiscPath, file))
            {
                throw new InvalidDataException($"Duplicate FST path in {iso.FileName}: {file.DiscPath}");
            }
        }

        var declaredFiles = Enumerable.Range(1, checked((int)entries - 1)).Count(index => tocBytes[12 * index] == 0);
        if (files.Count == 0 || files.Count != declaredFiles)
        {
            throw new InvalidDataException($"Incomplete file table in {iso.FileName}.");
        }

        var end = (long)BootSize;
        foreach (var file in files.Values.Concat([apploader, dol, toc]).Where(file => file.Size > 0).OrderBy(file => file.Offset))
        {
            var fileEnd = (long)file.Offset + file.Size;
            if (file.Offset < end || fileEnd > stream.Length)
            {
                throw new InvalidDataException($"Overlapping or out-of-bounds file in {iso.FileName}: {file.DiscPath}");
            }

            end = fileEnd;
        }

        // These fields describe placement and capacity on disc.
        boot.AsSpan(0x420, 0x10).Clear();
        boot.AsSpan(0x434, 8).Clear();
        return new DiscLayout(boot, apploader, dol, files);
    }

    private static void DescribeArchiveDifference(string path, FileStream expected, FileStream rebuilt,
        GameCubeIsoFileEntry left, GameCubeIsoFileEntry right, List<string> differences)
    {
        var initialCount = differences.Count;
        try
        {
            var a = FsysArchive.Parse(Read(expected, left.Offset, left.Size));
            var b = FsysArchive.Parse(Read(rebuilt, right.Offset, right.Size));
            if (a.GroupId != b.GroupId || a.Entries.Count != b.Entries.Count)
            {
                differences.Add($"{path}: FSYS group ID or member count differs.");
            }

            foreach (var (entry, other) in a.Entries.Zip(b.Entries))
            {
                if (entry.Identifier != other.Identifier || entry.Name != other.Name)
                {
                    differences.Add($"{path}: member {entry.Index} changed identity ({entry.Name} 0x{entry.Identifier:X8} vs {other.Name} 0x{other.Identifier:X8}).");
                }
                else if (!a.Extract(entry).AsSpan().SequenceEqual(b.Extract(other)))
                {
                    differences.Add($"{path}/{entry.Name} (0x{entry.Identifier:X8}): member contents differ.");
                }
            }
        }
        catch (Exception error) when (error is InvalidDataException or ArgumentException or OverflowException or IndexOutOfRangeException)
        {
            differences.Add($"{path}: FSYS changed and could not be compared ({error.Message}).");
        }

        if (differences.Count == initialCount)
        {
            differences.Add($"{path}: archive bytes differ. Member names, identifiers, and decoded contents match.");
        }
    }

    private static byte[] Read(FileStream stream, uint offset, uint length)
    {
        if ((long)offset + length > stream.Length || length > int.MaxValue)
        {
            throw new InvalidDataException($"Invalid disc range 0x{offset:X}+0x{length:X}.");
        }

        stream.Position = offset;
        var bytes = new byte[length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private sealed record DiscLayout(byte[] Boot, GameCubeIsoFileEntry Apploader, GameCubeIsoFileEntry Dol,
        Dictionary<string, GameCubeIsoFileEntry> Files);
}
