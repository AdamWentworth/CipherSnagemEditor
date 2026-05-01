using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.Tests;

public sealed class GameCubeScriptCodecTests
{
    [Fact]
    public void DecompilesEditableFunctionSubsetAndCompilesItBeforeRawFallback()
    {
        var script = CreateSampleScript(0x08000000);

        Assert.True(GameCubeScriptCodec.TryDecompileXds(script, "sample.scd", out var text, out var decompileError), decompileError);
        Assert.Contains("// cse_xds_begin", text);
        Assert.Contains("function @main()", text);
        Assert.Contains("    return", text);
        Assert.Contains("raw_scd_words", text);

        var edited = text.Replace("    return", "    exit", StringComparison.Ordinal);

        Assert.True(GameCubeScriptCodec.TryCompileXds(edited, out var compiled, out var compileError), compileError);

        Assert.NotEqual(script, compiled);
        Assert.Equal(0x0f000000u, FirstCodeWord(compiled));
    }

    [Fact]
    public void CompilesUneditedEditableSubsetBackToOriginalBytes()
    {
        var script = CreateSampleScript(0x1000002a, 0x08000000);

        Assert.True(GameCubeScriptCodec.TryDecompileXds(script, "sample.scd", out var text, out var decompileError), decompileError);
        Assert.True(GameCubeScriptCodec.TryCompileXds(text, out var compiled, out var compileError), compileError);

        Assert.Equal(script, compiled);
    }

    [Fact]
    public void DecompilesAndCompilesNamedScriptClassFunctions()
    {
        var script = CreateSampleScript(Instruction(9, 35, 73), 0x08000000);

        Assert.True(GameCubeScriptCodec.TryDecompileXds(script, "sample.scd", out var text, out var decompileError), decompileError);

        Assert.Contains("callstd Character.talk", text);
        Assert.Contains("class_35.function_73", text);
        Assert.True(GameCubeScriptCodec.TryCompileXds(text, out var compiled, out var compileError), compileError);
        Assert.Equal(script, compiled);
    }

    [Fact]
    public void StillCompilesLegacyNumericScriptClassFunctions()
    {
        var script = CreateSampleScript(Instruction(9, 0, 17), 0x08000000);

        Assert.True(GameCubeScriptCodec.TryDecompileXds(script, "sample.scd", out var text, out var decompileError), decompileError);
        var legacyText = text.Replace("callstd pause", "callstd class_0.function_17", StringComparison.Ordinal);

        Assert.True(GameCubeScriptCodec.TryCompileXds(legacyText, out var compiled, out var compileError), compileError);
        Assert.Equal(script, compiled);
    }

    private static uint FirstCodeWord(byte[] script)
    {
        var offset = 0x10;
        while (offset + 0x20 <= script.Length)
        {
            var magic = BigEndian.ReadUInt32(script, offset);
            var size = checked((int)BigEndian.ReadUInt32(script, offset + 4));
            if (magic == 0x434f4445)
            {
                return BigEndian.ReadUInt32(script, offset + 0x20);
            }

            offset += size;
        }

        throw new InvalidDataException("No CODE section was found.");
    }

    private static uint Instruction(int op, int sub, int parameter)
        => ((uint)(op & 0xff) << 24) | ((uint)(sub & 0xff) << 16) | (unchecked((uint)parameter) & 0xffff);

    private static byte[] CreateSampleScript(params uint[] codeWords)
    {
        uint[][] sections =
        [
            [
                0x4654424c, 0x30, 0, 0,
                1, 0x28, 0x12345678, 0,
                0, 0x28, 0x6d61696e, 0
            ],
            [
                0x48454144, 0x30, 0, 0,
                1, 0, 0, 0,
                0, 0, 0, 0
            ],
            CreateCodeSection(codeWords),
            [
                0x47564152, 0x30, 0, 0,
                1, 8, 0, 0,
                1, 42, 0, 0
            ],
            [
                0x53545247, 0x30, 0, 0,
                1, 0, 0, 0,
                0x68690000, 0, 0, 0
            ],
            [
                0x56454354, 0x20, 0, 0,
                0, 12, 0, 0
            ],
            [
                0x47495249, 0x20, 0, 0,
                0, 0, 0, 0
            ],
            [
                0x41525259, 0x20, 0, 0,
                0, 0, 0, 0
            ]
        ];

        var totalWords = 4 + sections.Sum(section => section.Length);
        var bytes = new byte[totalWords * 4];
        BigEndian.WriteUInt32(bytes, 0, 0x54434f44);
        BigEndian.WriteUInt32(bytes, 4, checked((uint)bytes.Length));
        var offset = 0x10;
        foreach (var section in sections)
        {
            foreach (var word in section)
            {
                BigEndian.WriteUInt32(bytes, offset, word);
                offset += 4;
            }
        }

        return bytes;
    }

    private static uint[] CreateCodeSection(params uint[] codeWords)
    {
        var sectionWordCount = (0x20 + (codeWords.Length * 4) + 0x0f) / 4 & ~0x03;
        var words = new uint[sectionWordCount];
        words[0] = 0x434f4445;
        words[1] = checked((uint)(sectionWordCount * 4));
        words[4] = 1;
        words[5] = checked((uint)codeWords.Length);
        codeWords.CopyTo(words.AsSpan(8));
        return words;
    }
}
