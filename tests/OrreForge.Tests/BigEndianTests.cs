using OrreForge.Core.Binary;

namespace OrreForge.Tests;

public sealed class BigEndianTests
{
    [Fact]
    public void ReadsAndWritesBigEndianValues()
    {
        var bytes = new byte[8];
        BigEndian.WriteUInt16(bytes, 0, 0x1234);
        BigEndian.WriteUInt32(bytes, 2, 0x47433645);

        Assert.Equal(0x1234, BigEndian.ReadUInt16(bytes, 0));
        Assert.Equal(0x47433645u, BigEndian.ReadUInt32(bytes, 2));
    }
}
