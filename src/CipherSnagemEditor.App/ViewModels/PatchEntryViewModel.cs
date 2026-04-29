using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class PatchEntryViewModel : ObservableObject
{
    private static readonly IBrush EvenBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly IBrush OddBrush = SolidColorBrush.Parse("#DADADA");
    private static readonly IBrush SelectedBorderBrush = SolidColorBrush.Parse("#000000");
    private static readonly IBrush TransparentBrush = Brushes.Transparent;

    public PatchEntryViewModel(ColosseumPatchDefinition definition, int row)
    {
        Definition = definition;
        Row = row;
        BackgroundBrush = row % 2 == 0 ? EvenBrush : OddBrush;
    }

    public ColosseumPatchDefinition Definition { get; }

    public int Row { get; }

    public string Name => Definition.Name;

    public IBrush BackgroundBrush { get; }

    public IBrush SelectionBorderBrush => IsSelected ? SelectedBorderBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;
}
