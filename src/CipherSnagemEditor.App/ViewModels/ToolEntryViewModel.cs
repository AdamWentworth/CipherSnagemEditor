using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class ToolEntryViewModel : ObservableObject
{
    private static readonly IBrush ItemCellFallback = SolidColorBrush.Parse("#eadde5");
    private static readonly IBrush ToolCellFallback = SolidColorBrush.Parse("#e7c995");
    private static readonly IBrush SelectedCellFallback = SolidColorBrush.Parse("#b8d9f0");

    private static readonly IImage? ItemCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/Ui/Cells/item-cell.png");
    private static readonly IImage? ToolCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/Ui/Cells/tool-cell.png");
    private static readonly IImage? SelectedCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/Ui/Cells/selected-cell.png");

    public ToolEntryViewModel(int index, ColosseumToolDefinition definition)
        : this(index, definition.Title, definition.LegacySegue, definition.LegacySource, GameCubeGame.PokemonColosseum)
    {
    }

    public ToolEntryViewModel(int index, XdToolDefinition definition)
        : this(index, definition.Title, definition.LegacySegue, definition.LegacySource, GameCubeGame.PokemonXD)
    {
    }

    private ToolEntryViewModel(
        int index,
        string title,
        string legacySegue,
        string legacySource,
        GameCubeGame game)
    {
        Index = index;
        Title = title;
        LegacySegue = legacySegue;
        LegacySource = legacySource;
        Game = game;
        BackgroundImage = index % 2 == 0 ? ItemCellImage : ToolCellImage;
        FallbackBrush = index % 2 == 0 ? ItemCellFallback : ToolCellFallback;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentBackgroundImage))]
    [NotifyPropertyChangedFor(nameof(CurrentFallbackBrush))]
    private bool _isSelected;

    public int Index { get; }

    public string Title { get; }

    public string LegacySegue { get; }

    public string LegacySource { get; }

    public GameCubeGame Game { get; }

    public string LegacyToolName => Game switch
    {
        GameCubeGame.PokemonXD => "GoD Tool",
        _ => "Colosseum Tool"
    };

    public IImage? BackgroundImage { get; }

    public IBrush FallbackBrush { get; }

    public IImage? CurrentBackgroundImage => IsSelected ? SelectedCellImage : BackgroundImage;

    public IBrush CurrentFallbackBrush => IsSelected ? SelectedCellFallback : FallbackBrush;

    private static IImage? LoadImage(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
