using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Colosseum.Data;

namespace OrreForge.App.ViewModels;

public sealed partial class TreasureEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");

    public TreasureEntryViewModel(ColosseumTreasure treasure)
    {
        Treasure = treasure;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumTreasure Treasure { get; }

    public string RowText => $"{Treasure.Index}: {Treasure.ItemName}";

    public string DetailText => Treasure.RoomName;

    public string SearchText => $"{Treasure.Index} {Treasure.ItemName} {Treasure.RoomName} {Treasure.ModelName} {Treasure.Flag}";

    public IBrush BackgroundBrush => Treasure.Index % 2 == 0
        ? SolidColorBrush.Parse("#FFFFFF")
        : SolidColorBrush.Parse("#DADADA");

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
