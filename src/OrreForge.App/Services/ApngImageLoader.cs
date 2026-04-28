using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OrreForge.App.Services;

public static class ApngImageLoader
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly TimeSpan DefaultFrameDuration = TimeSpan.FromMilliseconds(100);

    public static IReadOnlyList<PokemonBodyFrame> Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var png = Parse(bytes);
        if (png.Frames.Count == 0)
        {
            return [new PokemonBodyFrame(DecodeStaticImage(png), DefaultFrameDuration)];
        }

        return DecodeAnimation(png);
    }

    private static ParsedPng Parse(byte[] bytes)
    {
        if (bytes.Length < PngSignature.Length || !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new InvalidDataException("Not a PNG file.");
        }

        var png = new ParsedPng();
        FrameData? currentFrame = null;

        var offset = PngSignature.Length;
        while (offset + 12 <= bytes.Length)
        {
            var length = checked((int)ReadUInt32(bytes, offset));
            var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataOffset = offset + 8;
            var data = bytes.AsSpan(dataOffset, length);

            switch (type)
            {
                case "CgBI":
                    png.IsCgbi = true;
                    break;
                case "IHDR":
                    png.Width = checked((int)ReadUInt32(data, 0));
                    png.Height = checked((int)ReadUInt32(data, 4));
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
                case "fcTL":
                    if (currentFrame is not null)
                    {
                        png.Frames.Add(currentFrame);
                    }

                    currentFrame = new FrameData(
                        checked((int)ReadUInt32(data, 4)),
                        checked((int)ReadUInt32(data, 8)),
                        checked((int)ReadUInt32(data, 12)),
                        checked((int)ReadUInt32(data, 16)),
                        ReadUInt16(data, 20),
                        ReadUInt16(data, 22),
                        data[24],
                        data[25]);
                    break;
                case "IDAT":
                    if (currentFrame is null)
                    {
                        png.StaticImageChunks.Add(data.ToArray());
                    }
                    else
                    {
                        currentFrame.CompressedChunks.Add(data.ToArray());
                    }
                    break;
                case "fdAT":
                    currentFrame?.CompressedChunks.Add(data[4..].ToArray());
                    break;
                case "IEND":
                    offset = bytes.Length;
                    break;
            }

            offset += length + 12;
        }

        if (currentFrame is not null)
        {
            png.Frames.Add(currentFrame);
        }

        return png;
    }

    private static IReadOnlyList<PokemonBodyFrame> DecodeAnimation(ParsedPng png)
    {
        if (png.Width <= 0 || png.Height <= 0)
        {
            throw new InvalidDataException("PNG missing IHDR dimensions.");
        }

        if (png.BitDepth != 8)
        {
            throw new NotSupportedException("Only 8-bit APNG body images are currently supported.");
        }

        if (png.InterlaceMethod != 0)
        {
            throw new NotSupportedException("Interlaced APNG body images are currently unsupported.");
        }

        var frames = new List<PokemonBodyFrame>(png.Frames.Count);
        var canvas = new byte[png.Width * png.Height * 4];

        foreach (var frame in png.Frames.Where(frame => frame.CompressedChunks.Count > 0))
        {
            var restoreCanvas = frame.DisposeOp == 2 ? canvas.ToArray() : null;
            var framePixels = DecodeFramePixels(png, frame);

            BlendFrame(canvas, png.Width, png.Height, frame, framePixels);
            frames.Add(new PokemonBodyFrame(CreateBitmap(canvas, png.Width, png.Height), FrameDuration(frame)));

            switch (frame.DisposeOp)
            {
                case 1:
                    ClearFrameRegion(canvas, png.Width, png.Height, frame);
                    break;
                case 2 when restoreCanvas is not null:
                    canvas = restoreCanvas;
                    break;
            }
        }

        return frames.Count == 0
            ? throw new InvalidDataException("APNG has no decodable frames.")
            : frames;
    }

    private static Bitmap DecodeStaticImage(ParsedPng png)
    {
        ValidateDecodableImage(png, "PNG");

        if (png.StaticImageChunks.Count == 0)
        {
            throw new InvalidDataException("PNG has no image data.");
        }

        var frame = new FrameData(
            png.Width,
            png.Height,
            0,
            0,
            10,
            100,
            0,
            0);

        foreach (var chunk in png.StaticImageChunks)
        {
            frame.CompressedChunks.Add(chunk);
        }

        var pixels = DecodeFramePixels(png, frame);
        var canvas = new byte[png.Width * png.Height * 4];
        BlendFrame(canvas, png.Width, png.Height, frame, pixels);
        return CreateBitmap(canvas, png.Width, png.Height);
    }

    private static byte[] DecodeFramePixels(ParsedPng png, FrameData frame)
    {
        var compressed = Concatenate(frame.CompressedChunks);
        using var source = new MemoryStream(compressed);
        using Stream inflater = png.IsCgbi
            ? new DeflateStream(source, CompressionMode.Decompress)
            : new ZLibStream(source, CompressionMode.Decompress);
        using var decoded = new MemoryStream();
        inflater.CopyTo(decoded);

        var scanlines = Unfilter(decoded.ToArray(), frame.Width, frame.Height, png.ColorType, png.BitDepth);
        return ToRgba(scanlines, frame.Width, frame.Height, png);
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

    private static void ValidateDecodableImage(ParsedPng png, string formatName)
    {
        if (png.Width <= 0 || png.Height <= 0)
        {
            throw new InvalidDataException($"{formatName} missing IHDR dimensions.");
        }

        if (png.BitDepth != 8)
        {
            throw new NotSupportedException($"Only 8-bit {formatName} images are currently supported.");
        }

        if (png.InterlaceMethod != 0)
        {
            throw new NotSupportedException($"Interlaced {formatName} images are currently unsupported.");
        }
    }

    private static byte[] ToRgba(byte[] pixels, int width, int height, ParsedPng png)
    {
        var rgba = new byte[checked(width * height * 4)];

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
                ConvertRgba(pixels, rgba, png.IsCgbi);
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
            throw new InvalidDataException("Indexed PNG frame is missing a palette.");
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
        var transparent = transparency is { Length: >= 2 } ? BinaryPrimitives.ReadUInt16BigEndian(transparency) : -1;
        for (var i = 0; i < pixels.Length; i++)
        {
            var rgbaOffset = i * 4;
            var value = pixels[i];
            rgba[rgbaOffset] = value;
            rgba[rgbaOffset + 1] = value;
            rgba[rgbaOffset + 2] = value;
            rgba[rgbaOffset + 3] = value == transparent ? (byte)0 : (byte)255;
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
            var r = pixels[source];
            var g = pixels[source + 1];
            var b = pixels[source + 2];
            rgba[target] = r;
            rgba[target + 1] = g;
            rgba[target + 2] = b;
            rgba[target + 3] = r == transparentR && g == transparentG && b == transparentB ? (byte)0 : (byte)255;
        }
    }

    private static void ConvertGrayscaleAlpha(byte[] pixels, byte[] rgba)
    {
        for (var source = 0; source < pixels.Length; source += 2)
        {
            var target = source / 2 * 4;
            var value = pixels[source];
            rgba[target] = value;
            rgba[target + 1] = value;
            rgba[target + 2] = value;
            rgba[target + 3] = pixels[source + 1];
        }
    }

    private static void ConvertRgba(byte[] pixels, byte[] rgba, bool isCgbi)
    {
        if (pixels.Length != rgba.Length)
        {
            throw new InvalidDataException("RGBA PNG frame data does not match expected dimensions.");
        }

        if (!isCgbi)
        {
            Buffer.BlockCopy(pixels, 0, rgba, 0, pixels.Length);
            return;
        }

        for (var source = 0; source < pixels.Length; source += 4)
        {
            rgba[source] = pixels[source + 2];
            rgba[source + 1] = pixels[source + 1];
            rgba[source + 2] = pixels[source];
            rgba[source + 3] = pixels[source + 3];
        }
    }

    private static void BlendFrame(byte[] canvas, int canvasWidth, int canvasHeight, FrameData frame, byte[] framePixels)
    {
        for (var y = 0; y < frame.Height; y++)
        {
            var targetY = frame.YOffset + y;
            if (targetY < 0 || targetY >= canvasHeight)
            {
                continue;
            }

            for (var x = 0; x < frame.Width; x++)
            {
                var targetX = frame.XOffset + x;
                if (targetX < 0 || targetX >= canvasWidth)
                {
                    continue;
                }

                var source = (y * frame.Width + x) * 4;
                var target = (targetY * canvasWidth + targetX) * 4;

                if (frame.BlendOp == 0)
                {
                    Buffer.BlockCopy(framePixels, source, canvas, target, 4);
                }
                else
                {
                    AlphaComposite(framePixels, source, canvas, target);
                }
            }
        }
    }

    private static void AlphaComposite(byte[] sourcePixels, int source, byte[] targetPixels, int target)
    {
        var sourceAlpha = sourcePixels[source + 3] / 255.0;
        var targetAlpha = targetPixels[target + 3] / 255.0;
        var outputAlpha = sourceAlpha + (targetAlpha * (1.0 - sourceAlpha));

        if (outputAlpha <= 0)
        {
            targetPixels[target] = 0;
            targetPixels[target + 1] = 0;
            targetPixels[target + 2] = 0;
            targetPixels[target + 3] = 0;
            return;
        }

        for (var channel = 0; channel < 3; channel++)
        {
            var sourceColor = sourcePixels[source + channel] / 255.0;
            var targetColor = targetPixels[target + channel] / 255.0;
            var outputColor = ((sourceColor * sourceAlpha) + (targetColor * targetAlpha * (1.0 - sourceAlpha))) / outputAlpha;
            targetPixels[target + channel] = (byte)Math.Round(outputColor * 255.0);
        }

        targetPixels[target + 3] = (byte)Math.Round(outputAlpha * 255.0);
    }

    private static void ClearFrameRegion(byte[] canvas, int canvasWidth, int canvasHeight, FrameData frame)
    {
        for (var y = 0; y < frame.Height; y++)
        {
            var targetY = frame.YOffset + y;
            if (targetY < 0 || targetY >= canvasHeight)
            {
                continue;
            }

            for (var x = 0; x < frame.Width; x++)
            {
                var targetX = frame.XOffset + x;
                if (targetX < 0 || targetX >= canvasWidth)
                {
                    continue;
                }

                Array.Clear(canvas, (targetY * canvasWidth + targetX) * 4, 4);
            }
        }
    }

    private static Bitmap CreateBitmap(byte[] rgba, int width, int height)
    {
        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];
            bgra[i + 1] = rgba[i + 1];
            bgra[i + 2] = rgba[i];
            bgra[i + 3] = rgba[i + 3];
        }

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        using var framebuffer = bitmap.Lock();
        var sourceOffset = 0;
        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(bgra, sourceOffset, IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes), width * 4);
            sourceOffset += width * 4;
        }

        return bitmap;
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

    private static TimeSpan FrameDuration(FrameData frame)
    {
        var denominator = frame.DelayDenominator == 0 ? 100 : frame.DelayDenominator;
        var milliseconds = 1000.0 * frame.DelayNumerator / denominator;
        return milliseconds <= 0 ? DefaultFrameDuration : TimeSpan.FromMilliseconds(milliseconds);
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

    private static uint ReadUInt32(byte[] bytes, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(bytes[offset..(offset + 4)]);

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, int offset)
        => BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..(offset + 2)]);

    private sealed class ParsedPng
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int BitDepth { get; set; }

        public int ColorType { get; set; }

        public int InterlaceMethod { get; set; }

        public bool IsCgbi { get; set; }

        public byte[]? Palette { get; set; }

        public byte[]? Transparency { get; set; }

        public List<byte[]> StaticImageChunks { get; } = [];

        public List<FrameData> Frames { get; } = [];
    }

    private sealed class FrameData(
        int width,
        int height,
        int xOffset,
        int yOffset,
        int delayNumerator,
        int delayDenominator,
        byte disposeOp,
        byte blendOp)
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public int XOffset { get; } = xOffset;

        public int YOffset { get; } = yOffset;

        public int DelayNumerator { get; } = delayNumerator;

        public int DelayDenominator { get; } = delayDenominator;

        public byte DisposeOp { get; } = disposeOp;

        public byte BlendOp { get; } = blendOp;

        public List<byte[]> CompressedChunks { get; } = [];
    }
}
