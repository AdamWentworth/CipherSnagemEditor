using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.Tests;

public sealed class GameStringTableTests
{
    [Fact]
    public void RebuildsTableWithReplacementAndSpecialCharacters()
    {
        var original = new byte[]
        {
            0, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 7,
            0, 0, 0, 24,
            0, (byte)'O',
            0, (byte)'l',
            0, (byte)'d',
            0, 0
        };

        var rebuilt = GameStringTable.Parse(original)
            .WithString(7, "New[New Line]Text")
            .WithString(9, "Added")
            .ToArray();
        var reparsed = GameStringTable.Parse(rebuilt);

        Assert.Equal("New[New Line]Text", reparsed.StringWithId(7));
        Assert.Equal("Added", reparsed.StringWithId(9));
    }

    [Fact]
    public void PreservesSwiftSpecialCharacterExtraBytes()
    {
        var original = CreateOneStringTable(7,
        [
            0, (byte)'A',
            0xff, 0xff, 0x09, 0x1e,
            0, (byte)'B',
            0xff, 0xff, 0x08, 1, 2, 3, 4,
            0xff, 0xff, 0x4e,
            0, 0
        ]);

        var table = GameStringTable.Parse(original);

        Assert.Equal("A[Pause]{1e}B[Spec Colour]{01020304}[Pokemon 78]", table.StringWithId(7));
        Assert.Equal(original, table.ToArray());
    }

    [Fact]
    public void EncodesSwiftSpecialCharacterExtraByteSyntax()
    {
        var table = GameStringTable.FromStrings(
        [
            new GameString(7, "[Bold]{02}[Predef Colour]{03}Green[Predef Colour]{00}", 0)
        ]);

        var bytes = table.ToArray();

        Assert.Equal(
        [
            0xff, 0xff, 0x07, 0x02,
            0xff, 0xff, 0x38, 0x03,
            0, (byte)'G',
            0, (byte)'r',
            0, (byte)'e',
            0, (byte)'e',
            0, (byte)'n',
            0xff, 0xff, 0x38, 0x00,
            0, 0
        ], bytes[0x18..]);
    }

    [Fact]
    public void PreservesParsedTableLengthWhenReplacementFits()
    {
        var original = CreateOneStringTable(7,
        [
            0, (byte)'O',
            0, (byte)'l',
            0, (byte)'d',
            0, 0,
            0, 0,
            0, 0,
            0, 0
        ]);

        var rebuilt = GameStringTable.Parse(original)
            .WithString(7, "Newer")
            .ToArray(allowGrowth: false);

        Assert.Equal(original.Length, rebuilt.Length);
        Assert.Equal("Newer", GameStringTable.Parse(rebuilt).StringWithId(7));
        Assert.Equal([0, 0, 0, 0], rebuilt[^4..]);
    }

    [Fact]
    public void RefusesToGrowParsedTableWhenGrowthIsDisabled()
    {
        var original = CreateOneStringTable(7,
        [
            0, (byte)'O',
            0, (byte)'l',
            0, (byte)'d',
            0, 0
        ]);

        var table = GameStringTable.Parse(original).WithString(7, "This is much longer");

        Assert.Throws<InvalidDataException>(() => table.ToArray(allowGrowth: false));
        Assert.True(table.ToArray().Length > original.Length);
    }

    [Fact]
    public void AppliesJsonStylePartialUpdatesWithoutDroppingOtherStrings()
    {
        var original = GameStringTable.FromStrings(
        [
            new GameString(7, "Old", 0),
            new GameString(9, "Keep", 0)
        ]).ToArray();

        var rebuilt = GameStringTable.Parse(original)
            .WithStrings([new GameString(7, "New", 0)])
            .ToArray();
        var reparsed = GameStringTable.Parse(rebuilt);

        Assert.Equal("New", reparsed.StringWithId(7));
        Assert.Equal("Keep", reparsed.StringWithId(9));
    }

    private static byte[] CreateOneStringTable(int id, byte[] textBytes)
    {
        var bytes = new byte[0x18 + textBytes.Length];
        bytes[0x04] = 0;
        bytes[0x05] = 1;
        bytes[0x13] = checked((byte)id);
        bytes[0x17] = 0x18;
        textBytes.CopyTo(bytes.AsSpan(0x18));
        return bytes;
    }
}
