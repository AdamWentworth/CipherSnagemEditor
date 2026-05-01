using System.Text;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public static class GameCubeLegacyFileCodecs
{
    private static readonly byte[] WzxDatHeaderTail =
    [
        0x00, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
    ];

    public static bool TryExportPkxDat(ReadOnlySpan<byte> pkx, out byte[] dat)
    {
        dat = [];
        if (pkx.Length < 0x44)
        {
            return false;
        }

        var length = checked((int)BigEndian.ReadUInt32(pkx, 0));
        const int start = 0x40;
        if (length <= 0 || start + length > pkx.Length)
        {
            return false;
        }

        dat = pkx.Slice(start, length).ToArray();
        return true;
    }

    public static bool TryImportPkxDat(ReadOnlySpan<byte> pkx, ReadOnlySpan<byte> dat, out byte[] importedPkx)
    {
        importedPkx = [];
        if (pkx.Length < 0x40 || dat.Length == 0)
        {
            return false;
        }

        var oldDatLength = checked((int)BigEndian.ReadUInt32(pkx, 0));
        var oldDatEnd = Align(0x40 + oldDatLength, 0x10);
        if (oldDatLength <= 0 || oldDatEnd > pkx.Length)
        {
            return false;
        }

        var paddedDatLength = Align(dat.Length, 0x10);
        importedPkx = new byte[0x40 + paddedDatLength + (pkx.Length - oldDatEnd)];
        pkx[..0x40].CopyTo(importedPkx);
        dat.CopyTo(importedPkx.AsSpan(0x40));
        pkx[oldDatEnd..].CopyTo(importedPkx.AsSpan(0x40 + paddedDatLength));
        BigEndian.WriteUInt32(importedPkx, 0, checked((uint)dat.Length));
        return true;
    }

    public static IReadOnlyList<EmbeddedDatModel> ExtractWzxDatModels(ReadOnlySpan<byte> wzx)
    {
        var models = new List<EmbeddedDatModel>();
        var searchStart = 0;
        while (searchStart < wzx.Length)
        {
            var markerOffset = IndexOf(wzx[searchStart..], WzxDatHeaderTail);
            if (markerOffset < 0)
            {
                break;
            }

            markerOffset += searchStart;
            var modelStart = markerOffset - 12;
            if (modelStart < 0)
            {
                searchStart = markerOffset + WzxDatHeaderTail.Length;
                continue;
            }

            var rawModelLength = BigEndian.ReadUInt32(wzx, modelStart);
            if (rawModelLength > int.MaxValue)
            {
                searchStart = markerOffset + WzxDatHeaderTail.Length;
                continue;
            }

            var modelLength = (int)rawModelLength;
            var modelEnd = (long)modelStart + modelLength;
            if (modelLength > 32
                && modelEnd <= wzx.Length
                && LooksLikeDatLength(wzx, modelStart, modelLength)
                && HasDatTrailer(wzx[modelStart..(int)modelEnd]))
            {
                models.Add(new EmbeddedDatModel(
                    models.Count,
                    modelStart,
                    modelLength,
                    wzx.Slice(modelStart, modelLength).ToArray()));
                searchStart = (int)modelEnd;
            }
            else
            {
                searchStart = markerOffset + WzxDatHeaderTail.Length;
            }
        }

        return models;
    }

    public static bool TryImportWzxDatModel(ReadOnlySpan<byte> wzx, int modelIndex, ReadOnlySpan<byte> model, out byte[] importedWzx)
    {
        importedWzx = [];
        var models = ExtractWzxDatModels(wzx);
        var target = models.FirstOrDefault(candidate => candidate.Index == modelIndex);
        if (target is null || target.Length != model.Length)
        {
            return false;
        }

        importedWzx = wzx.ToArray();
        model.CopyTo(importedWzx.AsSpan(target.Offset, target.Length));
        return true;
    }

    public static bool TrySplitThp(ReadOnlySpan<byte> thp, out byte[] header, out byte[] body)
    {
        header = [];
        body = [];
        if (thp.Length < 0x30)
        {
            return false;
        }

        var componentsOffset = checked((int)BigEndian.ReadUInt32(thp, 0x20));
        if (componentsOffset < 0 || componentsOffset + 20 > thp.Length)
        {
            return false;
        }

        var componentsLength = 0;
        var componentTypeStart = componentsOffset + 4;
        var componentTypeEnd = Math.Min(componentTypeStart + 16, thp.Length);
        for (var offset = componentTypeStart; offset < componentTypeEnd; offset++)
        {
            componentsLength += thp[offset] switch
            {
                0 => 12,
                1 => 16,
                _ => 0
            };
        }

        var headerLength = componentsOffset + 20 + componentsLength;
        if (headerLength <= 0 || headerLength > thp.Length)
        {
            return false;
        }

        header = thp[..headerLength].ToArray();
        body = thp[headerLength..].ToArray();
        AdjustThpBodyOffsets(header, -header.Length);
        return true;
    }

    public static byte[] CombineThp(ReadOnlySpan<byte> header, ReadOnlySpan<byte> body)
    {
        var thp = new byte[header.Length + body.Length];
        header.CopyTo(thp);
        body.CopyTo(thp.AsSpan(header.Length));
        AdjustThpBodyOffsets(thp, header.Length);
        return thp;
    }

    private static void AdjustThpBodyOffsets(Span<byte> data, int adjustment)
    {
        if (data.Length < 0x30)
        {
            return;
        }

        BigEndian.WriteUInt32(data, 0x28, checked((uint)(BigEndian.ReadUInt32(data, 0x28) + adjustment)));
        BigEndian.WriteUInt32(data, 0x2c, checked((uint)(BigEndian.ReadUInt32(data, 0x2c) + adjustment)));
    }

    private static bool LooksLikeDatLength(ReadOnlySpan<byte> data, int modelStart, int modelLength)
    {
        if (modelStart < 8)
        {
            return true;
        }

        foreach (var repeatOffset in new[] { 8, 20, 28 })
        {
            var offset = modelStart - repeatOffset;
            if (offset >= 0 && offset + 4 <= data.Length && BigEndian.ReadUInt32(data, offset) == modelLength)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDatTrailer(ReadOnlySpan<byte> model)
        => ContainsAsciiNearEnd(model, "scene_data") || ContainsAsciiNearEnd(model, "bound_box");

    private static bool ContainsAsciiNearEnd(ReadOnlySpan<byte> data, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        var start = Math.Max(0, data.Length - 64);
        return IndexOf(data[start..], bytes) >= 0;
    }

    private static int IndexOf(ReadOnlySpan<byte> data, ReadOnlySpan<byte> pattern)
    {
        if (pattern.Length == 0 || pattern.Length > data.Length)
        {
            return -1;
        }

        for (var offset = 0; offset <= data.Length - pattern.Length; offset++)
        {
            if (data.Slice(offset, pattern.Length).SequenceEqual(pattern))
            {
                return offset;
            }
        }

        return -1;
    }

    private static int Align(int value, int alignment)
        => ((value + alignment - 1) / alignment) * alignment;
}

public sealed record EmbeddedDatModel(int Index, int Offset, int Length, byte[] Data);
