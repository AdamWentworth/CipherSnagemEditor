using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class InteractionEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#F7B409FF");
    private static readonly IBrush DefaultBackgroundBrush = SolidColorBrush.Parse("#FFFFFFFF");
    private static readonly IBrush RedBackgroundBrush = SolidColorBrush.Parse("#FC6848FF");
    private static readonly IBrush OrangeBackgroundBrush = SolidColorBrush.Parse("#F7B409FF");
    private static readonly IBrush YellowBackgroundBrush = SolidColorBrush.Parse("#F8F888FF");
    private static readonly IBrush GreenBackgroundBrush = SolidColorBrush.Parse("#A8E79CFF");
    private static readonly IBrush LightGreenBackgroundBrush = SolidColorBrush.Parse("#D0FFD0FF");
    private static readonly IBrush BlueBackgroundBrush = SolidColorBrush.Parse("#80ACFFFF");
    private static readonly IBrush LightBlueBackgroundBrush = SolidColorBrush.Parse("#B8F0FFFF");
    private static readonly IBrush LightPurpleBackgroundBrush = SolidColorBrush.Parse("#E0C0FFFF");
    private static readonly IBrush PurpleBackgroundBrush = SolidColorBrush.Parse("#A070FFFF");
    private static readonly IBrush BabyPinkBackgroundBrush = SolidColorBrush.Parse("#FFE0E8FF");
    private static readonly IBrush NavyBackgroundBrush = SolidColorBrush.Parse("#28276BFF");
    private static readonly IBrush BrownBackgroundBrush = SolidColorBrush.Parse("#C0A078FF");
    private static readonly IBrush GreyBackgroundBrush = SolidColorBrush.Parse("#C0C0C8FF");
    private static readonly IBrush LightGreyBackgroundBrush = SolidColorBrush.Parse("#F0F0FCFF");
    private static readonly IBrush LightOrangeBackgroundBrush = SolidColorBrush.Parse("#FFD080FF");

    public InteractionEntryViewModel(ColosseumInteractionPoint interaction)
    {
        Interaction = interaction;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumInteractionPoint Interaction { get; }

    public string RowText => $"{Interaction.Index} - {Interaction.RoomName}";

    public string DetailText => ColosseumRoomCatalog.MapNameForRoom(Interaction.RoomName);

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBackgroundBrush
        : ColosseumRoomCatalog.Prefix(Interaction.RoomName) switch
        {
            "D1" => BabyPinkBackgroundBrush,
            "D2" => RedBackgroundBrush,
            "D3" => LightOrangeBackgroundBrush,
            "D4" => LightGreenBackgroundBrush,
            "D5" => LightPurpleBackgroundBrush,
            "D6" => PurpleBackgroundBrush,
            "D7" => BrownBackgroundBrush,
            "M1" => LightBlueBackgroundBrush,
            "M2" => RedBackgroundBrush,
            "M3" => GreenBackgroundBrush,
            "M4" => GreyBackgroundBrush,
            "M5" => NavyBackgroundBrush,
            "M6" => BlueBackgroundBrush,
            "S1" => OrangeBackgroundBrush,
            "S2" => RedBackgroundBrush,
            "S3" => LightGreyBackgroundBrush,
            "es" => YellowBackgroundBrush,
            "T1" => DefaultBackgroundBrush,
            _ => DefaultBackgroundBrush
        };

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
