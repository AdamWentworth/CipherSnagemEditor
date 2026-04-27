using OrreForge.Colosseum;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrreForge.App.ViewModels;

public sealed partial class ToolEntryViewModel : ObservableObject
{
    public ToolEntryViewModel(int index, ColosseumToolDefinition definition)
    {
        Index = index;
        Title = definition.Title;
        LegacySegue = definition.LegacySegue;
        LegacySource = definition.LegacySource;
        BackgroundAsset = index % 2 == 1
            ? "/Assets/LegacyCells/ItemCell.png"
            : "/Assets/LegacyCells/ToolCell.png";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentBackgroundAsset))]
    private bool _isSelected;

    public int Index { get; }

    public string Title { get; }

    public string LegacySegue { get; }

    public string LegacySource { get; }

    public string BackgroundAsset { get; }

    public string CurrentBackgroundAsset => IsSelected
        ? "/Assets/LegacyCells/SelectedCell.png"
        : BackgroundAsset;
}
