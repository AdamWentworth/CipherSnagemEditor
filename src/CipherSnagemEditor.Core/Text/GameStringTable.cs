using System.Text;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.Text;

public sealed class GameStringTable
{
    private const int NumberOfStringsOffset = 0x04;
    private const int EndOfHeader = 0x10;
    private static readonly IReadOnlyDictionary<byte, string> SpecialNames = new Dictionary<byte, string>
    {
        [0x00] = "New Line",
        [0x02] = "Dialogue End",
        [0x03] = "Clear Window",
        [0x04] = "Kanji",
        [0x05] = "Furigana",
        [0x06] = "Furigana End",
        [0x07] = "Font",
        [0x08] = "Spec Colour",
        [0x09] = "Pause",
        [0x0f] = "Pokemon 15",
        [0x10] = "Pokemon 16",
        [0x11] = "Pokemon 17",
        [0x12] = "Pokemon 18",
        [0x13] = "Player Battle 19",
        [0x14] = "Switch Pokemon 20",
        [0x15] = "Switch Pokemon 21",
        [0x16] = "Pokemon 22",
        [0x17] = "Pokemon 23",
        [0x18] = "Pokemon 24",
        [0x19] = "Pokemon 25",
        [0x1a] = "Ability 26",
        [0x1b] = "Ability 27",
        [0x1c] = "Ability 28",
        [0x1d] = "Ability 29",
        [0x1e] = "Pokemon 30",
        [0x20] = "Pokemon 32",
        [0x21] = "Pokemon 33",
        [0x22] = "Foe Tr Class 34",
        [0x23] = "Foe Tr Name 35",
        [0x28] = "Move 40",
        [0x29] = "Item 41",
        [0x2b] = "Player Field 43",
        [0x2c] = "Rui 44",
        [0x2d] = "Item 45",
        [0x2e] = "Item 46",
        [0x2f] = "Quantity 47",
        [0x32] = "String 50",
        [0x38] = "Predef Colour",
        [0x4d] = "MsgID 77",
        [0x4e] = "Pokemon 78",
        [0x50] = "Pokemon Cry 80",
        [0x59] = "Speaker",
        [0x6a] = "Set Speaker 106",
        [0x6d] = "Wait Input 109"
    };
    private static readonly IReadOnlyDictionary<string, byte> SpecialTokens = BuildSpecialTokens();

    private readonly int? _parsedLength;

    private GameStringTable(IReadOnlyList<GameString> strings, int? parsedLength = null)
    {
        Strings = strings;
        _parsedLength = parsedLength;
    }

    public IReadOnlyList<GameString> Strings { get; }

    public static GameStringTable Load(string path) => Parse(File.ReadAllBytes(path));

    public string StringWithId(int id)
        => id == 0 ? "-" : Strings.FirstOrDefault(text => text.Id == id)?.Text ?? $"#{id}";

    public static GameStringTable FromStrings(IEnumerable<GameString> strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        return new GameStringTable(strings
            .Where(message => message.Id > 0)
            .GroupBy(message => message.Id)
            .Select(group => group.Last())
            .OrderBy(message => message.Id)
            .ToArray());
    }

    public GameStringTable WithString(int id, string text)
        => WithStrings([new GameString(id, text, 0)]);

    public GameStringTable WithStrings(IEnumerable<GameString> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);

        var normalized = replacements
            .Select(message =>
            {
                if (message.Id <= 0 || message.Id > 0x000fffff)
                {
                    throw new ArgumentOutOfRangeException(nameof(replacements), "Message string IDs must be between 1 and 0xFFFFF.");
                }

                var replacementText = string.IsNullOrEmpty(message.Text) ? "-" : message.Text;
                return new GameString(message.Id, replacementText, message.Offset);
            })
            .ToArray();
        if (normalized.Length == 0)
        {
            return this;
        }

        var replacementIds = normalized.Select(message => message.Id).ToHashSet();
        var strings = Strings
            .Where(message => !replacementIds.Contains(message.Id))
            .Concat(normalized)
            .GroupBy(message => message.Id)
            .Select(group => group.Last())
            .OrderBy(message => message.Id)
            .ToArray();
        return new GameStringTable(strings, _parsedLength);
    }

    public byte[] ToArray(bool allowGrowth = true)
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
        var encodedLength = textOffset + encodedStrings.Sum(message => message.Bytes.Length);
        if (!allowGrowth && _parsedLength is int parsedLength && encodedLength > parsedLength)
        {
            throw new InvalidDataException(
                $"String table requires {encodedLength} bytes but the original table has {parsedLength} bytes.");
        }

        var length = _parsedLength is int originalLength && encodedLength <= originalLength
            ? originalLength
            : encodedLength;
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

        return new GameStringTable(strings, bytes.Length);
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
                var extraLength = SpecialCharacterExtraBytes(special);
                var extra = current + 3 + extraLength <= bytes.Length
                    ? bytes.Slice(current + 3, extraLength)
                    : ReadOnlySpan<byte>.Empty;
                builder.Append(SpecialCharacterText(special, extra));
                current += 3 + extraLength;
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
                    if (TryParseSpecialToken(token, out var special))
                    {
                        var extra = ParseSpecialExtraBytes(text, end + 1, special, out var extraEnd);
                        WriteSpecial(stream, special, extra);
                        index = extraEnd;
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

    private static bool TryParseSpecialToken(string token, out byte special)
    {
        if (SpecialTokens.TryGetValue(token.Trim(), out special))
        {
            return true;
        }

        if (byte.TryParse(token.Trim(), out special))
        {
            return true;
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && byte.TryParse(token[2..], System.Globalization.NumberStyles.HexNumber, null, out special))
        {
            return true;
        }

        special = 0;
        return false;
    }

    private static byte[] ParseSpecialExtraBytes(string text, int start, byte special, out int end)
    {
        var expectedLength = SpecialCharacterExtraBytes(special);
        end = start;
        if (expectedLength == 0)
        {
            return [];
        }

        var extra = new byte[expectedLength];
        if (start >= text.Length || text[start] != '{')
        {
            return extra;
        }

        var close = text.IndexOf('}', start + 1);
        if (close < 0)
        {
            return extra;
        }

        var hex = text[(start + 1)..close].Trim();
        for (var index = 0; index < expectedLength && index * 2 + 1 < hex.Length; index++)
        {
            if (byte.TryParse(
                hex.Substring(index * 2, 2),
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var value))
            {
                extra[index] = value;
            }
        }

        end = close + 1;
        return extra;
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

    private static string SpecialCharacterText(byte special, ReadOnlySpan<byte> extra)
    {
        var text = SpecialNames.TryGetValue(special, out var name)
            ? $"[{name}]"
            : $"[{special}]";
        if (extra.Length == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text);
        builder.Append('{');
        foreach (var value in extra)
        {
            builder.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static int SpecialCharacterExtraBytes(byte special)
        => special switch
        {
            0x07 or 0x09 or 0x38 or 0x52 or 0x53 or 0x5b or 0x5c => 1,
            0x08 => 4,
            _ => 0
        };

    private static IReadOnlyDictionary<string, byte> BuildSpecialTokens()
    {
        var tokens = SpecialNames.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);
        tokens["Bold"] = 0x07;
        return tokens;
    }
}
