namespace OrreForge.Core.Binary;

public static class BigEndian
{
    public static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
        EnsureRange(data, offset, 2);
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    public static short ReadInt16(ReadOnlySpan<byte> data, int offset)
        => unchecked((short)ReadUInt16(data, offset));

    public static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        EnsureRange(data, offset, 4);
        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    public static int ReadInt32(ReadOnlySpan<byte> data, int offset)
        => unchecked((int)ReadUInt32(data, offset));

    public static void WriteUInt16(Span<byte> data, int offset, ushort value)
    {
        EnsureRange(data, offset, 2);
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    public static void WriteUInt32(Span<byte> data, int offset, uint value)
    {
        EnsureRange(data, offset, 4);
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} length {length} is outside a {data.Length}-byte buffer.");
        }
    }
}
