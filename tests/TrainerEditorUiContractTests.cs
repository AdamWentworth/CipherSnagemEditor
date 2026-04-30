using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace CipherSnagemEditor.Tests;

public sealed class TrainerEditorUiContractTests
{
    private const string ApprovedTrainerPokemonTemplateSha256 = "9b0c7f1d53e1af092dd6a60b1e859daab9cc48fc8515bb4abdb47ddb5fb75f26";

    [Fact]
    public void TrainerPokemonCardTemplateMatchesApprovedWindowsLayout()
    {
        var document = XDocument.Load(TrainerEditorViewPath);
        var xNamespace = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        var templates = document
            .Descendants()
            .Where(element => element.Name.LocalName == "DataTemplate"
                && element.Attribute(xNamespace + "DataType")?.Value == "vm:TrainerPokemonSlotViewModel")
            .ToArray();

        Assert.Single(templates);

        var canonicalTemplate = Canonicalize(templates.Single());
        Assert.Equal(ApprovedTrainerPokemonTemplateSha256, Sha256(canonicalTemplate));
    }

    [Fact]
    public void TrainerEditorDoesNotUseKnownBrokenLayoutExperiments()
    {
        var xaml = File.ReadAllText(TrainerEditorViewPath);

        Assert.DoesNotContain("<Viewbox", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("LegacyPicker", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<views:TrainerPokemonSlotView", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<views:TrainerPokemonSetSlotView", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<views:TrainerPokemonEmptySlotView", xaml, StringComparison.Ordinal);

        Assert.Contains("UniformGrid Rows=\"2\" Columns=\"3\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinitions=\"64,*,*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinitions=\"42,42,42,42,42,42\"", xaml, StringComparison.Ordinal);
        Assert.Contains("LegacyPokemonComboBox", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TrainerEditorWindowKeepsApprovedMinimumSize()
    {
        var source = File.ReadAllText(MainWindowSourcePath);

        Assert.Contains("\"Trainer Editor\" => new Size(1420, 760)", source, StringComparison.Ordinal);
        Assert.Contains("var minSize = ToolWindowMinSize(tool.Title)", source, StringComparison.Ordinal);
        Assert.Contains("MinWidth = minSize.Width", source, StringComparison.Ordinal);
        Assert.Contains("MinHeight = minSize.Height", source, StringComparison.Ordinal);
        Assert.Contains("title == \"Trainer Editor\"", source, StringComparison.Ordinal);
        Assert.Contains("? size", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TrainerSidebarKeepsVirtualizedRowList()
    {
        var xaml = File.ReadAllText(TrainerEditorViewPath);

        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding Trainers}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ListBox Grid.Row=\"1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Trainers}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedTrainer, Mode=TwoWay}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<VirtualizingStackPanel", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TrainerEditorDefersPokemonCardUiUntilTrainerSelection()
    {
        var source = File.ReadAllText(MainWindowViewModelSourcePath).ReplaceLineEndings("\n");

        Assert.Contains("ApplyTrainerFilter(TrainerSearchText, selectFirstWhenMissing: false)", source, StringComparison.Ordinal);
        Assert.Contains("private TrainerEntryViewModel? _lastSelectedTrainer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var trainer in _allTrainers)\n        {\n            trainer.IsSelected = ReferenceEquals(trainer, value);\n        }", source, StringComparison.Ordinal);
    }

    private static string TrainerEditorViewPath
        => Path.Combine(RepoRoot, "src", "CipherSnagemEditor.App", "Views", "TrainerEditorView.axaml");

    private static string MainWindowSourcePath
        => Path.Combine(RepoRoot, "src", "CipherSnagemEditor.App", "Views", "MainWindow.axaml.cs");

    private static string MainWindowViewModelSourcePath
        => Path.Combine(RepoRoot, "src", "CipherSnagemEditor.App", "ViewModels", "MainWindowViewModel.cs");

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    "src",
                    "CipherSnagemEditor.App",
                    "Views",
                    "TrainerEditorView.axaml");
                if (File.Exists(candidate))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
        }
    }

    private static string Canonicalize(XElement element)
    {
        foreach (var descendant in element.DescendantsAndSelf())
        {
            descendant.ReplaceAttributes(descendant.Attributes().OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal));
        }

        return element.ToString(SaveOptions.DisableFormatting);
    }

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
