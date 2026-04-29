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
}
