using CipherSnagemEditor.Colosseum.Data;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumDatVertexColorModelTests
{
    [Fact]
    public void ParsesAndWritesRgba8VertexColours()
    {
        var bytes = BuildTinyDatWithRgba8VertexColours();
        var model = ColosseumDatVertexColorModel.Parse(bytes);

        Assert.Equal(2, model.VertexColors.Count);
        Assert.All(model.VertexColors, color => Assert.Equal(ColosseumDatVertexColorFormat.Rgba8, color.Format));
        Assert.Equal(0x100, model.VertexColors[0].ModelOffset);
        Assert.Equal(10, model.VertexColors[0].Red);
        Assert.Equal(20, model.VertexColors[0].Green);
        Assert.Equal(30, model.VertexColors[0].Blue);

        var editedCount = model.ApplyFilter(ColosseumVertexColorFilter.MinorRedShift);
        var edited = model.ToArray();

        Assert.Equal(2, editedCount);
        Assert.Equal(30, edited[0x120]);
        Assert.Equal(10, edited[0x121]);
        Assert.Equal(20, edited[0x122]);
        Assert.Equal(0xff, edited[0x123]);
        Assert.Equal(0xff, edited[0x124]);
        Assert.Equal(0, edited[0x125]);
        Assert.Equal(0, edited[0x126]);
        Assert.Equal(0xff, edited[0x127]);
        Assert.Equal((byte)'s', edited[0x1a8]);
    }

    private static byte[] BuildTinyDatWithRgba8VertexColours()
    {
        var bytes = new byte[0x1c0];
        WriteFileUInt32(bytes, 0x00, bytes.Length);
        WriteFileUInt32(bytes, 0x04, 0x180);
        WriteFileUInt32(bytes, 0x08, 0);
        WriteFileUInt32(bytes, 0x0c, 1);
        WriteFileUInt32(bytes, 0x10, 0);

        WriteModelUInt32(bytes, 0x20, 0x30);
        WriteModelUInt32(bytes, 0x30, 0x40);
        WriteModelUInt32(bytes, 0x34, 0);
        WriteModelUInt32(bytes, 0x40, 0x50);
        WriteModelUInt32(bytes, 0x60, 0x90);

        WriteModelUInt32(bytes, 0x9c, 0xa0);
        WriteModelUInt32(bytes, 0xa8, 0xc0);

        WriteModelUInt32(bytes, 0xc0, 11);
        WriteModelUInt32(bytes, 0xcc, 5);
        WriteModelUInt16(bytes, 0xd2, 4);
        WriteModelUInt32(bytes, 0xd4, 0x100);

        bytes[0x120] = 10;
        bytes[0x121] = 20;
        bytes[0x122] = 30;
        bytes[0x123] = 0xff;
        bytes[0x124] = 250;
        bytes[0x125] = 5;
        bytes[0x126] = 6;
        bytes[0x127] = 0xff;

        WriteModelUInt32(bytes, 0x180, 0x20);
        WriteModelUInt32(bytes, 0x184, 0);
        "scene_data"u8.CopyTo(bytes.AsSpan(0x1a8));
        return bytes;
    }

    private static void WriteFileUInt32(byte[] bytes, int offset, int value)
        => BigEndian.WriteUInt32(bytes, offset, unchecked((uint)value));

    private static void WriteModelUInt32(byte[] bytes, int modelOffset, int value)
        => BigEndian.WriteUInt32(bytes, 0x20 + modelOffset, unchecked((uint)value));

    private static void WriteModelUInt16(byte[] bytes, int modelOffset, int value)
        => BigEndian.WriteUInt16(bytes, 0x20 + modelOffset, unchecked((ushort)value));
}
