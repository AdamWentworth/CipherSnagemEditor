using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CipherSnagemEditor.Colosseum;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class ToolEntryViewModel : ObservableObject
{
    private static readonly IBrush ItemCellFallback = SolidColorBrush.Parse("#eadde5");
    private static readonly IBrush ToolCellFallback = SolidColorBrush.Parse("#e7c995");
    private static readonly IBrush SelectedCellFallback = SolidColorBrush.Parse("#b8d9f0");

    private static readonly IImage? ItemCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/LegacyCells/ItemCell.png");
    private static readonly IImage? ToolCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/LegacyCells/ToolCell.png");
    private static readonly IImage? SelectedCellImage = LoadImage("avares://CipherSnagemEditor.App/Assets/LegacyCells/SelectedCell.png");

    public ToolEntryViewModel(int index, ColosseumToolDefinition definition)
    {
        Index = index;
        Title = definition.Title;
        LegacySegue = definition.LegacySegue;
        LegacySource = definition.LegacySource;
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
