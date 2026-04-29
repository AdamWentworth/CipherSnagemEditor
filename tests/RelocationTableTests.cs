using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Relocation;

namespace CipherSnagemEditor.Tests;

public sealed class RelocationTableTests
{
    [Fact]
    public void ParsesRelocationPointersAndSymbolLengths()
    {
        var bytes = new byte[0x300];
        BigEndian.WriteUInt32(bytes, 0x0c, 2);
        BigEndian.WriteUInt32(bytes, 0x10, 0x40);
        BigEndian.WriteUInt32(bytes, 0x24, 0x80);

        BigEndian.WriteUInt32(bytes, 0x40, 0);
        BigEndian.WriteUInt32(bytes, 0x44, 0);
        BigEndian.WriteUInt32(bytes, 0x48, 0x100);
        BigEndian.WriteUInt32(bytes, 0x4c, 0x80);

        bytes[0x82] = 1;
        bytes[0x83] = 1;
        BigEndian.WriteUInt32(bytes, 0x84, 0x10);
        bytes[0x8a] = 1;
        bytes[0x8b] = 1;
        BigEndian.WriteUInt32(bytes, 0x8c, 0x20);
        bytes[0x92] = 203;

        BigEndian.WriteUInt32(bytes, 0x110, 0x12345678);

        var table = RelocationTable.Parse(bytes);

        Assert.Equal(0x110, table.GetPointer(0));
        Assert.Equal(0x120, table.GetPointer(1));
        Assert.Equal(0x10, table.GetSymbolLength(0));
        Assert.Equal(0x60, table.GetSymbolLength(1));
        Assert.Equal(0x12345678, table.GetValueAtPointer(0));
    }
}
