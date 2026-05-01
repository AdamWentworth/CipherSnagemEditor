using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public static class GameCubeScriptCodec
{
    private const uint TcodMagic = 0x54434f44;
    private const int TcodHeaderSize = 0x10;
    private const int SectionHeaderSize = 0x20;
    private const int ScriptIdentifierOffset = 0x28;

    private static readonly string[] OpNames =
    [
        "nop",
        "operator",
        "ldimm",
        "ldvar",
        "setvar",
        "setvector",
        "pop",
        "call",
        "return",
        "callstd",
        "jmptrue",
        "jmpfalse",
        "jmp",
        "reserve",
        "release",
        "exit",
        "setline",
        "ldncpvar"
    ];

    private static readonly Regex RawWordRegex = new(@"0x(?<value>[0-9a-fA-F]{8})", RegexOptions.Compiled);

    public static bool TryDecompileXds(byte[] scriptBytes, string sourceName, out string text, out string? error)
    {
        text = string.Empty;
        if (!TryParse(scriptBytes, out var script, out error))
        {
            return false;
        }

        var builder = new StringBuilder();
        var xdsName = Path.GetFileName(sourceName) + ".xds";
        var title = $"//// {xdsName}".PadRight(61, ' ') + "////";
        builder.AppendLine("/////////////////////////////////////////////////////////////");
        builder.AppendLine("/////////////////////////////////////////////////////////////");
        builder.AppendLine("////                                                     ////");
        builder.AppendLine(title);
        builder.AppendLine("//// Decompiled using Cipher Snagem Editor              ////");
        builder.AppendLine("//// Raw XDS/SCD round-trip format                       ////");
        builder.AppendLine("////                                                     ////");
        builder.AppendLine("/////////////////////////////////////////////////////////////");
        builder.AppendLine("/////////////////////////////////////////////////////////////");
        builder.AppendLine();
        builder.AppendLine("define ++XDSVersion 2.0 // matches the legacy GoD Tool XDS version gate");
        builder.AppendLine($"define ++ScriptIdentifier 0x{script.ScriptIdentifier:x8} // best not to change this");
        builder.AppendLine();
        builder.AppendLine("// Function table");
        foreach (var function in script.Functions)
        {
            builder.AppendLine($"// function {function.Index}: {function.Name} @0x{function.CodeOffset:x6}");
        }

        builder.AppendLine();
        builder.AppendLine("// Sections");
        foreach (var section in script.Sections)
        {
            builder.AppendLine($"// {section.Name} offset=0x{section.Offset:x6} size=0x{section.Size:x4} entries={section.EntryCount}");
        }

        AppendGlobals(builder, script);
        AppendDisassembly(builder, script);
        AppendRawWords(builder, scriptBytes);
        text = builder.ToString();
        return true;
    }

    public static bool TryCompileXds(string xdsText, out byte[] scriptBytes, out string? error)
    {
        scriptBytes = [];
        error = null;

        var block = ExtractRawWordsBlock(xdsText);
        if (block is null)
        {
            error = "No raw_scd_words block was found. This compiler currently supports Cipher Snagem's lossless raw XDS/SCD format.";
            return false;
        }

        var matches = RawWordRegex.Matches(block);
        if (matches.Count == 0)
        {
            error = "The raw_scd_words block did not contain any 32-bit words.";
            return false;
        }

        var bytes = new byte[matches.Count * 4];
        for (var index = 0; index < matches.Count; index++)
        {
            var raw = matches[index].Groups["value"].Value;
            if (!uint.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var word))
            {
                error = $"Invalid raw SCD word: 0x{raw}.";
                return false;
            }

            BigEndian.WriteUInt32(bytes, index * 4, word);
        }

        if (!TryParse(bytes, out _, out error))
        {
            return false;
        }

        scriptBytes = bytes;
        return true;
    }

    public static bool TryFindEmbeddedScript(byte[] containerBytes, out int offset, out int length)
    {
        offset = 0;
        length = 0;

        for (var current = 0; current <= containerBytes.Length - TcodHeaderSize; current += 4)
        {
            if (BigEndian.ReadUInt32(containerBytes, current) != TcodMagic)
            {
                continue;
            }

            var declaredSize = checked((int)BigEndian.ReadUInt32(containerBytes, current + 4));
            if (declaredSize < TcodHeaderSize || current + declaredSize > containerBytes.Length)
            {
                continue;
            }

            var candidate = containerBytes[current..(current + declaredSize)];
            if (!TryParse(candidate, out _, out _))
            {
                continue;
            }

            offset = current;
            length = declaredSize;
            return true;
        }

        return false;
    }

    private static void AppendGlobals(StringBuilder builder, ParsedScript script)
    {
        if (script.GlobalVariables.Count == 0
            && script.Strings.Count == 0
            && script.Vectors.Count == 0
            && script.Giris.Count == 0
            && script.Arrays.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("// Global data");
        foreach (var global in script.GlobalVariables)
        {
            builder.AppendLine($"// gvar_{global.Index:00} = {ConstantText(global.Type, global.Value)}");
        }

        for (var index = 0; index < script.Strings.Count; index++)
        {
            builder.AppendLine($"// strg_{index:00} = \"{EscapeComment(script.Strings[index])}\"");
        }

        for (var index = 0; index < script.Vectors.Count; index++)
        {
            var vector = script.Vectors[index];
            builder.AppendLine(FormattableString.Invariant($"// vector_{index:00} = <{vector.X:0.####} {vector.Y:0.####} {vector.Z:0.####}>"));
        }

        for (var index = 0; index < script.Giris.Count; index++)
        {
            var giri = script.Giris[index];
            builder.AppendLine($"// giri_{index:00} = GroupID:{giri.GroupId} ResourceID:{giri.ResourceId}");
        }

        for (var index = 0; index < script.Arrays.Count; index++)
        {
            var values = string.Join(", ", script.Arrays[index].Select(value => ConstantText(value.Type, value.Value)));
            builder.AppendLine($"// array_{index:00} = [{values}]");
        }
    }

    private static void AppendDisassembly(StringBuilder builder, ParsedScript script)
    {
        builder.AppendLine();
        builder.AppendLine("// Disassembly");

        var functionsByOffset = script.Functions
            .GroupBy(function => function.CodeOffset)
            .ToDictionary(group => group.Key, group => group.Select(function => function.Name).ToArray());
        foreach (var instruction in script.Instructions)
        {
            if (functionsByOffset.TryGetValue(instruction.WordIndex, out var names))
            {
                foreach (var name in names)
                {
                    builder.AppendLine($"// @{name}");
                }
            }

            builder.Append($"// {instruction.WordIndex:x6}: ");
            foreach (var word in instruction.Words)
            {
                builder.Append("0x");
                builder.Append(word.ToString("x8", CultureInfo.InvariantCulture));
                builder.Append(' ');
            }

            builder.AppendLine(InstructionText(instruction.Words));
        }
    }

    private static void AppendRawWords(StringBuilder builder, byte[] scriptBytes)
    {
        builder.AppendLine();
        builder.AppendLine("// The compiler uses this word stream. Keep the block balanced and use 32-bit hex words.");
        builder.AppendLine("raw_scd_words {");
        for (var offset = 0; offset < scriptBytes.Length; offset += 16)
        {
            builder.Append("    ");
            var lineLength = Math.Min(16, scriptBytes.Length - offset);
            for (var index = 0; index < lineLength; index += 4)
            {
                var word = BigEndian.ReadUInt32(scriptBytes, offset + index);
                builder.Append("0x");
                builder.Append(word.ToString("x8", CultureInfo.InvariantCulture));
                if (index + 4 < lineLength)
                {
                    builder.Append(' ');
                }
            }

            builder.AppendLine();
        }

        builder.AppendLine("}");
    }

    private static bool TryParse(byte[] scriptBytes, out ParsedScript script, out string? error)
    {
        script = ParsedScript.Empty;
        error = null;

        if (scriptBytes.Length < TcodHeaderSize)
        {
            error = "SCD script is too small to contain a TCOD header.";
            return false;
        }

        if (scriptBytes.Length % 4 != 0)
        {
            error = "SCD script length must be 32-bit aligned.";
            return false;
        }

        if (BigEndian.ReadUInt32(scriptBytes, 0) != TcodMagic)
        {
            error = "SCD script does not start with a TCOD header.";
            return false;
        }

        var declaredSize = checked((int)BigEndian.ReadUInt32(scriptBytes, 4));
        if (declaredSize < TcodHeaderSize || declaredSize > scriptBytes.Length)
        {
            error = $"SCD script declares invalid size 0x{declaredSize:x}.";
            return false;
        }

        var sections = new List<ScriptSection>();
        var offset = TcodHeaderSize;
        while (offset < declaredSize)
        {
            if (offset + SectionHeaderSize > declaredSize)
            {
                error = $"SCD section at 0x{offset:x} is truncated.";
                return false;
            }

            var size = checked((int)BigEndian.ReadUInt32(scriptBytes, offset + 4));
            if (size < SectionHeaderSize || offset + size > declaredSize)
            {
                error = $"SCD section at 0x{offset:x} has invalid size 0x{size:x}.";
                return false;
            }

            sections.Add(new ScriptSection(
                SectionName(BigEndian.ReadUInt32(scriptBytes, offset)),
                offset,
                size,
                checked((int)BigEndian.ReadUInt32(scriptBytes, offset + 0x10)),
                checked((int)BigEndian.ReadUInt32(scriptBytes, offset + 0x14))));
            offset += size;
        }

        if (offset != declaredSize)
        {
            error = "SCD sections did not end at the declared TCOD size.";
            return false;
        }

        var sectionMap = sections.ToDictionary(section => section.Name, StringComparer.OrdinalIgnoreCase);
        var functions = sectionMap.TryGetValue("FTBL", out var ftbl)
            ? ReadFunctions(scriptBytes, ftbl)
            : [];
        var globals = sectionMap.TryGetValue("GVAR", out var gvar)
            ? ReadConstants(scriptBytes, gvar)
            : [];
        var strings = sectionMap.TryGetValue("STRG", out var strg)
            ? ReadStrings(scriptBytes, strg)
            : [];
        var vectors = sectionMap.TryGetValue("VECT", out var vect)
            ? ReadVectors(scriptBytes, vect)
            : [];
        var giris = sectionMap.TryGetValue("GIRI", out var giri)
            ? ReadGiris(scriptBytes, giri)
            : [];
        var arrays = sectionMap.TryGetValue("ARRY", out var arry)
            ? ReadArrays(scriptBytes, arry)
            : [];
        var instructions = sectionMap.TryGetValue("CODE", out var code)
            ? ReadInstructions(scriptBytes, code)
            : [];

        var scriptIdentifier = scriptBytes.Length > ScriptIdentifierOffset + 4
            ? BigEndian.ReadUInt32(scriptBytes, ScriptIdentifierOffset)
            : 0u;
        script = new ParsedScript(scriptIdentifier, sections, functions, globals, strings, vectors, giris, arrays, instructions);
        return true;
    }

    private static List<ScriptFunction> ReadFunctions(byte[] data, ScriptSection section)
    {
        var functions = new List<ScriptFunction>();
        for (var index = 0; index < section.EntryCount; index++)
        {
            var entryOffset = section.Offset + SectionHeaderSize + (index * 8);
            if (entryOffset + 8 > section.Offset + section.Size)
            {
                break;
            }

            var codeOffset = checked((int)BigEndian.ReadUInt32(data, entryOffset));
            var nameRelativeOffset = checked((int)BigEndian.ReadUInt32(data, entryOffset + 4));
            var nameOffset = TcodHeaderSize + nameRelativeOffset;
            var name = nameOffset >= 0 && nameOffset < data.Length
                ? ReadNullTerminatedAscii(data, nameOffset, section.Offset + section.Size)
                : $"function_{index}";
            functions.Add(new ScriptFunction(index, codeOffset, string.IsNullOrWhiteSpace(name) ? $"function_{index}" : name));
        }

        return functions;
    }

    private static List<ScriptConstant> ReadConstants(byte[] data, ScriptSection section)
    {
        var constants = new List<ScriptConstant>();
        for (var index = 0; index < section.EntryCount; index++)
        {
            var entryOffset = section.Offset + SectionHeaderSize + (index * 8);
            if (entryOffset + 8 > section.Offset + section.Size)
            {
                break;
            }

            constants.Add(new ScriptConstant(
                index,
                BigEndian.ReadUInt16(data, entryOffset + 2),
                BigEndian.ReadUInt32(data, entryOffset + 4)));
        }

        return constants;
    }

    private static List<string> ReadStrings(byte[] data, ScriptSection section)
    {
        var strings = new List<string>();
        var offset = section.Offset + SectionHeaderSize;
        for (var index = 0; index < section.EntryCount && offset < section.Offset + section.Size; index++)
        {
            var value = ReadNullTerminatedAscii(data, offset, section.Offset + section.Size);
            strings.Add(value);
            offset += Encoding.ASCII.GetByteCount(value) + 1;
        }

        return strings;
    }

    private static List<ScriptVector> ReadVectors(byte[] data, ScriptSection section)
    {
        var vectors = new List<ScriptVector>();
        for (var index = 0; index < section.EntryCount; index++)
        {
            var entryOffset = section.Offset + SectionHeaderSize + (index * 12);
            if (entryOffset + 12 > section.Offset + section.Size)
            {
                break;
            }

            vectors.Add(new ScriptVector(
                ReadSingle(data, entryOffset),
                ReadSingle(data, entryOffset + 4),
                ReadSingle(data, entryOffset + 8)));
        }

        return vectors;
    }

    private static List<ScriptGiri> ReadGiris(byte[] data, ScriptSection section)
    {
        var giris = new List<ScriptGiri>();
        for (var index = 0; index < section.EntryCount; index++)
        {
            var entryOffset = section.Offset + SectionHeaderSize + (index * 8);
            if (entryOffset + 8 > section.Offset + section.Size)
            {
                break;
            }

            giris.Add(new ScriptGiri(
                checked((int)BigEndian.ReadUInt32(data, entryOffset)),
                checked((int)BigEndian.ReadUInt32(data, entryOffset + 4))));
        }

        return giris;
    }

    private static List<List<ScriptConstantValue>> ReadArrays(byte[] data, ScriptSection section)
    {
        var arrays = new List<List<ScriptConstantValue>>();
        var tableStart = section.Offset + SectionHeaderSize;
        for (var index = 0; index < section.EntryCount; index++)
        {
            var pointerOffset = tableStart + (index * 4);
            if (pointerOffset + 4 > section.Offset + section.Size)
            {
                break;
            }

            var relativeStart = checked((int)BigEndian.ReadUInt32(data, pointerOffset));
            var headerOffset = tableStart + relativeStart - 0x10;
            var valueOffset = tableStart + relativeStart;
            if (relativeStart < 0x10 || headerOffset + 0x10 > section.Offset + section.Size)
            {
                arrays.Add([]);
                continue;
            }

            var count = checked((int)BigEndian.ReadUInt32(data, headerOffset));
            var values = new List<ScriptConstantValue>();
            for (var valueIndex = 0; valueIndex < count; valueIndex++)
            {
                var entryOffset = valueOffset + (valueIndex * 8);
                if (entryOffset + 8 > section.Offset + section.Size)
                {
                    break;
                }

                values.Add(new ScriptConstantValue(
                    BigEndian.ReadUInt16(data, entryOffset),
                    BigEndian.ReadUInt32(data, entryOffset + 4)));
            }

            arrays.Add(values);
        }

        return arrays;
    }

    private static List<ScriptInstruction> ReadInstructions(byte[] data, ScriptSection section)
    {
        var instructions = new List<ScriptInstruction>();
        var instructionWordCount = section.AuxiliaryCount > 0
            ? section.AuxiliaryCount
            : (section.Size - SectionHeaderSize) / 4;
        var wordsStart = section.Offset + SectionHeaderSize;
        var wordIndex = 0;
        while (wordIndex < instructionWordCount)
        {
            var wordOffset = wordsStart + (wordIndex * 4);
            if (wordOffset + 4 > section.Offset + section.Size)
            {
                break;
            }

            var word = BigEndian.ReadUInt32(data, wordOffset);
            var op = (int)((word >> 24) & 0xff);
            var sub = (int)((word >> 16) & 0xff);
            var length = op == 2 && sub is not (3 or 4) ? 2 : 1;
            if (wordIndex + length > instructionWordCount)
            {
                length = 1;
            }

            var instructionWords = new uint[length];
            instructionWords[0] = word;
            if (length == 2)
            {
                instructionWords[1] = BigEndian.ReadUInt32(data, wordOffset + 4);
            }

            instructions.Add(new ScriptInstruction(wordIndex, instructionWords));
            wordIndex += length;
        }

        return instructions;
    }

    private static string? ExtractRawWordsBlock(string xdsText)
    {
        var markerIndex = xdsText.IndexOf("raw_scd_words", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var openIndex = xdsText.IndexOf('{', markerIndex);
        if (openIndex < 0)
        {
            return null;
        }

        var closeIndex = xdsText.IndexOf('}', openIndex + 1);
        return closeIndex < 0 ? null : xdsText[(openIndex + 1)..closeIndex];
    }

    private static string InstructionText(IReadOnlyList<uint> words)
    {
        var word = words[0];
        var op = (int)((word >> 24) & 0xff);
        var sub = (int)((word >> 16) & 0xff);
        var param = unchecked((short)(word & 0xffff));
        var longParameter = (int)(word & 0x00ff_ffff);
        var opName = op >= 0 && op < OpNames.Length ? OpNames[op] : $"unknown{op}";

        return op switch
        {
            1 => $"{opName} operator_{sub}",
            2 => words.Count == 2
                ? $"{opName} {ConstantText((ushort)sub, words[1])}"
                : $"{opName} {ConstantTypeName(sub)}({param})",
            3 => $"{opName} {VariableText(sub, param)}",
            4 => $"{opName} {VariableText(sub, param)}",
            5 => $"{opName} {VectorDimensionText(sub)} {VariableText(sub, param)}",
            6 => $"{opName} {sub}",
            7 => $"{opName} @0x{longParameter:x6}",
            8 => opName,
            9 => $"{opName} class_{sub}.function_{param}",
            10 => $"{opName} @0x{longParameter:x6}",
            11 => $"{opName} @0x{longParameter:x6}",
            12 => $"{opName} @0x{longParameter:x6}",
            13 => $"{opName} {sub}",
            14 => $"{opName} {sub}",
            15 => opName,
            16 => $"{opName} {param}",
            17 => $"{opName} {VariableText(sub, param)}",
            _ => $"{opName} sub={sub} param={param}"
        };
    }

    private static string ConstantText(ushort type, uint value)
        => (int)type switch
        {
            0 => "Null",
            1 => unchecked((int)value) >= 0x200 ? $"Int(0x{value:x8})" : $"Int({unchecked((int)value)})",
            2 => FormattableString.Invariant($"Float({ReadSingle(value):0.####})"),
            3 => $"String({unchecked((int)value)})",
            4 => $"Vector({unchecked((int)value)})",
            5 => $"Matrix({unchecked((int)value)})",
            6 => $"Object({unchecked((int)value)})",
            7 => $"Array({unchecked((int)value)})",
            8 => $"Text({unchecked((int)value)})",
            35 => $"Character({unchecked((int)value)})",
            37 => $"Pokemon({unchecked((int)value)})",
            53 => $"Pointer(0x{value:x8})",
            _ => $"{ConstantTypeName(type)}({unchecked((int)value)})"
        };

    private static string ConstantTypeName(int type)
        => type switch
        {
            0 => "Void",
            1 => "Int",
            2 => "Float",
            3 => "String",
            4 => "Vector",
            5 => "Matrix",
            6 => "Object",
            7 => "Array",
            8 => "Text",
            35 => "Character",
            37 => "Pokemon",
            53 => "Pointer",
            _ => $"Type{type}"
        };

    private static string VariableText(int subOpCode, int parameter)
    {
        var level = subOpCode & 0x0f;
        return level switch
        {
            0 => $"gvar_{parameter:00}",
            1 => parameter < 0 ? $"var_{-parameter}" : $"arg_{parameter - 1}",
            2 => "LastResult",
            _ when parameter == 0x80 => "player_character",
            _ when parameter is > 0 and < 0x80 => $"class_object_{parameter}",
            _ when parameter <= 0x120 => $"character_{parameter - 0x80:00}",
            _ when parameter is >= 0x200 and < 0x300 => $"array_{parameter - 0x200:00}",
            _ => "_invalid_var_"
        };
    }

    private static string VectorDimensionText(int subOpCode)
        => ((subOpCode >> 4) & 0x0f) switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            var value => $"dim{value}"
        };

    private static string SectionName(uint magic)
    {
        Span<byte> bytes = stackalloc byte[4];
        BigEndian.WriteUInt32(bytes, 0, magic);
        return Encoding.ASCII.GetString(bytes);
    }

    private static string ReadNullTerminatedAscii(byte[] data, int offset, int limit)
    {
        var end = offset;
        limit = Math.Min(limit, data.Length);
        while (end < limit && data[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(data, offset, end - offset);
    }

    private static float ReadSingle(byte[] data, int offset)
        => ReadSingle(BigEndian.ReadUInt32(data, offset));

    private static float ReadSingle(uint raw)
    {
        Span<byte> littleEndian = stackalloc byte[4];
        BitConverter.TryWriteBytes(littleEndian, raw);
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.ToSingle(littleEndian);
        }

        Span<byte> bigEndian = stackalloc byte[4];
        BigEndian.WriteUInt32(bigEndian, 0, raw);
        return BitConverter.ToSingle(bigEndian);
    }

    private static string EscapeComment(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

    private sealed record ParsedScript(
        uint ScriptIdentifier,
        IReadOnlyList<ScriptSection> Sections,
        IReadOnlyList<ScriptFunction> Functions,
        IReadOnlyList<ScriptConstant> GlobalVariables,
        IReadOnlyList<string> Strings,
        IReadOnlyList<ScriptVector> Vectors,
        IReadOnlyList<ScriptGiri> Giris,
        IReadOnlyList<IReadOnlyList<ScriptConstantValue>> Arrays,
        IReadOnlyList<ScriptInstruction> Instructions)
    {
        public static ParsedScript Empty { get; } = new(0, [], [], [], [], [], [], [], []);
    }

    private sealed record ScriptSection(string Name, int Offset, int Size, int EntryCount, int AuxiliaryCount);

    private sealed record ScriptFunction(int Index, int CodeOffset, string Name);

    private sealed record ScriptConstant(int Index, ushort Type, uint Value);

    private sealed record ScriptConstantValue(ushort Type, uint Value);

    private sealed record ScriptVector(float X, float Y, float Z);

    private sealed record ScriptGiri(int GroupId, int ResourceId);

    private sealed record ScriptInstruction(int WordIndex, IReadOnlyList<uint> Words);
}
