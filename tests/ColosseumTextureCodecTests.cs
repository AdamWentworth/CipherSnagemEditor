using System.Buffers.Binary;
using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumTextureCodecTests
{
    [Fact]
    public void DecodesAndImportsRgb565TexturePng()
    {
        var texture = CreateTexture(4, 4, format: 0x44, bitsPerPixel: 16, textureLength: 32);
        var rawColors = new ushort[]
        {
            0xf800, 0x07e0, 0x001f, 0xffff,
            0x0000, 0xffe0, 0xf81f, 0x07ff,
            0x8410, 0xc618, 0x4208, 0xa514,
            0x8000, 0x0400, 0x0010, 0xffff
        };
        for (var i = 0; i < rawColors.Length; i++)
        {
            BigEndian.WriteUInt16(texture, 0x80 + i * 2, rawColors[i]);
        }

        Assert.True(ColosseumTextureCodec.TryDecodePng(texture, out var png));
        Assert.Equal([0x89, 0x50, 0x4e, 0x47], png[..4]);
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));

        Assert.True(ColosseumTextureCodec.TryImportPng(texture, png, out var imported));
        Assert.Equal(texture, imported);
    }

    [Fact]
    public void DecodesAndImportsIndexedC8TexturePng()
    {
        var texture = CreateTexture(8, 4, format: 0x01, bitsPerPixel: 8, textureLength: 32, paletteLength: 512, paletteFormat: 3);
        for (var i = 0; i < 32; i++)
        {
            texture[0x80 + i] = (byte)(i % 2);
        }

        BigEndian.WriteUInt16(texture, 0x80 + 32, 0xfc00);
        BigEndian.WriteUInt16(texture, 0x80 + 34, 0x83e0);

        Assert.True(ColosseumTextureCodec.TryDecodePng(texture, out var png));
        Assert.True(ColosseumTextureCodec.TryImportPng(texture, png, out var imported));
        Assert.Equal(texture, imported);
    }

    [Fact]
    public void DecodesAndImportsTransparentCmprTexturePng()
    {
        var texture = CreateTexture(8, 8, format: 0xb0, bitsPerPixel: 4, textureLength: 32);
        for (var i = 0; i < 4; i++)
        {
            texture[0x80 + i * 8 + 4] = 0xff;
            texture[0x80 + i * 8 + 5] = 0xff;
            texture[0x80 + i * 8 + 6] = 0xff;
            texture[0x80 + i * 8 + 7] = 0xff;
        }

        Assert.True(ColosseumTextureCodec.TryDecodePng(texture, out var png));
        Assert.True(ColosseumTextureCodec.TryImportPng(texture, png, out var imported));
        Assert.Equal(texture, imported);
    }

    [Fact]
    public void ExtractsAndImportsDatModelTexture()
    {
        var dat = CreateDatWithRgb565Texture();

        var texture = Assert.Single(ColosseumDatTextureCodec.ExtractTextures(dat));
        Assert.Equal(0, texture.Index);
        Assert.True(ColosseumTextureCodec.TryDecodePng(texture.TextureBytes, out _));

        var replacement = texture.TextureBytes.ToArray();
        BigEndian.WriteUInt16(replacement, 0x80, 0x07e0);
        Assert.True(ColosseumDatTextureCodec.TryImportTextures(
            dat,
            new Dictionary<int, byte[]> { [texture.Index] = replacement },
            out var importedDat,
            out var importedCount));

        Assert.Equal(1, importedCount);
        Assert.Equal((ushort)0x07e0, BigEndian.ReadUInt16(importedDat, 0x20 + 0x1a0));
    }

    [Fact]
    public void ExtractsAndImportsGswTextureSection()
    {
        var gsw = CreateGswWithC8Texture();

        var texture = Assert.Single(ColosseumGswTextureCodec.ExtractTextures(gsw));
        Assert.Equal(1, texture.Id);
        Assert.True(ColosseumTextureCodec.TryDecodePng(texture.TextureBytes, out _));

        var replacement = texture.TextureBytes.ToArray();
        replacement[0x80] = 1;
        Assert.True(ColosseumGswTextureCodec.TryImportTextures(
            gsw,
            new Dictionary<int, byte[]> { [texture.Id] = replacement },
            out var importedGsw,
            out var importedCount));

        Assert.Equal(1, importedCount);
        Assert.Equal(1, importedGsw[0x60 + 0x80]);
    }

    private static byte[] CreateTexture(
        int width,
        int height,
        byte format,
        byte bitsPerPixel,
        int textureLength,
        int paletteLength = 0,
        byte paletteFormat = 0)
    {
        var texture = new byte[0x80 + textureLength + paletteLength];
        BigEndian.WriteUInt16(texture, 0x00, (ushort)width);
        BigEndian.WriteUInt16(texture, 0x02, (ushort)height);
        texture[0x04] = bitsPerPixel;
        texture[0x05] = paletteLength > 0 ? (byte)1 : (byte)0;
        texture[0x0b] = format;
        texture[0x0f] = paletteFormat;
        BigEndian.WriteUInt32(texture, 0x28, 0x80);
        BigEndian.WriteUInt32(texture, 0x48, checked((uint)(0x80 + textureLength)));
        return texture;
    }

    private static byte[] CreateDatWithRgb565Texture()
    {
        const int modelLength = 0x1c0;
        var dat = new byte[0x20 + modelLength];
        BigEndian.WriteUInt32(dat, 0x00, (uint)dat.Length);
        BigEndian.WriteUInt32(dat, 0x04, 0x10);
        BigEndian.WriteUInt32(dat, 0x08, 4);
        BigEndian.WriteUInt32(dat, 0x0c, 1);
        BigEndian.WriteUInt32(dat, 0x10, 0);

        WriteModelWord(dat, 0x20, 0x30);
        WriteModelWord(dat, 0x30, 0x50);
        WriteModelWord(dat, 0x50, 0x60);
        WriteModelWord(dat, 0x60, 0x80);
        WriteModelWord(dat, 0x80 + 0x10, 0xc0);
        WriteModelWord(dat, 0xc0 + 0x08, 0xe0);
        WriteModelWord(dat, 0xe0 + 0x08, 0x100);
        WriteModelWord(dat, 0x100 + 0x4c, 0x170);
        WriteModelWord(dat, 0x170, 0x1a0);
        BigEndian.WriteUInt16(dat, 0x20 + 0x170 + 0x04, 4);
        BigEndian.WriteUInt16(dat, 0x20 + 0x170 + 0x06, 4);
        WriteModelWord(dat, 0x170 + 0x08, 4);

        for (var i = 0; i < 16; i++)
        {
            BigEndian.WriteUInt16(dat, 0x20 + 0x1a0 + i * 2, (ushort)(i % 2 == 0 ? 0xf800 : 0x001f));
        }

        return dat;
    }

    private static byte[] CreateGswWithC8Texture()
    {
        var texture = CreateTexture(8, 4, format: 0x01, bitsPerPixel: 8, textureLength: 32, paletteLength: 512, paletteFormat: 3);
        for (var i = 0; i < 32; i++)
        {
            texture[0x80 + i] = (byte)(i % 2);
        }

        BigEndian.WriteUInt16(texture, 0x80 + 32, 0xfc00);
        BigEndian.WriteUInt16(texture, 0x80 + 34, 0x83e0);

        var gsw = new byte[0x60 + texture.Length];
        BigEndian.WriteUInt16(gsw, 0x18, 1);
        gsw[0x40] = 0x03;
        gsw[0x43] = 0x01;
        texture.CopyTo(gsw.AsSpan(0x60));
        return gsw;
    }

    private static void WriteModelWord(byte[] dat, int modelOffset, uint value)
        => BigEndian.WriteUInt32(dat, 0x20 + modelOffset, value);
}
