using System.Text;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.Text;

public sealed class GameStringTable
{
    private const int NumberOfStringsOffset = 0x04;
    private const int EndOfHeader = 0x10;
    private static readonly IReadOnlyDictionary<string, byte> SpecialTokens = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
    {
        ["New Line"] = 0x00,
        ["Dialogue End"] = 0x02,
        ["Clear Window"] = 0x03,
        ["Kanji"] = 0x04,
        ["Furigana"] = 0x05,
        ["Furigana End"] = 0x06,
        ["Font"] = 0x07,
        ["Spec Colour"] = 0x08,
        ["Pause"] = 0x09,
        ["Player Battle 19"] = 0x13,
        ["Switch Pokemon 20"] = 0x14,
        ["Switch Pokemon 21"] = 0x15,
        ["Foe Tr Class 34"] = 0x22,
        ["Foe Tr Name 35"] = 0x23,
        ["Move 40"] = 0x28,
        ["Item 41"] = 0x29,
        ["Player Field 43"] = 0x2b,
        ["Rui 44"] = 0x2c,
        ["Item 45"] = 0x2d,
        ["Item 46"] = 0x2e,
        ["Quantity 47"] = 0x2f,
        ["String 50"] = 0x32,
        ["Predef Colour"] = 0x38,
        ["MsgID 77"] = 0x4d,
        ["Pokemon Cry 80"] = 0x50,
        ["Speaker"] = 0x59,
        ["Set Speaker 106"] = 0x6a,
        ["Wait Input 109"] = 0x6d
    };

    private GameStringTable(IReadOnlyList<GameString> strings)
    {
        Strings = strings;
    }

    public IReadOnlyList<GameString> Strings { get; }

    public static GameStringTable Load(string path) => Parse(File.ReadAllBytes(path));

    public string StringWithId(int id)
        => id == 0 ? "-" : Strings.FirstOrDefault(text => text.Id == id)?.Text ?? $"#{id}";

    public GameStringTable WithString(int id, string text)
    {
        if (id <= 0 || id > 0x000fffff)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Message string IDs must be between 1 and 0xFFFFF.");
        }

        var replacementText = string.IsNullOrEmpty(text) ? "-" : text;
        var strings = Strings
            .Where(message => message.Id != id)
            .Append(new GameString(id, replacementText, 0))
            .OrderBy(message => message.Id)
            .ToArray();
        return new GameStringTable(strings);
    }

    public byte[] ToArray()
    {
        if (Strings.Count > ushort.MaxValue)
        {
            throw new InvalidDataException("String table has too many entries.");
        }

        var encodedStrings = Strings
            .OrderBy(message => message.Id)
            .Select(message => (message.Id, Bytes: EncodeGameString(message.Text)))
            .ToArray();
        var textOffset = EndOfHeader + (encodedStrings.Length * 8);
        var length = textOffset + encodedStrings.Sum(message => message.Bytes.Length);
        var bytes = new byte[length];
        BigEndian.WriteUInt16(bytes, NumberOfStringsOffset, checked((ushort)encodedStrings.Length));

        var currentEntry = EndOfHeader;
        var currentText = textOffset;
        foreach (var message in encodedStrings)
        {
            BigEndian.WriteUInt32(bytes, currentEntry, checked((uint)message.Id & 0x000fffff));
            BigEndian.WriteUInt32(bytes, currentEntry + 4, checked((uint)currentText));
            message.Bytes.CopyTo(bytes, currentText);
            currentEntry += 8;
            currentText += message.Bytes.Length;
        }

        return bytes;
    }

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

    private static byte[] EncodeGameString(string text)
    {
        using var stream = new MemoryStream();
        for (var index = 0; index < text.Length;)
        {
            if (text[index] == '\r')
            {
                index++;
                continue;
            }

            if (text[index] == '\n')
            {
                WriteSpecial(stream, 0x00, []);
                index++;
                continue;
            }

            if (text[index] == '[')
            {
                var end = text.IndexOf(']', index + 1);
                if (end > index)
                {
                    var token = text[(index + 1)..end];
                    if (TryParseSpecialToken(token, out var special, out var extra))
                    {
                        WriteSpecial(stream, special, extra);
                        index = end + 1;
                        continue;
                    }
                }
            }

            WriteUInt16(stream, text[index]);
            index++;
        }

        WriteUInt16(stream, 0);
        return stream.ToArray();
    }

    private static bool TryParseSpecialToken(string token, out byte special, out byte[] extra)
    {
        extra = [];
        if (SpecialTokens.TryGetValue(token.Trim(), out special))
        {
            extra = new byte[SpecialCharacterExtraBytes(special)];
            return true;
        }

        if (byte.TryParse(token.Trim(), out special))
        {
            extra = new byte[SpecialCharacterExtraBytes(special)];
            return true;
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && byte.TryParse(token[2..], System.Globalization.NumberStyles.HexNumber, null, out special))
        {
            extra = new byte[SpecialCharacterExtraBytes(special)];
            return true;
        }

        special = 0;
        return false;
    }

    private static void WriteSpecial(Stream stream, byte special, ReadOnlySpan<byte> extra)
    {
        WriteUInt16(stream, 0xffff);
        stream.WriteByte(special);
        for (var index = 0; index < SpecialCharacterExtraBytes(special); index++)
        {
            stream.WriteByte(index < extra.Length ? extra[index] : (byte)0);
        }
    }

    private static void WriteUInt16(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
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
