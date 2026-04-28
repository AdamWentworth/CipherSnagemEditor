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

    public string StringWithId(int id)
        => id == 0 ? "-" : Strings.FirstOrDefault(text => text.Id == id)?.Text ?? $"#{id}";

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

            var stringId = data.ReadUInt32(entryOffset) & 0x000fffff;
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
        for (var current = offset; current + 1 < bytes.Length;)
        {
            var value = BigEndian.ReadUInt16(bytes, current);
            if (value == 0)
            {
                break;
            }

            if (value == 0xffff)
            {
                if (current + 2 >= bytes.Length)
                {
                    break;
                }

                var special = bytes[current + 2];
                builder.Append(SpecialCharacterText(special));
                current += 3 + SpecialCharacterExtraBytes(special);
                continue;
            }

            if (value < 0x20)
            {
                builder.Append($"<{value:x2}>");
                current += 2;
                continue;
            }

            builder.Append((char)value);
            current += 2;
        }

        return builder.ToString();
    }

    private static string SpecialCharacterText(byte special)
        => special switch
        {
            0x00 => "[New Line]",
            0x02 => "[Dialogue End]",
            0x03 => "[Clear Window]",
            0x04 => "[Kanji]",
            0x05 => "[Furigana]",
            0x06 => "[Furigana End]",
            0x07 => "[Font]",
            0x08 => "[Spec Colour]",
            0x09 => "[Pause]",
            0x13 => "[Player Battle 19]",
            0x14 => "[Switch Pokemon 20]",
            0x15 => "[Switch Pokemon 21]",
            0x22 => "[Foe Tr Class 34]",
            0x23 => "[Foe Tr Name 35]",
            0x28 => "[Move 40]",
            0x29 => "[Item 41]",
            0x2b => "[Player Field 43]",
            0x2c => "[Rui 44]",
            0x2d => "[Item 45]",
            0x2e => "[Item 46]",
            0x2f => "[Quantity 47]",
            0x32 => "[String 50]",
            0x38 => "[Predef Colour]",
            0x4d => "[MsgID 77]",
            0x50 => "[Pokemon Cry 80]",
            0x59 => "[Speaker]",
            0x6a => "[Set Speaker 106]",
            0x6d => "[Wait Input 109]",
            _ => $"[{special}]"
        };

    private static int SpecialCharacterExtraBytes(byte special)
        => special switch
        {
            0x07 or 0x09 or 0x38 or 0x52 or 0x53 or 0x5b or 0x5c => 1,
            0x08 => 4,
            _ => 0
        };
}
