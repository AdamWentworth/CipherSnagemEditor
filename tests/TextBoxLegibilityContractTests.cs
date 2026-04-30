namespace CipherSnagemEditor.Tests;

public sealed class TextBoxLegibilityContractTests
{
    [Fact]
    public void TextBoxTemplateKeepsControlBackgroundDuringFocusStates()
    {
        var xaml = File.ReadAllText(AppXamlPath);

        Assert.Contains("TextBox /template/ Border#PART_BorderElement", xaml, StringComparison.Ordinal);
        Assert.Contains("TextBox:pointerover /template/ Border#PART_BorderElement", xaml, StringComparison.Ordinal);
        Assert.Contains("TextBox:focus /template/ Border#PART_BorderElement", xaml, StringComparison.Ordinal);
        Assert.Contains("TextBox:focus-within /template/ Border#PART_BorderElement", xaml, StringComparison.Ordinal);
        Assert.Equal(9, CountOccurrences(xaml, "RelativeSource={RelativeSource TemplatedParent}}"));
    }

    private static string AppXamlPath
        => Path.Combine(RepoRoot, "src", "CipherSnagemEditor.App", "App.axaml");

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "CipherSnagemEditor.App", "App.axaml");
                if (File.Exists(candidate))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
        }
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }
}
