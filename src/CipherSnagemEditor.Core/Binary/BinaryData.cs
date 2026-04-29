using System.Text;

namespace CipherSnagemEditor.Core.Binary;

public sealed class BinaryData
{
    private readonly byte[] _bytes;

    public BinaryData(byte[] bytes)
    {
        _bytes = bytes;
    }

    public int Length => _bytes.Length;

    public ReadOnlySpan<byte> Span => _bytes;

    public byte[] ToArray() => (byte[])_bytes.Clone();

    public byte ReadByte(int offset)
    {
        EnsureRange(offset, 1);
        return _bytes[offset];
    }

    public ushort ReadUInt16(int offset) => BigEndian.ReadUInt16(_bytes, offset);

    public short ReadInt16(int offset) => BigEndian.ReadInt16(_bytes, offset);

    public uint ReadUInt32(int offset) => BigEndian.ReadUInt32(_bytes, offset);

    public int ReadInt32(int offset) => BigEndian.ReadInt32(_bytes, offset);

    public void WriteByte(int offset, byte value)
    {
        EnsureRange(offset, 1);
        _bytes[offset] = value;
    }

    public void WriteUInt16(int offset, ushort value) => BigEndian.WriteUInt16(_bytes, offset, value);

    public void WriteUInt32(int offset, uint value) => BigEndian.WriteUInt32(_bytes, offset, value);

    public void WriteBytes(int offset, ReadOnlySpan<byte> values)
    {
        EnsureRange(offset, values.Length);
        values.CopyTo(_bytes.AsSpan(offset, values.Length));
    }

    public BinaryData Slice(int offset, int length)
    {
        EnsureRange(offset, length);
        var bytes = new byte[length];
        Buffer.BlockCopy(_bytes, offset, bytes, 0, length);
        return new BinaryData(bytes);
    }

    public byte[] ReadBytes(int offset, int length)
    {
        EnsureRange(offset, length);
        var bytes = new byte[length];
        Buffer.BlockCopy(_bytes, offset, bytes, 0, length);
        return bytes;
    }

    public string ReadNullTerminatedAscii(int offset)
    {
        EnsureRange(offset, 0);
        var end = offset;
        while (end < _bytes.Length && _bytes[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(_bytes, offset, end - offset);
    }

    public static BinaryData FromFile(string path) => new(File.ReadAllBytes(path));

    private void EnsureRange(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > _bytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} length {length} is outside a {Length}-byte buffer.");
        }
    }
}
