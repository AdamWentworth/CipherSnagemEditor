using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeTexturePayload(byte[] PixelBytes, byte[] PaletteBytes);

public static class GameCubeTextureCodec
{
    private const int TextureWidthOffset = 0x00;
    private const int TextureHeightOffset = 0x02;
    private const int TextureBppOffset = 0x04;
    private const int TextureIndexOffset = 0x05;
    private const int TextureFormatOffset = 0x0b;
    private const int PaletteFormatOffset = 0x0f;
    private const int TexturePointerOffset = 0x28;
    private const int PalettePointerOffset = 0x48;
    private const int DefaultTextureStart = 0x80;
    private const int DancerStartOffset = 0x0c;
    private static readonly byte[] DancerBytes = [0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00];

    public static bool TryDecodePng(byte[] textureBytes, out byte[] pngBytes)
    {
        pngBytes = [];
        if (!TryParse(textureBytes, out var info))
        {
            return false;
        }

        var image = DecodeImage(info);
        pngBytes = PngRgbaCodec.Encode(info.Width, info.Height, image);
        return true;
    }

    public static bool TryImportPng(byte[] textureBytes, byte[] pngBytes, out byte[] updatedTextureBytes)
    {
        updatedTextureBytes = [];
        if (!TryParse(textureBytes, out var info))
        {
            return false;
        }

        var image = PngRgbaCodec.Decode(pngBytes);
        var textureData = EncodeTextureData(info, image);
        var result = textureBytes.ToArray();
        EnsureWritableRange(result, info.TextureStart, textureData.Length);
        textureData.CopyTo(result, info.TextureStart);

        if (info.IsIndexed)
        {
            var paletteData = EncodePaletteData(info, image);
            EnsureWritableRange(result, info.PaletteStart, paletteData.Length);
            paletteData.CopyTo(result, info.PaletteStart);
        }

        WriteMetadata(result, info);
        updatedTextureBytes = result;
        return true;
    }

    public static bool TryGetPayload(byte[] textureBytes, int? maxPaletteEntries, out GameCubeTexturePayload payload)
    {
        payload = new GameCubeTexturePayload([], []);
        if (!TryParse(textureBytes, out var info))
        {
            return false;
        }

        var pixelBytes = info.Data.AsSpan(info.TextureStart, info.ExpectedTextureLength).ToArray();
        if (info.IsIndexed && maxPaletteEntries is > 0)
        {
            ClampIndexedPixels(pixelBytes, info.Format, maxPaletteEntries.Value);
        }

        var paletteBytes = Array.Empty<byte>();
        if (info.IsIndexed)
        {
            var paletteEntries = maxPaletteEntries is > 0
                ? Math.Min(info.PaletteCount, maxPaletteEntries.Value)
                : info.PaletteCount;
            paletteBytes = info.Data.AsSpan(info.PaletteStart, paletteEntries * 2).ToArray();
        }

        payload = new GameCubeTexturePayload(pixelBytes, paletteBytes);
        return true;
    }

    public static bool TryCreateModelTexture(
        int width,
        int height,
        int standardFormat,
        int paletteFormatId,
        byte[] pixelBytes,
        byte[] paletteBytes,
        out byte[] textureBytes)
    {
        textureBytes = [];
        if (width <= 0 || height <= 0 || !TryFormatFromStandardRawValue(standardFormat, out var format))
        {
            return false;
        }

        var paddedWidth = Align(width, BlockWidth(format));
        var paddedHeight = Align(height, BlockHeight(format));
        var textureLength = checked(paddedWidth * paddedHeight * BitsPerPixel(format) / 8);
        if (pixelBytes.Length < textureLength)
        {
            return false;
        }

        var paletteFormat = PaletteFormatFromId((byte)paletteFormatId);
        var paletteLength = IsIndexed(format) ? StandardPaletteCount(format) * 2 : 0;
        var paletteStart = DefaultTextureStart + textureLength;
        textureBytes = new byte[DefaultTextureStart + textureLength + paletteLength];
        BigEndian.WriteUInt16(textureBytes, TextureWidthOffset, (ushort)width);
        BigEndian.WriteUInt16(textureBytes, TextureHeightOffset, (ushort)height);
        textureBytes[TextureBppOffset] = (byte)BitsPerPixel(format);
        textureBytes[TextureIndexOffset] = IsIndexed(format) ? (byte)1 : (byte)0;
        textureBytes[TextureFormatOffset] = (byte)format;
        textureBytes[PaletteFormatOffset] = PaletteId(paletteFormat);
        BigEndian.WriteUInt32(textureBytes, TexturePointerOffset, DefaultTextureStart);
        BigEndian.WriteUInt32(textureBytes, PalettePointerOffset, checked((uint)paletteStart));
        pixelBytes.AsSpan(0, textureLength).CopyTo(textureBytes.AsSpan(DefaultTextureStart));

        if (paletteLength > 0)
        {
            paletteBytes.AsSpan(0, Math.Min(paletteBytes.Length, paletteLength)).CopyTo(textureBytes.AsSpan(paletteStart));
        }

        return true;
    }

    private static byte[] DecodeImage(TextureInfo info)
    {
        var pixels = new RgbaColor[info.PaddedWidth * info.PaddedHeight];
        var palette = DecodePalette(info);

        if (info.Format == TextureFormat.Rgba32)
        {
            DecodeRgba32(info, pixels);
        }
        else if (info.Format == TextureFormat.Cmpr)
        {
            DecodeCmpr(info, pixels);
        }
        else
        {
            DecodeTiledPixels(info, palette, pixels);
        }

        var rgba = new byte[checked(info.Width * info.Height * 4)];
        for (var y = 0; y < info.Height; y++)
        {
            for (var x = 0; x < info.Width; x++)
            {
                WriteRgba(rgba, (y * info.Width + x) * 4, pixels[y * info.PaddedWidth + x]);
            }
        }

        return rgba;
    }

    private static void DecodeTiledPixels(TextureInfo info, IReadOnlyList<RgbaColor> palette, RgbaColor[] pixels)
    {
        var reader = new TextureBitReader(info.Data, info.TextureStart, info.ExpectedTextureLength);

        foreach (var (x, y) in EnumerateBlockPixels(info))
        {
            var raw = reader.Read(info.BitsPerPixel);
            pixels[y * info.PaddedWidth + x] = info.IsIndexed
                ? raw >= 0 && raw < palette.Count ? palette[raw] : RgbaColor.Transparent
                : FromRaw(raw, info.Format);
        }
    }

    private static void DecodeRgba32(TextureInfo info, RgbaColor[] pixels)
    {
        var offset = info.TextureStart;
        foreach (var (blockX, blockY) in EnumerateBlocks(info))
        {
            EnsureReadableRange(info.Data, offset, 64);
            for (var pixel = 0; pixel < 16; pixel++)
            {
                var ar = BigEndian.ReadUInt16(info.Data, offset + pixel * 2);
                var gb = BigEndian.ReadUInt16(info.Data, offset + 32 + pixel * 2);
                var x = blockX + pixel % 4;
                var y = blockY + pixel / 4;
                pixels[y * info.PaddedWidth + x] = new RgbaColor(
                    (byte)(ar & 0xff),
                    (byte)(gb >> 8),
                    (byte)(gb & 0xff),
                    (byte)(ar >> 8));
            }

            offset += 64;
        }
    }

    private static void DecodeCmpr(TextureInfo info, RgbaColor[] pixels)
    {
        var offset = info.TextureStart;
        foreach (var (blockX, blockY) in EnumerateBlocks(info))
        {
            for (var subBlock = 0; subBlock < 4; subBlock++)
            {
                EnsureReadableRange(info.Data, offset, 8);
                var color0 = BigEndian.ReadUInt16(info.Data, offset);
                var color1 = BigEndian.ReadUInt16(info.Data, offset + 2);
                var indexes = BigEndian.ReadUInt32(info.Data, offset + 4);
                var palette = BuildCmprPalette(color0, color1);
                var subX = blockX + (subBlock % 2) * 4;
                var subY = blockY + (subBlock / 2) * 4;

                for (var i = 0; i < 16; i++)
                {
                    var index = (int)((indexes >> ((15 - i) * 2)) & 0x03);
                    var x = subX + i % 4;
                    var y = subY + i / 4;
                    pixels[y * info.PaddedWidth + x] = palette[index];
                }

                offset += 8;
            }
        }
    }

    private static byte[] EncodeTextureData(TextureInfo info, PngImage image)
    {
        return info.Format switch
        {
            TextureFormat.Rgba32 => EncodeRgba32(info, image),
            TextureFormat.Cmpr => EncodeCmpr(info, image),
            _ => EncodeTiledPixels(info, image)
        };
    }

    private static byte[] EncodeTiledPixels(TextureInfo info, PngImage image)
    {
        var values = new List<int>(info.PaddedWidth * info.PaddedHeight);
        var palette = info.IsIndexed ? BuildIndexedPalette(info, image) : [];

        foreach (var (x, y) in EnumerateBlockPixels(info))
        {
            var color = GetImagePixel(info, image, x, y);
            values.Add(info.IsIndexed ? PaletteIndex(palette, color) : ToRaw(color, info.Format));
        }

        var writer = new TextureBitWriter(info.ExpectedTextureLength);
        foreach (var value in values)
        {
            writer.Write(value, info.BitsPerPixel);
        }

        return writer.ToArray();
    }

    private static byte[] EncodeRgba32(TextureInfo info, PngImage image)
    {
        var output = new byte[info.ExpectedTextureLength];
        var offset = 0;
        foreach (var (blockX, blockY) in EnumerateBlocks(info))
        {
            for (var pixel = 0; pixel < 16; pixel++)
            {
                var color = GetImagePixel(info, image, blockX + pixel % 4, blockY + pixel / 4);
                BigEndian.WriteUInt16(output, offset + pixel * 2, (ushort)((color.A << 8) | color.R));
                BigEndian.WriteUInt16(output, offset + 32 + pixel * 2, (ushort)((color.G << 8) | color.B));
            }

            offset += 64;
        }

        return output;
    }

    private static byte[] EncodeCmpr(TextureInfo info, PngImage image)
    {
        var output = new byte[info.ExpectedTextureLength];
        var offset = 0;
        foreach (var (blockX, blockY) in EnumerateBlocks(info))
        {
            for (var subBlock = 0; subBlock < 4; subBlock++)
            {
                var subX = blockX + (subBlock % 2) * 4;
                var subY = blockY + (subBlock / 2) * 4;
                var colors = new RgbaColor[16];
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = GetImagePixel(info, image, subX + i % 4, subY + i / 4);
                }

                var compressed = CompressCmprSubBlock(colors);
                compressed.CopyTo(output, offset);
                offset += compressed.Length;
            }
        }

        return output;
    }

    private static byte[] CompressCmprSubBlock(IReadOnlyList<RgbaColor> colors)
    {
        var unique = colors
            .Where(color => color.A >= 0x80)
            .Select(color => (int)ToRgb565(color))
            .Distinct()
            .ToArray();
        var output = new byte[8];

        if (unique.Length == 0)
        {
            output.AsSpan(4, 4).Fill(0xff);
            return output;
        }

        ushort color0;
        ushort color1;
        if (unique.Length == 1)
        {
            color0 = color1 = (ushort)unique[0];
        }
        else
        {
            var best0 = unique[0];
            var best1 = unique[1];
            var greatestRange = -1;
            foreach (var test0 in unique)
            {
                foreach (var test1 in unique)
                {
                    var range = ColorRange(FromRgb565((ushort)test0), FromRgb565((ushort)test1));
                    if (range <= greatestRange)
                    {
                        continue;
                    }

                    greatestRange = range;
                    best0 = test0;
                    best1 = test1;
                }
            }

            var hasTransparency = colors.Any(color => color.A < 0x80);
            color0 = (ushort)(hasTransparency ? Math.Min(best0, best1) : Math.Max(best0, best1));
            color1 = (ushort)(hasTransparency ? Math.Max(best0, best1) : Math.Min(best0, best1));
        }

        var palette = BuildCmprPalette(color0, color1);
        uint indexes = 0;
        foreach (var color in colors)
        {
            indexes <<= 2;
            indexes |= (uint)(color.A < 0x80 ? 3 : ClosestPaletteIndex(palette, color));
        }

        BigEndian.WriteUInt16(output, 0, color0);
        BigEndian.WriteUInt16(output, 2, color1);
        BigEndian.WriteUInt32(output, 4, indexes);
        return output;
    }

    private static byte[] EncodePaletteData(TextureInfo info, PngImage image)
    {
        var palette = BuildIndexedPalette(info, image);
        var output = new byte[info.PaletteCount * 2];
        for (var i = 0; i < info.PaletteCount; i++)
        {
            var color = i < palette.Count ? palette[i] : RgbaColor.Transparent;
            BigEndian.WriteUInt16(output, i * 2, (ushort)ToRaw(color, info.PaletteFormat));
        }

        return output;
    }

    private static List<RgbaColor> BuildIndexedPalette(TextureInfo info, PngImage image)
    {
        var palette = new List<RgbaColor>(info.PaletteCount);
        foreach (var (x, y) in EnumerateBlockPixels(info))
        {
            var color = Quantize(GetImagePixel(info, image, x, y), info.PaletteFormat);
            if (palette.Any(existing => ColorsEquivalent(existing, color)))
            {
                continue;
            }

            if (palette.Count >= info.PaletteCount)
            {
                break;
            }

            palette.Add(color);
        }

        if (palette.Count == 0)
        {
            palette.Add(RgbaColor.Transparent);
        }

        return palette;
    }

    private static int PaletteIndex(IReadOnlyList<RgbaColor> palette, RgbaColor color)
    {
        for (var i = 0; i < palette.Count; i++)
        {
            if (ColorsEquivalent(palette[i], color))
            {
                return i;
            }
        }

        return 0;
    }

    private static IReadOnlyList<RgbaColor> DecodePalette(TextureInfo info)
    {
        if (!info.IsIndexed)
        {
            return [];
        }

        var palette = new RgbaColor[info.PaletteCount];
        for (var i = 0; i < palette.Length; i++)
        {
            palette[i] = FromRaw(BigEndian.ReadUInt16(info.Data, info.PaletteStart + i * 2), info.PaletteFormat);
        }

        return palette;
    }

    private static IEnumerable<(int X, int Y)> EnumerateBlocks(TextureInfo info)
    {
        for (var blockY = 0; blockY < info.PaddedHeight; blockY += info.BlockHeight)
        {
            for (var blockX = 0; blockX < info.PaddedWidth; blockX += info.BlockWidth)
            {
                yield return (blockX, blockY);
            }
        }
    }

    private static IEnumerable<(int X, int Y)> EnumerateBlockPixels(TextureInfo info)
    {
        foreach (var (blockX, blockY) in EnumerateBlocks(info))
        {
            for (var row = 0; row < info.BlockHeight; row++)
            {
                for (var column = 0; column < info.BlockWidth; column++)
                {
                    yield return (blockX + column, blockY + row);
                }
            }
        }
    }

    private static RgbaColor GetImagePixel(TextureInfo info, PngImage image, int x, int y)
    {
        if (x >= info.Width || y >= info.Height || x >= image.Width || y >= image.Height)
        {
            return RgbaColor.Transparent;
        }

        var offset = checked((y * image.Width + x) * 4);
        return new RgbaColor(image.Rgba[offset], image.Rgba[offset + 1], image.Rgba[offset + 2], image.Rgba[offset + 3]);
    }

    private static RgbaColor FromRaw(int raw, TextureFormat format)
    {
        return format switch
        {
            TextureFormat.I4 => new RgbaColor((byte)(raw * 0x11), (byte)(raw * 0x11), (byte)(raw * 0x11), 0xff),
            TextureFormat.I8 => new RgbaColor((byte)raw, (byte)raw, (byte)raw, 0xff),
            TextureFormat.Ia4 => new RgbaColor((byte)((raw & 0x0f) * 0x11), (byte)((raw & 0x0f) * 0x11), (byte)((raw & 0x0f) * 0x11), (byte)((raw >> 4) * 0x11)),
            TextureFormat.Ia8 => new RgbaColor((byte)(raw & 0xff), (byte)(raw & 0xff), (byte)(raw & 0xff), (byte)(raw >> 8)),
            TextureFormat.Rgb565 => FromRgb565((ushort)raw),
            TextureFormat.Rgb5A3 => FromRgb5A3((ushort)raw),
            TextureFormat.Cmpr => FromRgb565((ushort)raw),
            _ => RgbaColor.Transparent
        };
    }

    private static RgbaColor FromRgb565(ushort raw)
        => new((byte)(((raw >> 11) & 0x1f) * 8), (byte)(((raw >> 5) & 0x3f) * 4), (byte)((raw & 0x1f) * 8), 0xff);

    private static RgbaColor FromRgb5A3(ushort raw)
    {
        if ((raw & 0x8000) != 0)
        {
            return new RgbaColor(
                (byte)(((raw >> 10) & 0x1f) * 8),
                (byte)(((raw >> 5) & 0x1f) * 8),
                (byte)((raw & 0x1f) * 8),
                0xff);
        }

        return new RgbaColor(
            (byte)(((raw >> 8) & 0x0f) * 0x11),
            (byte)(((raw >> 4) & 0x0f) * 0x11),
            (byte)((raw & 0x0f) * 0x11),
            (byte)(((raw >> 12) & 0x07) * 0x20));
    }

    private static int ToRaw(RgbaColor color, TextureFormat format)
    {
        return format switch
        {
            TextureFormat.I4 => ((color.R + color.G + color.B) / 3) / 0x11,
            TextureFormat.I8 => (color.R + color.G + color.B) / 3,
            TextureFormat.Ia4 => ((color.A / 0x11) << 4) | (((color.R + color.G + color.B) / 3) / 0x11),
            TextureFormat.Ia8 => (color.A << 8) | ((color.R + color.G + color.B) / 3),
            TextureFormat.Rgb565 => ToRgb565(color),
            TextureFormat.Rgb5A3 => ToRgb5A3(color),
            TextureFormat.Cmpr => ToRgb565(color),
            _ => 0
        };
    }

    private static ushort ToRgb565(RgbaColor color)
        => (ushort)(((color.R / 8) << 11) | ((color.G / 4) << 5) | (color.B / 8));

    private static ushort ToRgb5A3(RgbaColor color)
    {
        if (color.A == 0xff)
        {
            return (ushort)(0x8000 | ((color.R / 8) << 10) | ((color.G / 8) << 5) | (color.B / 8));
        }

        return (ushort)(((color.A / 0x20) << 12) | ((color.R / 0x11) << 8) | ((color.G / 0x11) << 4) | (color.B / 0x11));
    }

    private static RgbaColor Quantize(RgbaColor color, TextureFormat paletteFormat)
        => FromRaw(ToRaw(color, paletteFormat), paletteFormat);

    private static bool ColorsEquivalent(RgbaColor left, RgbaColor right)
    {
        if (left.A == 0 && right.A == 0)
        {
            return true;
        }

        return left.Equals(right);
    }

    private static RgbaColor[] BuildCmprPalette(ushort color0, ushort color1)
    {
        var first = FromRgb565(color0);
        var second = FromRgb565(color1);
        if (color0 > color1)
        {
            return
            [
                first,
                second,
                new RgbaColor((byte)((2 * first.R + second.R) / 3), (byte)((2 * first.G + second.G) / 3), (byte)((2 * first.B + second.B) / 3), 0xff),
                new RgbaColor((byte)((2 * second.R + first.R) / 3), (byte)((2 * second.G + first.G) / 3), (byte)((2 * second.B + first.B) / 3), 0xff)
            ];
        }

        return
        [
            first,
            second,
            new RgbaColor((byte)((first.R + second.R) / 2), (byte)((first.G + second.G) / 2), (byte)((first.B + second.B) / 2), 0xff),
            RgbaColor.Transparent
        ];
    }

    private static int ClosestPaletteIndex(IReadOnlyList<RgbaColor> palette, RgbaColor color)
    {
        var index = 0;
        var smallestRange = int.MaxValue;
        for (var i = 0; i < palette.Count; i++)
        {
            var range = ColorRange(color, palette[i]);
            if (range >= smallestRange)
            {
                continue;
            }

            smallestRange = range;
            index = i;
        }

        return index;
    }

    private static int ColorRange(RgbaColor left, RgbaColor right)
        => Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);

    private static void WriteRgba(byte[] bytes, int offset, RgbaColor color)
    {
        bytes[offset] = color.R;
        bytes[offset + 1] = color.G;
        bytes[offset + 2] = color.B;
        bytes[offset + 3] = color.A;
    }

    private static void WriteMetadata(byte[] bytes, TextureInfo info)
    {
        BigEndian.WriteUInt16(bytes, info.StartOffset + TextureWidthOffset, (ushort)info.Width);
        BigEndian.WriteUInt16(bytes, info.StartOffset + TextureHeightOffset, (ushort)info.Height);
        bytes[info.StartOffset + TextureBppOffset] = (byte)info.BitsPerPixel;
        bytes[info.StartOffset + TextureIndexOffset] = info.IsIndexed ? (byte)1 : (byte)0;
        bytes[info.StartOffset + TextureFormatOffset] = (byte)info.Format;
        bytes[info.StartOffset + PaletteFormatOffset] = PaletteId(info.PaletteFormat);
        BigEndian.WriteUInt32(bytes, info.StartOffset + TexturePointerOffset, checked((uint)(info.TextureStart - info.StartOffset)));
        BigEndian.WriteUInt32(bytes, info.StartOffset + PalettePointerOffset, checked((uint)(info.PaletteStart - info.StartOffset)));
    }

    private static void ClampIndexedPixels(byte[] pixelBytes, TextureFormat format, int maxPaletteEntries)
    {
        if (maxPaletteEntries <= 0)
        {
            return;
        }

        var maxIndex = maxPaletteEntries - 1;
        switch (format)
        {
            case TextureFormat.C4:
                for (var i = 0; i < pixelBytes.Length; i++)
                {
                    var high = Math.Min(pixelBytes[i] >> 4, maxIndex);
                    var low = Math.Min(pixelBytes[i] & 0x0f, maxIndex);
                    pixelBytes[i] = (byte)((high << 4) | low);
                }

                break;
            case TextureFormat.C8:
                for (var i = 0; i < pixelBytes.Length; i++)
                {
                    pixelBytes[i] = (byte)Math.Min(pixelBytes[i], maxIndex);
                }

                break;
            case TextureFormat.C14X2:
                for (var i = 0; i + 1 < pixelBytes.Length; i += 2)
                {
                    var index = Math.Min(BigEndian.ReadUInt16(pixelBytes, i), maxIndex);
                    BigEndian.WriteUInt16(pixelBytes, i, (ushort)index);
                }

                break;
        }
    }

    private static bool TryParse(byte[] data, out TextureInfo info)
    {
        info = default;
        if (data.Length < 0x50)
        {
            return false;
        }

        var startOffset = StartsWith(data, DancerBytes)
            ? checked((int)BigEndian.ReadUInt32(data, DancerStartOffset))
            : 0;
        if (startOffset < 0 || startOffset + 0x50 > data.Length)
        {
            return false;
        }

        var width = BigEndian.ReadUInt16(data, startOffset + TextureWidthOffset);
        var height = BigEndian.ReadUInt16(data, startOffset + TextureHeightOffset);
        if (width == 0 || height == 0)
        {
            return false;
        }

        if (!Enum.IsDefined(typeof(TextureFormat), (int)data[startOffset + TextureFormatOffset]))
        {
            return false;
        }

        var format = (TextureFormat)data[startOffset + TextureFormatOffset];
        var textureStart = startOffset + checked((int)BigEndian.ReadUInt32(data, startOffset + TexturePointerOffset));
        var paletteStart = startOffset + checked((int)BigEndian.ReadUInt32(data, startOffset + PalettePointerOffset));
        var blockWidth = BlockWidth(format);
        var blockHeight = BlockHeight(format);
        var paddedWidth = Align(width, blockWidth);
        var paddedHeight = Align(height, blockHeight);
        var bitsPerPixel = BitsPerPixel(format);
        var textureLength = checked(paddedWidth * paddedHeight * bitsPerPixel / 8);
        if (!IsReadableRange(data, textureStart, textureLength))
        {
            return false;
        }

        var paletteFormat = PaletteFormatFromId(data[startOffset + PaletteFormatOffset]);
        var paletteCount = 0;
        if (IsIndexed(format))
        {
            if (paletteStart < 0 || paletteStart >= data.Length)
            {
                return false;
            }

            var availablePaletteEntries = (data.Length - paletteStart) / 2;
            paletteCount = Math.Min(availablePaletteEntries, StandardPaletteCount(format));
            if (paletteCount <= 0)
            {
                return false;
            }
        }

        info = new TextureInfo(
            data,
            startOffset,
            width,
            height,
            paddedWidth,
            paddedHeight,
            textureStart,
            paletteStart,
            textureLength,
            paletteCount,
            format,
            paletteFormat);
        return true;
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
        => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    private static int Align(int value, int alignment)
        => (value + alignment - 1) / alignment * alignment;

    private static int BitsPerPixel(TextureFormat format)
        => format switch
        {
            TextureFormat.I4 or TextureFormat.C4 or TextureFormat.Cmpr => 4,
            TextureFormat.I8 or TextureFormat.Ia4 or TextureFormat.C8 => 8,
            TextureFormat.Ia8 or TextureFormat.Rgb565 or TextureFormat.Rgb5A3 or TextureFormat.C14X2 => 16,
            TextureFormat.Rgba32 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

    private static int BlockWidth(TextureFormat format)
        => format is TextureFormat.Ia8 or TextureFormat.Rgb565 or TextureFormat.Rgb5A3 or TextureFormat.Rgba32 or TextureFormat.C14X2
            ? 4
            : 8;

    private static int BlockHeight(TextureFormat format)
        => format is TextureFormat.I4 or TextureFormat.C4 or TextureFormat.Cmpr ? 8 : 4;

    private static bool IsIndexed(TextureFormat format)
        => format is TextureFormat.C4 or TextureFormat.C8 or TextureFormat.C14X2;

    private static int StandardPaletteCount(TextureFormat format)
        => format switch
        {
            TextureFormat.C4 => 16,
            TextureFormat.C8 => 256,
            TextureFormat.C14X2 => 16_384,
            _ => 0
        };

    private static TextureFormat PaletteFormatFromId(byte paletteId)
        => paletteId switch
        {
            1 => TextureFormat.Rgb565,
            2 or 3 => TextureFormat.Rgb5A3,
            _ => TextureFormat.Ia8
        };

    private static bool TryFormatFromStandardRawValue(int standardRawValue, out TextureFormat format)
    {
        format = standardRawValue switch
        {
            0 => TextureFormat.I4,
            1 => TextureFormat.I8,
            2 => TextureFormat.Ia4,
            3 => TextureFormat.Ia8,
            4 => TextureFormat.Rgb565,
            5 => TextureFormat.Rgb5A3,
            6 => TextureFormat.Rgba32,
            8 => TextureFormat.C4,
            9 => TextureFormat.C8,
            10 => TextureFormat.C14X2,
            14 => TextureFormat.Cmpr,
            _ => default
        };
        return standardRawValue is 0 or 1 or 2 or 3 or 4 or 5 or 6 or 8 or 9 or 10 or 14;
    }

    private static byte PaletteId(TextureFormat format)
        => format switch
        {
            TextureFormat.Rgb565 => 1,
            TextureFormat.Rgb5A3 => 3,
            _ => 0
        };

    private static void EnsureReadableRange(byte[] bytes, int offset, int length)
    {
        if (!IsReadableRange(bytes, offset, length))
        {
            throw new InvalidDataException($"Texture data ended before offset 0x{offset:x} length 0x{length:x}.");
        }
    }

    private static void EnsureWritableRange(byte[] bytes, int offset, int length)
    {
        if (!IsReadableRange(bytes, offset, length))
        {
            throw new InvalidDataException($"Texture replacement does not fit at offset 0x{offset:x} length 0x{length:x}.");
        }
    }

    private static bool IsReadableRange(byte[] bytes, int offset, int length)
        => offset >= 0 && length >= 0 && offset + length <= bytes.Length;

    private enum TextureFormat
    {
        C4 = 0x00,
        C8 = 0x01,
        C14X2 = 0x30,
        I4 = 0x40,
        Ia4 = 0x41,
        I8 = 0x42,
        Ia8 = 0x43,
        Rgb565 = 0x44,
        Rgba32 = 0x45,
        Rgb5A3 = 0x90,
        Cmpr = 0xb0
    }

    private readonly record struct TextureInfo(
        byte[] Data,
        int StartOffset,
        int Width,
        int Height,
        int PaddedWidth,
        int PaddedHeight,
        int TextureStart,
        int PaletteStart,
        int ExpectedTextureLength,
        int PaletteCount,
        TextureFormat Format,
        TextureFormat PaletteFormat)
    {
        public int BitsPerPixel => GameCubeTextureCodec.BitsPerPixel(Format);

        public int BlockWidth => GameCubeTextureCodec.BlockWidth(Format);

        public int BlockHeight => GameCubeTextureCodec.BlockHeight(Format);

        public bool IsIndexed => GameCubeTextureCodec.IsIndexed(Format);
    }

    private readonly record struct RgbaColor(byte R, byte G, byte B, byte A)
    {
        public static RgbaColor Transparent { get; } = new(0, 0, 0, 0);
    }

    private sealed record PngImage(int Width, int Height, byte[] Rgba);

    private ref struct TextureBitReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private readonly int _end;
        private int _offset;
        private bool _highNibble = true;

        public TextureBitReader(ReadOnlySpan<byte> data, int offset, int length)
        {
            _data = data;
            _offset = offset;
            _end = offset + length;
        }

        public int Read(int bitsPerPixel)
        {
            if (bitsPerPixel == 4)
            {
                if (_offset >= _end)
                {
                    return 0;
                }

                var value = _highNibble ? _data[_offset] >> 4 : _data[_offset] & 0x0f;
                if (!_highNibble)
                {
                    _offset++;
                }

                _highNibble = !_highNibble;
                return value;
            }

            if (bitsPerPixel == 8)
            {
                return _offset < _end ? _data[_offset++] : 0;
            }

            if (bitsPerPixel == 16)
            {
                if (_offset + 2 > _end)
                {
                    return 0;
                }

                var value = BigEndian.ReadUInt16(_data, _offset);
                _offset += 2;
                return value;
            }

            throw new NotSupportedException($"{bitsPerPixel}-bit tiled texture reads are not supported here.");
        }
    }

    private sealed class TextureBitWriter(int capacity)
    {
        private readonly byte[] _bytes = new byte[capacity];
        private int _offset;
        private bool _highNibble = true;

        public void Write(int value, int bitsPerPixel)
        {
            if (bitsPerPixel == 4)
            {
                if (_offset >= _bytes.Length)
                {
                    return;
                }

                if (_highNibble)
                {
                    _bytes[_offset] = (byte)((value & 0x0f) << 4);
                }
                else
                {
                    _bytes[_offset] |= (byte)(value & 0x0f);
                    _offset++;
                }

                _highNibble = !_highNibble;
                return;
            }

            if (bitsPerPixel == 8)
            {
                if (_offset < _bytes.Length)
                {
                    _bytes[_offset++] = (byte)value;
                }

                return;
            }

            if (bitsPerPixel == 16)
            {
                if (_offset + 2 <= _bytes.Length)
                {
                    BigEndian.WriteUInt16(_bytes, _offset, (ushort)value);
                    _offset += 2;
                }

                return;
            }

            throw new NotSupportedException($"{bitsPerPixel}-bit tiled texture writes are not supported here.");
        }

        public byte[] ToArray()
            => _bytes;
    }

    private static class PngRgbaCodec
    {
        private static readonly byte[] Signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
        private static readonly uint[] CrcTable = CreateCrcTable();

        public static byte[] Encode(int width, int height, byte[] rgba)
        {
            if (rgba.Length != checked(width * height * 4))
            {
                throw new ArgumentException("RGBA buffer length does not match PNG dimensions.", nameof(rgba));
            }

            var scanlines = new byte[checked((width * 4 + 1) * height)];
            var sourceOffset = 0;
            var targetOffset = 0;
            for (var y = 0; y < height; y++)
            {
                scanlines[targetOffset++] = 0;
                Buffer.BlockCopy(rgba, sourceOffset, scanlines, targetOffset, width * 4);
                sourceOffset += width * 4;
                targetOffset += width * 4;
            }

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(scanlines);
            }

            using var output = new MemoryStream();
            output.Write(Signature);
            Span<byte> ihdr = stackalloc byte[13];
            BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
            BinaryPrimitives.WriteUInt32BigEndian(ihdr[4..], (uint)height);
            ihdr[8] = 8;
            ihdr[9] = 6;
            WriteChunk(output, "IHDR", ihdr);
            WriteChunk(output, "IDAT", compressed.ToArray());
            WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
            return output.ToArray();
        }

        public static PngImage Decode(byte[] bytes)
        {
            if (bytes.Length < Signature.Length || !bytes.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            {
                throw new InvalidDataException("Not a PNG file.");
            }

            var png = new ParsedPng();
            var offset = Signature.Length;
            while (offset + 12 <= bytes.Length)
            {
                var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
                var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                var dataOffset = offset + 8;
                if (dataOffset + length + 4 > bytes.Length)
                {
                    throw new InvalidDataException("PNG chunk length extends beyond the file.");
                }

                var data = bytes.AsSpan(dataOffset, length);
                switch (type)
                {
                    case "IHDR":
                        png.Width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data[..4]));
                        png.Height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data[4..8]));
                        png.BitDepth = data[8];
                        png.ColorType = data[9];
                        png.InterlaceMethod = data[12];
                        break;
                    case "PLTE":
                        png.Palette = data.ToArray();
                        break;
                    case "tRNS":
                        png.Transparency = data.ToArray();
                        break;
                    case "IDAT":
                        png.ImageChunks.Add(data.ToArray());
                        break;
                    case "IEND":
                        offset = bytes.Length;
                        continue;
                }

                offset += length + 12;
            }

            Validate(png);
            var compressed = Concatenate(png.ImageChunks);
            using var source = new MemoryStream(compressed);
            using var inflater = new ZLibStream(source, CompressionMode.Decompress);
            using var decoded = new MemoryStream();
            inflater.CopyTo(decoded);

            var scanlines = Unfilter(decoded.ToArray(), png.Width, png.Height, png.ColorType, png.BitDepth);
            return new PngImage(png.Width, png.Height, ToRgba(scanlines, png));
        }

        private static void Validate(ParsedPng png)
        {
            if (png.Width <= 0 || png.Height <= 0)
            {
                throw new InvalidDataException("PNG missing IHDR dimensions.");
            }

            if (png.BitDepth != 8)
            {
                throw new NotSupportedException("Only 8-bit PNG images are supported for texture import.");
            }

            if (png.InterlaceMethod != 0)
            {
                throw new NotSupportedException("Interlaced PNG images are not supported for texture import.");
            }

            if (png.ImageChunks.Count == 0)
            {
                throw new InvalidDataException("PNG has no image data.");
            }
        }

        private static byte[] Unfilter(byte[] raw, int width, int height, int colorType, int bitDepth)
        {
            var bitsPerPixel = BitsPerPixel(colorType, bitDepth);
            var stride = checked((bitsPerPixel * width + 7) / 8);
            var bytesPerPixel = Math.Max(1, (bitsPerPixel + 7) / 8);
            var expectedLength = checked((stride + 1) * height);
            if (raw.Length < expectedLength)
            {
                throw new InvalidDataException("PNG image data ended before all scanlines were decoded.");
            }

            var result = new byte[stride * height];
            var rawOffset = 0;
            for (var y = 0; y < height; y++)
            {
                var filter = raw[rawOffset++];
                var rowOffset = y * stride;
                var previousRowOffset = rowOffset - stride;

                for (var x = 0; x < stride; x++)
                {
                    var value = raw[rawOffset++];
                    var left = x >= bytesPerPixel ? result[rowOffset + x - bytesPerPixel] : (byte)0;
                    var up = y > 0 ? result[previousRowOffset + x] : (byte)0;
                    var upLeft = y > 0 && x >= bytesPerPixel ? result[previousRowOffset + x - bytesPerPixel] : (byte)0;

                    result[rowOffset + x] = filter switch
                    {
                        0 => value,
                        1 => unchecked((byte)(value + left)),
                        2 => unchecked((byte)(value + up)),
                        3 => unchecked((byte)(value + ((left + up) / 2))),
                        4 => unchecked((byte)(value + Paeth(left, up, upLeft))),
                        _ => throw new InvalidDataException($"Unsupported PNG filter type {filter}.")
                    };
                }
            }

            return result;
        }

        private static byte[] ToRgba(byte[] pixels, ParsedPng png)
        {
            var rgba = new byte[checked(png.Width * png.Height * 4)];
            switch (png.ColorType)
            {
                case 0:
                    ConvertGrayscale(pixels, rgba, png.Transparency);
                    break;
                case 2:
                    ConvertRgb(pixels, rgba, png.Transparency);
                    break;
                case 3:
                    ConvertPalette(pixels, rgba, png.Palette, png.Transparency);
                    break;
                case 4:
                    ConvertGrayscaleAlpha(pixels, rgba);
                    break;
                case 6:
                    if (pixels.Length != rgba.Length)
                    {
                        throw new InvalidDataException("RGBA PNG data does not match expected dimensions.");
                    }

                    Buffer.BlockCopy(pixels, 0, rgba, 0, pixels.Length);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported PNG color type {png.ColorType}.");
            }

            return rgba;
        }

        private static void ConvertPalette(byte[] pixels, byte[] rgba, byte[]? palette, byte[]? transparency)
        {
            if (palette is null || palette.Length == 0)
            {
                throw new InvalidDataException("Indexed PNG is missing a palette.");
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                var paletteIndex = pixels[i];
                var paletteOffset = paletteIndex * 3;
                var rgbaOffset = i * 4;
                if (paletteOffset + 2 >= palette.Length)
                {
                    rgba[rgbaOffset + 3] = 0;
                    continue;
                }

                rgba[rgbaOffset] = palette[paletteOffset];
                rgba[rgbaOffset + 1] = palette[paletteOffset + 1];
                rgba[rgbaOffset + 2] = palette[paletteOffset + 2];
                rgba[rgbaOffset + 3] = transparency is not null && paletteIndex < transparency.Length
                    ? transparency[paletteIndex]
                    : (byte)255;
            }
        }

        private static void ConvertGrayscale(byte[] pixels, byte[] rgba, byte[]? transparency)
        {
            var transparent = transparency is { Length: >= 2 }
                ? BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2))
                : -1;
            for (var i = 0; i < pixels.Length; i++)
            {
                var offset = i * 4;
                var value = pixels[i];
                rgba[offset] = value;
                rgba[offset + 1] = value;
                rgba[offset + 2] = value;
                rgba[offset + 3] = value == transparent ? (byte)0 : (byte)255;
            }
        }

        private static void ConvertRgb(byte[] pixels, byte[] rgba, byte[]? transparency)
        {
            var transparentR = -1;
            var transparentG = -1;
            var transparentB = -1;
            if (transparency is { Length: >= 6 })
            {
                transparentR = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2));
                transparentG = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2, 2));
                transparentB = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4, 2));
            }

            for (var source = 0; source < pixels.Length; source += 3)
            {
                var target = source / 3 * 4;
                rgba[target] = pixels[source];
                rgba[target + 1] = pixels[source + 1];
                rgba[target + 2] = pixels[source + 2];
                rgba[target + 3] = pixels[source] == transparentR
                    && pixels[source + 1] == transparentG
                    && pixels[source + 2] == transparentB
                        ? (byte)0
                        : (byte)255;
            }
        }

        private static void ConvertGrayscaleAlpha(byte[] pixels, byte[] rgba)
        {
            for (var source = 0; source < pixels.Length; source += 2)
            {
                var target = source / 2 * 4;
                rgba[target] = pixels[source];
                rgba[target + 1] = pixels[source];
                rgba[target + 2] = pixels[source];
                rgba[target + 3] = pixels[source + 1];
            }
        }

        private static int BitsPerPixel(int colorType, int bitDepth)
            => colorType switch
            {
                0 => bitDepth,
                2 => bitDepth * 3,
                3 => bitDepth,
                4 => bitDepth * 2,
                6 => bitDepth * 4,
                _ => throw new NotSupportedException($"Unsupported PNG color type {colorType}.")
            };

        private static byte Paeth(byte left, byte up, byte upLeft)
        {
            var p = left + up - upLeft;
            var pa = Math.Abs(p - left);
            var pb = Math.Abs(p - up);
            var pc = Math.Abs(p - upLeft);
            if (pa <= pb && pa <= pc)
            {
                return left;
            }

            return pb <= pc ? up : upLeft;
        }

        private static byte[] Concatenate(IReadOnlyList<byte[]> chunks)
        {
            var length = chunks.Sum(chunk => chunk.Length);
            var result = new byte[length];
            var offset = 0;
            foreach (var chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> lengthBytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(lengthBytes, (uint)data.Length);
            stream.Write(lengthBytes);

            var typeBytes = Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes);
            stream.Write(data);

            using var crcInput = new MemoryStream(typeBytes.Length + data.Length);
            crcInput.Write(typeBytes);
            crcInput.Write(data);
            BinaryPrimitives.WriteUInt32BigEndian(lengthBytes, Crc32(crcInput.ToArray()));
            stream.Write(lengthBytes);
        }

        private static uint Crc32(ReadOnlySpan<byte> data)
        {
            var crc = 0xffffffffu;
            foreach (var value in data)
            {
                crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
            }

            return crc ^ 0xffffffffu;
        }

        private static uint[] CreateCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < table.Length; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }

        private sealed class ParsedPng
        {
            public int Width { get; set; }

            public int Height { get; set; }

            public int BitDepth { get; set; }

            public int ColorType { get; set; }

            public int InterlaceMethod { get; set; }

            public byte[]? Palette { get; set; }

            public byte[]? Transparency { get; set; }

            public List<byte[]> ImageChunks { get; } = [];
        }
    }
}
