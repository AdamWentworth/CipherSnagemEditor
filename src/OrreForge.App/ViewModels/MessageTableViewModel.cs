using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed class MessageTableViewModel
{
    public MessageTableViewModel(ColosseumMessageTable table)
    {
        Table = table;
    }

    public ColosseumMessageTable Table { get; private set; }

    public string Label => $"{Table.DisplayName} ({Table.Strings.Count})";

    public void ReplaceTable(ColosseumMessageTable table)
    {
        Table = table;
    }

    public override string ToString() => Label;
}
