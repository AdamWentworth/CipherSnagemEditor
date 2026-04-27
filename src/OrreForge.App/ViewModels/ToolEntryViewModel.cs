using OrreForge.Colosseum;

namespace OrreForge.App.ViewModels;

public sealed class ToolEntryViewModel
{
    public ToolEntryViewModel(int index, ColosseumToolDefinition definition)
    {
        Index = index;
        Title = definition.Title;
        LegacySegue = definition.LegacySegue;
        LegacySource = definition.LegacySource;
    }

    public int Index { get; }

    public string Title { get; }

    public string LegacySegue { get; }

    public string LegacySource { get; }
}
