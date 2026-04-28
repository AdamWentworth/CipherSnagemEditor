using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TreasureEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#F2B400");
    private static readonly IBrush DefaultBackgroundBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly IBrush PhenacBackgroundBrush = SolidColorBrush.Parse("#B7EEFF");
    private static readonly IBrush PyriteBackgroundBrush = SolidColorBrush.Parse("#FF7656");
    private static readonly IBrush AgateBackgroundBrush = SolidColorBrush.Parse("#5DEB5D");
    private static readonly IBrush UnderBackgroundBrush = SolidColorBrush.Parse("#BE74FF");
    private static readonly IBrush LabBackgroundBrush = SolidColorBrush.Parse("#A4A4A4");
    private static readonly IBrush MtBattleBackgroundBrush = SolidColorBrush.Parse("#FFD05D");
    private static readonly IBrush RealgamBackgroundBrush = SolidColorBrush.Parse("#CFCFCF");
    private static readonly IBrush SnagemBackgroundBrush = SolidColorBrush.Parse("#89B9FF");

    public TreasureEntryViewModel(ColosseumTreasure treasure)
    {
        Treasure = treasure;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumTreasure Treasure { get; }

    public string RowText => $"{Treasure.Index} - {Treasure.RoomName}";

    public string DetailText => ColosseumRoomCatalog.MapNameForRoom(Treasure.RoomName);

    public string SearchText => $"{Treasure.Index} {Treasure.ItemName} {Treasure.RoomName} {Treasure.ModelName} {Treasure.Flag}";

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBackgroundBrush
        : ColosseumRoomCatalog.Prefix(Treasure.RoomName) switch
        {
            "M1" => PhenacBackgroundBrush,
            "M2" => PyriteBackgroundBrush,
            "M3" => AgateBackgroundBrush,
            "M4" => UnderBackgroundBrush,
            "D1" => LabBackgroundBrush,
            "D2" => MtBattleBackgroundBrush,
            "D4" => RealgamBackgroundBrush,
            "S2" => SnagemBackgroundBrush,
            _ => DefaultBackgroundBrush
        };

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
