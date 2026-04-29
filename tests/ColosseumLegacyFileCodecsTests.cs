using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumLegacyFileCodecsTests
{
    [Fact]
    public void ExtractsAndImportsColosseumPkxDatPayload()
    {
        var pkx = new byte[0x40 + 0x10 + 3];
        BigEndian.WriteUInt32(pkx, 0, 5);
        new byte[] { 1, 2, 3, 4, 5 }.CopyTo(pkx, 0x40);
        new byte[] { 0xaa, 0xbb, 0xcc }.CopyTo(pkx, 0x50);

        Assert.True(ColosseumLegacyFileCodecs.TryExportColosseumPkxDat(pkx, out var dat));
        Assert.Equal([1, 2, 3, 4, 5], dat);

        Assert.True(ColosseumLegacyFileCodecs.TryImportColosseumPkxDat(pkx, [9, 8, 7, 6, 5, 4, 3], out var imported));
        Assert.Equal((uint)7, BigEndian.ReadUInt32(imported, 0));
        Assert.Equal([9, 8, 7, 6, 5, 4, 3], imported[0x40..0x47]);
        Assert.Equal([0xaa, 0xbb, 0xcc], imported[^3..]);
    }

    [Fact]
    public void ExtractsAndImportsEmbeddedWzxDatModel()
    {
        var wzx = new byte[0x80];
        var model = CreateDatModel(0x40, fill: 0x20);
        BigEndian.WriteUInt32(wzx, 0x08, 0x40);
        model.CopyTo(wzx, 0x10);

        var models = ColosseumLegacyFileCodecs.ExtractWzxDatModels(wzx);

        var extracted = Assert.Single(models);
        Assert.Equal(0, extracted.Index);
        Assert.Equal(0x10, extracted.Offset);
        Assert.Equal(model, extracted.Data);

        var replacement = CreateDatModel(0x40, fill: 0x50);
        Assert.True(ColosseumLegacyFileCodecs.TryImportWzxDatModel(wzx, 0, replacement, out var imported));
        Assert.Equal(replacement, imported[0x10..0x50]);
    }

    [Fact]
    public void SplitsAndCombinesThpHeaderBodyPair()
    {
        var thp = new byte[0x70];
        BigEndian.WriteUInt32(thp, 0x20, 0x30);
        BigEndian.WriteUInt32(thp, 0x28, 0x64);
        BigEndian.WriteUInt32(thp, 0x2c, 0x68);
        thp.AsSpan(0x34, 16).Fill(0xff);
        thp[0x34] = 0;
        thp[0x35] = 1;
        thp[0x60] = 0xde;
        thp[0x61] = 0xad;

        Assert.True(ColosseumLegacyFileCodecs.TrySplitThp(thp, out var header, out var body));

        Assert.Equal(0x60, header.Length);
        Assert.Equal(0x10, body.Length);
        Assert.Equal((uint)4, BigEndian.ReadUInt32(header, 0x28));
        Assert.Equal((uint)8, BigEndian.ReadUInt32(header, 0x2c));
        Assert.Equal(thp, ColosseumLegacyFileCodecs.CombineThp(header, body));
    }

    private static byte[] CreateDatModel(int length, byte fill)
    {
        var model = Enumerable.Repeat(fill, length).ToArray();
        BigEndian.WriteUInt32(model, 0, (uint)length);
        model[12] = 0;
        model[13] = 0;
        model[14] = 0;
        model[15] = 1;
        Array.Clear(model, 16, 16);
        "scene_data\0"u8.CopyTo(model.AsSpan(length - 16));
        return model;
    }
}
