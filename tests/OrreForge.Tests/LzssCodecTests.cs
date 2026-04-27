using OrreForge.Core.Binary;
using OrreForge.Core.Compression;

namespace OrreForge.Tests;

public sealed class LzssCodecTests
{
    [Fact]
    public void DecodesLiteralPayload()
    {
        var compressed = new byte[0x14];
        BigEndian.WriteUInt32(compressed, 0, LzssCodec.Magic);
        BigEndian.WriteUInt32(compressed, 4, 3);
        BigEndian.WriteUInt32(compressed, 8, 0x14);
        compressed[0x10] = 0x07;
        compressed[0x11] = (byte)'A';
        compressed[0x12] = (byte)'B';
        compressed[0x13] = (byte)'C';

        Assert.Equal([(byte)'A', (byte)'B', (byte)'C'], LzssCodec.DecodeFile(compressed));
    }
}
