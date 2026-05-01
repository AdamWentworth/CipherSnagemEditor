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
    private const string HighLevelBeginMarker = "// cse_xds_begin";
    private const string HighLevelEndMarker = "// cse_xds_end";

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
    private static readonly Regex FunctionHeaderRegex = new(
        @"^function\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)\s*\((?<args>[^)]*)\)\s*\{\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        AppendHighLevelSubset(builder, script);
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
        if (block is not null
            && TryCompileHighLevelSubset(xdsText, block, out scriptBytes, out error))
        {
            return true;
        }

        if (block is null)
        {
            error = "No editable XDS subset or raw_scd_words block was found.";
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

    private static void AppendHighLevelSubset(StringBuilder builder, ParsedScript script)
    {
        if (script.Functions.Count == 0 || script.Instructions.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("// Editable XDS subset");
        builder.AppendLine("// This mirrors the legacy function-block shape and takes priority over raw_scd_words when it compiles.");
        builder.AppendLine(HighLevelBeginMarker);

        var functions = script.Functions
            .OrderBy(function => function.CodeOffset)
            .ToArray();
        var instructionMap = script.Instructions.ToDictionary(instruction => instruction.WordIndex);
        var functionNamesByOffset = functions
            .GroupBy(function => function.CodeOffset)
            .ToDictionary(group => group.Key, group => "@" + group.First().Name);
        var totalWords = script.Instructions.Sum(instruction => instruction.Words.Count);

        for (var index = 0; index < functions.Length; index++)
        {
            var function = functions[index];
            var endOffset = index + 1 < functions.Length ? functions[index + 1].CodeOffset : totalWords;
            builder.AppendLine($"function @{function.Name}() {{");
            var wordIndex = function.CodeOffset;
            while (wordIndex < endOffset)
            {
                if (!instructionMap.TryGetValue(wordIndex, out var instruction))
                {
                    builder.AppendLine($"    // missing instruction at 0x{wordIndex:x6}");
                    wordIndex++;
                    continue;
                }

                builder.AppendLine("    " + HighLevelInstructionText(instruction.Words, functionNamesByOffset));
                wordIndex += instruction.Words.Count;
            }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        builder.AppendLine(HighLevelEndMarker);
    }

    private static bool TryCompileHighLevelSubset(string xdsText, string rawWordsBlock, out byte[] scriptBytes, out string? error)
    {
        scriptBytes = [];
        error = null;

        var highLevelBlock = ExtractHighLevelBlock(xdsText);
        if (string.IsNullOrWhiteSpace(highLevelBlock))
        {
            return false;
        }

        var templateWords = RawWordRegex.Matches(rawWordsBlock);
        if (templateWords.Count == 0)
        {
            error = "The raw_scd_words template block did not contain any 32-bit words.";
            return false;
        }

        var templateBytes = new byte[templateWords.Count * 4];
        for (var index = 0; index < templateWords.Count; index++)
        {
            if (!uint.TryParse(templateWords[index].Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var word))
            {
                error = $"Invalid raw SCD word: 0x{templateWords[index].Groups["value"].Value}.";
                return false;
            }

            BigEndian.WriteUInt32(templateBytes, index * 4, word);
        }

        if (!TryParse(templateBytes, out var template, out error))
        {
            return false;
        }

        if (!TryParseHighLevelFunctions(highLevelBlock, out var functions, out error))
        {
            return false;
        }

        if (functions.Count == 0)
        {
            return false;
        }

        if (!TryBuildCodeWords(functions, out var functionOffsets, out var codeWords, out error))
        {
            return false;
        }

        scriptBytes = RebuildScript(templateBytes, template, functions, functionOffsets, codeWords);
        return TryParse(scriptBytes, out _, out error);
    }

    private static bool TryParseHighLevelFunctions(string block, out List<HighLevelFunction> functions, out string? error)
    {
        functions = [];
        error = null;

        HighLevelFunction? currentFunction = null;
        var lineNumber = 0;
        foreach (var rawLine in block.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            lineNumber++;
            var line = StripInlineComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var match = FunctionHeaderRegex.Match(line);
            if (match.Success)
            {
                if (currentFunction is not null)
                {
                    error = $"Nested function at editable XDS line {lineNumber}.";
                    return false;
                }

                currentFunction = new HighLevelFunction(NormalizeFunctionName(match.Groups["name"].Value), []);
                continue;
            }

            if (line == "}")
            {
                if (currentFunction is null)
                {
                    error = $"Unexpected function close at editable XDS line {lineNumber}.";
                    return false;
                }

                functions.Add(currentFunction);
                currentFunction = null;
                continue;
            }

            if (currentFunction is null)
            {
                error = $"Instruction outside function at editable XDS line {lineNumber}: {line}";
                return false;
            }

            if (!TryParseHighLevelInstruction(line, out var instruction, out error))
            {
                error = $"Editable XDS line {lineNumber}: {error}";
                return false;
            }

            currentFunction.Instructions.Add(instruction);
        }

        if (currentFunction is not null)
        {
            error = $"Function @{currentFunction.Name} was not closed.";
            return false;
        }

        return true;
    }

    private static bool TryParseHighLevelInstruction(string line, out HighLevelInstruction instruction, out string? error)
    {
        instruction = HighLevelInstruction.Empty;
        error = null;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            error = "Empty instruction.";
            return false;
        }

        var op = parts[0].ToLowerInvariant();
        if (op == "raw")
        {
            var rawWords = RawWordRegex.Matches(line)
                .Select(match => uint.Parse(match.Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
            if (rawWords.Length == 0)
            {
                error = "raw requires at least one 32-bit word.";
                return false;
            }

            instruction = new HighLevelInstruction(rawWords.Length, _ => rawWords);
            return true;
        }

        if (op is "nop" or "return" or "exit")
        {
            var opcode = op switch
            {
                "nop" => 0,
                "return" => 8,
                _ => 15
            };
            instruction = SingleWord(opcode, 0, 0);
            return true;
        }

        if (op is "operator" or "pop" or "reserve" or "release")
        {
            if (parts.Length < 2 || !TryParseInteger(parts[1], out var value))
            {
                error = $"{op} requires an integer parameter.";
                return false;
            }

            var opcode = op switch
            {
                "operator" => 1,
                "pop" => 6,
                "reserve" => 13,
                _ => 14
            };
            instruction = SingleWord(opcode, value, 0);
            return true;
        }

        if (op == "setline")
        {
            if (parts.Length < 2 || !TryParseInteger(parts[1], out var lineNumber))
            {
                error = "setline requires an integer parameter.";
                return false;
            }

            instruction = SingleWord(16, 0, lineNumber);
            return true;
        }

        if (op is "ldvar" or "setvar" or "ldncpvar")
        {
            if (parts.Length < 2 || !TryParseVariable(parts[1], out var level, out var parameter))
            {
                error = $"{op} requires a known variable name.";
                return false;
            }

            var opcode = op switch
            {
                "ldvar" => 3,
                "setvar" => 4,
                _ => 17
            };
            instruction = SingleWord(opcode, level, parameter);
            return true;
        }

        if (op == "setvector")
        {
            if (parts.Length < 3 || !TryParseVectorDimension(parts[1], out var dimension) || !TryParseVariable(parts[2], out var level, out var parameter))
            {
                error = "setvector requires a dimension and variable name.";
                return false;
            }

            instruction = SingleWord(5, (dimension << 4) | level, parameter);
            return true;
        }

        if (op == "ldimm")
        {
            var constantText = line["ldimm".Length..].Trim();
            if (!TryParseConstant(constantText, out var type, out var value))
            {
                error = $"Unsupported immediate constant: {constantText}";
                return false;
            }

            if (type is 3 or 4)
            {
                instruction = SingleWord(2, type, unchecked((int)value));
            }
            else
            {
                var first = BuildWord(2, type, 0);
                instruction = new HighLevelInstruction(2, _ => [first, value]);
            }

            return true;
        }

        if (op == "callstd")
        {
            if (parts.Length < 2 || !TryParseClassFunction(parts[1], out var classId, out var functionId))
            {
                error = "callstd requires class_<id>.function_<id>.";
                return false;
            }

            instruction = SingleWord(9, classId, functionId);
            return true;
        }

        if (op is "call" or "goto" or "jmptrue" or "jmpfalse")
        {
            if (parts.Length < 2)
            {
                error = $"{op} requires a target.";
                return false;
            }

            var opcode = op switch
            {
                "call" => 7,
                "jmptrue" => 10,
                "jmpfalse" => 11,
                _ => 12
            };
            var target = parts[1].TrimEnd('(', ')');
            instruction = new HighLevelInstruction(1, offsets =>
            {
                var resolved = ResolveLocation(target, offsets);
                return [BuildLongWord(opcode, resolved)];
            });
            return true;
        }

        if (op.StartsWith('@') && line.EndsWith("()", StringComparison.Ordinal))
        {
            var target = op.TrimEnd('(', ')');
            instruction = new HighLevelInstruction(1, offsets => [BuildLongWord(7, ResolveLocation(target, offsets))]);
            return true;
        }

        error = $"Unsupported editable XDS instruction: {line}";
        return false;
    }

    private static bool TryBuildCodeWords(
        IReadOnlyList<HighLevelFunction> functions,
        out Dictionary<string, int> functionOffsets,
        out List<uint> codeWords,
        out string? error)
    {
        error = null;
        functionOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        codeWords = [];

        var currentOffset = 0;
        foreach (var function in functions)
        {
            functionOffsets[function.Name] = currentOffset;
            functionOffsets["@" + function.Name] = currentOffset;
            currentOffset += function.Instructions.Sum(instruction => instruction.WordLength);
        }

        try
        {
            foreach (var function in functions)
            {
                foreach (var instruction in function.Instructions)
                {
                    codeWords.AddRange(instruction.ToWords(functionOffsets));
                }
            }
        }
        catch (InvalidDataException ex)
        {
            error = ex.Message;
            return false;
        }

        return true;
    }

    private static byte[] RebuildScript(
        byte[] templateBytes,
        ParsedScript template,
        IReadOnlyList<HighLevelFunction> functions,
        IReadOnlyDictionary<string, int> functionOffsets,
        IReadOnlyList<uint> codeWords)
    {
        var sections = new List<byte[]>
        {
            BuildFtblSection(functions, functionOffsets, template.ScriptIdentifier),
            BuildHeadSection(functions, functionOffsets),
            BuildCodeSection(functions.Count, codeWords)
        };

        foreach (var section in template.Sections.Where(section => section.Name is not ("FTBL" or "HEAD" or "CODE")))
        {
            sections.Add(templateBytes[section.Offset..(section.Offset + section.Size)]);
        }

        var totalLength = TcodHeaderSize + sections.Sum(section => section.Length);
        var bytes = new byte[totalLength];
        BigEndian.WriteUInt32(bytes, 0, TcodMagic);
        BigEndian.WriteUInt32(bytes, 4, checked((uint)totalLength));
        var offset = TcodHeaderSize;
        foreach (var section in sections)
        {
            section.CopyTo(bytes.AsSpan(offset));
            offset += section.Length;
        }

        return bytes;
    }

    private static byte[] BuildFtblSection(
        IReadOnlyList<HighLevelFunction> functions,
        IReadOnlyDictionary<string, int> functionOffsets,
        uint scriptIdentifier)
    {
        var uniqueNames = functions
            .Select(function => function.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var nameOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
        var stringBytes = new List<byte>();
        foreach (var name in uniqueNames)
        {
            nameOffsets[name] = stringBytes.Count;
            stringBytes.AddRange(Encoding.ASCII.GetBytes(name));
            stringBytes.Add(0);
        }

        var entryBytes = functions.Count * 8;
        var firstStringOffset = SectionHeaderSize + entryBytes;
        var sectionSize = Align16(firstStringOffset + stringBytes.Count);
        var bytes = new byte[sectionSize];
        BigEndian.WriteUInt32(bytes, 0x00, 0x4654424c);
        BigEndian.WriteUInt32(bytes, 0x04, checked((uint)sectionSize));
        BigEndian.WriteUInt32(bytes, 0x10, checked((uint)functions.Count));
        BigEndian.WriteUInt32(bytes, 0x14, checked((uint)firstStringOffset));
        BigEndian.WriteUInt32(bytes, 0x18, scriptIdentifier);

        for (var index = 0; index < functions.Count; index++)
        {
            var entryOffset = SectionHeaderSize + (index * 8);
            var function = functions[index];
            BigEndian.WriteUInt32(bytes, entryOffset, checked((uint)functionOffsets[function.Name]));
            BigEndian.WriteUInt32(bytes, entryOffset + 4, checked((uint)(firstStringOffset + nameOffsets[function.Name])));
        }

        stringBytes.ToArray().CopyTo(bytes.AsSpan(firstStringOffset));
        return bytes;
    }

    private static byte[] BuildHeadSection(
        IReadOnlyList<HighLevelFunction> functions,
        IReadOnlyDictionary<string, int> functionOffsets)
    {
        var sectionSize = Align16(SectionHeaderSize + (functions.Count * 4));
        var bytes = new byte[sectionSize];
        BigEndian.WriteUInt32(bytes, 0x00, 0x48454144);
        BigEndian.WriteUInt32(bytes, 0x04, checked((uint)sectionSize));
        BigEndian.WriteUInt32(bytes, 0x10, checked((uint)functions.Count));
        for (var index = 0; index < functions.Count; index++)
        {
            BigEndian.WriteUInt32(bytes, SectionHeaderSize + (index * 4), checked((uint)functionOffsets[functions[index].Name]));
        }

        return bytes;
    }

    private static byte[] BuildCodeSection(int functionCount, IReadOnlyList<uint> codeWords)
    {
        var sectionSize = Align16(SectionHeaderSize + (codeWords.Count * 4));
        var bytes = new byte[sectionSize];
        BigEndian.WriteUInt32(bytes, 0x00, 0x434f4445);
        BigEndian.WriteUInt32(bytes, 0x04, checked((uint)sectionSize));
        BigEndian.WriteUInt32(bytes, 0x10, checked((uint)functionCount));
        BigEndian.WriteUInt32(bytes, 0x14, checked((uint)codeWords.Count));
        for (var index = 0; index < codeWords.Count; index++)
        {
            BigEndian.WriteUInt32(bytes, SectionHeaderSize + (index * 4), codeWords[index]);
        }

        return bytes;
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
        var marker = Regex.Match(xdsText, @"(?im)^\s*raw_scd_words\s*\{");
        if (!marker.Success)
        {
            return null;
        }

        var openIndex = xdsText.IndexOf('{', marker.Index);
        if (openIndex < 0)
        {
            return null;
        }

        var closeIndex = xdsText.IndexOf('}', openIndex + 1);
        return closeIndex < 0 ? null : xdsText[(openIndex + 1)..closeIndex];
    }

    private static string? ExtractHighLevelBlock(string xdsText)
    {
        var begin = xdsText.IndexOf(HighLevelBeginMarker, StringComparison.OrdinalIgnoreCase);
        if (begin < 0)
        {
            return null;
        }

        begin += HighLevelBeginMarker.Length;
        var end = xdsText.IndexOf(HighLevelEndMarker, begin, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? null : xdsText[begin..end];
    }

    private static string HighLevelInstructionText(IReadOnlyList<uint> words, IReadOnlyDictionary<int, string> functionNamesByOffset)
    {
        var word = words[0];
        var op = (int)((word >> 24) & 0xff);
        var sub = (int)((word >> 16) & 0xff);
        var param = unchecked((short)(word & 0xffff));
        var longParameter = (int)(word & 0x00ff_ffff);

        return op switch
        {
            0 => "nop",
            1 => $"operator {sub}",
            2 => words.Count == 2
                ? $"ldimm {ConstantText((ushort)sub, words[1])}"
                : $"ldimm {ConstantText((ushort)sub, unchecked((uint)param))}",
            3 => $"ldvar {VariableText(sub, param)}",
            4 => $"setvar {VariableText(sub, param)}",
            5 => $"setvector {VectorDimensionText(sub)} {VariableText(sub, param)}",
            6 => $"pop {sub}",
            7 => $"call {LocationText(longParameter, functionNamesByOffset)}()",
            8 => "return",
            9 => CallStdText(sub, param),
            10 => $"jmptrue {LocationText(longParameter, functionNamesByOffset)}",
            11 => $"jmpfalse {LocationText(longParameter, functionNamesByOffset)}",
            12 => $"goto {LocationText(longParameter, functionNamesByOffset)}",
            13 => $"reserve {sub}",
            14 => $"release {sub}",
            15 => "exit",
            16 => $"setline {param}",
            17 => $"ldncpvar {VariableText(sub, param)}",
            _ => "raw " + string.Join(" ", words.Select(raw => $"0x{raw:x8}"))
        };
    }

    private static string LocationText(int offset, IReadOnlyDictionary<int, string> functionNamesByOffset)
        => functionNamesByOffset.TryGetValue(offset, out var name) ? name : $"0x{offset:x6}";

    private static string CallStdText(int classId, int functionId)
    {
        var name = GameCubeScriptCatalog.FunctionDisplayName(classId, functionId);
        var comment = GameCubeScriptCatalog.FunctionComment(classId, functionId);
        return string.IsNullOrWhiteSpace(comment) ? $"callstd {name}" : $"callstd {name} // {comment}";
    }

    private static string StripInlineComment(string line)
    {
        var quoted = false;
        for (var index = 0; index + 1 < line.Length; index++)
        {
            if (line[index] == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (!quoted && line[index] == '/' && line[index + 1] == '/')
            {
                return line[..index];
            }
        }

        return line;
    }

    private static string NormalizeFunctionName(string value)
        => value.Trim().TrimStart('@');

    private static HighLevelInstruction SingleWord(int op, int sub, int parameter)
        => new(1, _ => [BuildWord(op, sub, parameter)]);

    private static uint BuildWord(int op, int sub, int parameter)
        => ((uint)(op & 0xff) << 24) | ((uint)(sub & 0xff) << 16) | (unchecked((uint)parameter) & 0xffff);

    private static uint BuildLongWord(int op, int parameter)
        => ((uint)(op & 0xff) << 24) | (unchecked((uint)parameter) & 0x00ff_ffff);

    private static int ResolveLocation(string target, IReadOnlyDictionary<string, int> functionOffsets)
    {
        target = target.Trim().TrimEnd('(', ')');
        if (functionOffsets.TryGetValue(target, out var offset)
            || functionOffsets.TryGetValue(NormalizeFunctionName(target), out offset))
        {
            return offset;
        }

        if (TryParseInteger(target, out offset))
        {
            return offset;
        }

        throw new InvalidDataException($"Unknown XDS location target: {target}");
    }

    private static bool TryParseInteger(string text, out int value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseVariable(string text, out int level, out int parameter)
    {
        level = 0;
        parameter = 0;
        text = text.Trim().TrimEnd(',', ';');

        if (text.Equals("LastResult", StringComparison.OrdinalIgnoreCase))
        {
            level = 2;
            return true;
        }

        if (text.Equals("player_character", StringComparison.OrdinalIgnoreCase))
        {
            level = 3;
            parameter = 0x80;
            return true;
        }

        if (TryParsePrefixedIndex(text, "gvar_", out parameter))
        {
            level = 0;
            return true;
        }

        if (TryParsePrefixedIndex(text, "arg_", out parameter))
        {
            level = 1;
            parameter++;
            return true;
        }

        if (TryParsePrefixedIndex(text, "var_", out parameter))
        {
            level = 1;
            parameter = -parameter;
            return true;
        }

        if (TryParsePrefixedIndex(text, "character_", out parameter))
        {
            level = 3;
            parameter += 0x80;
            return true;
        }

        if (TryParsePrefixedIndex(text, "array_", out parameter))
        {
            level = 3;
            parameter += 0x200;
            return true;
        }

        if (TryParsePrefixedIndex(text, "class_object_", out parameter))
        {
            level = 3;
            return true;
        }

        return false;
    }

    private static bool TryParsePrefixedIndex(string text, string prefix, out int value)
    {
        value = 0;
        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(text[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseVectorDimension(string text, out int dimension)
    {
        dimension = text.ToLowerInvariant() switch
        {
            "x" => 0,
            "y" => 1,
            "z" => 2,
            _ => -1
        };
        if (dimension >= 0)
        {
            return true;
        }

        if (text.StartsWith("dim", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(text[3..], NumberStyles.Integer, CultureInfo.InvariantCulture, out dimension);
        }

        return false;
    }

    private static bool TryParseClassFunction(string text, out int classId, out int functionId)
    {
        return GameCubeScriptCatalog.TryResolveClassFunction(text, out classId, out functionId);
    }

    private static bool TryParseConstant(string text, out int type, out uint value)
    {
        type = 0;
        value = 0;
        text = text.Trim();
        if (text.Equals("Null", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var open = text.IndexOf('(');
        var close = text.LastIndexOf(')');
        if (open <= 0 || close <= open)
        {
            return false;
        }

        var kind = text[..open].Trim();
        var rawValue = text[(open + 1)..close].Trim();
        type = kind.ToLowerInvariant() switch
        {
            "int" => 1,
            "float" => 2,
            "string" => 3,
            "vector" => 4,
            "matrix" => 5,
            "object" => 6,
            "array" => 7,
            "text" => 8,
            "character" => 35,
            "pokemon" => 37,
            "pointer" => 53,
            _ when kind.StartsWith("type", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(kind[4..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var customType) => customType,
            _ => -1
        };
        if (type < 0)
        {
            return false;
        }

        if (type == 2)
        {
            if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var single))
            {
                return false;
            }

            value = SingleToUInt32(single);
            return true;
        }

        if (!TryParseInteger(rawValue, out var parsed))
        {
            return false;
        }

        value = unchecked((uint)parsed);
        return true;
    }

    private static uint SingleToUInt32(float value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BitConverter.TryWriteBytes(bytes, value);
        if (BitConverter.IsLittleEndian)
        {
            return BitConverter.ToUInt32(bytes);
        }

        return BigEndian.ReadUInt32(bytes, 0);
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
            9 => $"{opName} {GameCubeScriptCatalog.FunctionDisplayName(sub, param)}",
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

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private sealed record HighLevelFunction(string Name, List<HighLevelInstruction> Instructions);

    private sealed record HighLevelInstruction(int WordLength, Func<IReadOnlyDictionary<string, int>, IReadOnlyList<uint>> ToWords)
    {
        public static HighLevelInstruction Empty { get; } = new(0, _ => []);
    }

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
