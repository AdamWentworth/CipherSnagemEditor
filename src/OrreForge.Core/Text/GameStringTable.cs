using System.Text;
using OrreForge.Core.Binary;

namespace OrreForge.Core.Text;

public sealed class GameStringTable
{
    private const int NumberOfStringsOffset = 0x04;
    private const int EndOfHeader = 0x10;

    private GameStringTable(IReadOnlyList<GameString> strings)
    {
        Strings = strings;
    }

    public IReadOnlyList<GameString> Strings { get; }

    public static GameStringTable Load(string path) => Parse(File.ReadAllBytes(path));

    public static GameStringTable Parse(byte[] bytes)
    {
        var data = new BinaryData(bytes);
        var count = data.ReadUInt16(NumberOfStringsOffset);
        var strings = new List<GameString>(count);

        for (var index = 0; index < count; index++)
        {
            var entryOffset = EndOfHeader + (index * 8);
            if (entryOffset + 8 > data.Length)
            {
                break;
            }

            var stringId = data.ReadUInt32(entryOffset);
            var stringOffset = checked((int)data.ReadUInt32(entryOffset + 4));
            if (stringOffset <= 0 || stringOffset >= data.Length)
            {
                continue;
            }

            strings.Add(new GameString(checked((int)stringId), DecodeGameString(bytes, stringOffset), stringOffset));
        }

        return new GameStringTable(strings);
    }

    private static string DecodeGameString(ReadOnlySpan<byte> bytes, int offset)
    {
        var builder = new StringBuilder();
        for (var current = offset; current + 1 < bytes.Length; current += 2)
        {
            var value = BigEndian.ReadUInt16(bytes, current);
            if (value == 0)
            {
                break;
            }

            if (value < 0x20)
            {
                builder.Append($"<{value:x2}>");
                continue;
            }

            builder.Append((char)value);
        }

        return builder.ToString();
    }
}
