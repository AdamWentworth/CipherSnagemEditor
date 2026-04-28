using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class MessageTableViewModel : ObservableObject
{
    public MessageTableViewModel(ColosseumMessageTable table)
    {
        Table = table;
    }

    public ColosseumMessageTable Table { get; }

    public string Label => $"{Table.DisplayName} ({Table.Strings.Count})";

    public override string ToString() => Label;
}
