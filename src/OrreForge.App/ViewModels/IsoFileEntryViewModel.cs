using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OrreForge.Core.Files;
using OrreForge.Core.GameCube;

namespace OrreForge.App.ViewModels;

public sealed partial class IsoFileEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectedTextBrush = SolidColorBrush.Parse("#80ACFF");
    private static readonly IBrush NormalTextBrush = SolidColorBrush.Parse("#000000");
    private static readonly IBrush RowBrush = SolidColorBrush.Parse("#FFFFFF");

    public IsoFileEntryViewModel(GameCubeIsoFileEntry entry)
    {
        Entry = entry;
        FileType = GameFileTypes.FromExtension(entry.Name);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextBrush))]
    [NotifyPropertyChangedFor(nameof(Alpha))]
    private bool _isSelected;

    public GameCubeIsoFileEntry Entry { get; }

    public GameFileType FileType { get; }

    public string Name => Entry.Name;

    public string OffsetHex => $"0x{Entry.Offset:x8}";

    public string SizeText => Entry.Size.ToString("N0");

    public string FileSizeText => IsFsys
        ? $"File size: 0x{Entry.Size:x}"
        : $"File size: {Entry.Size}";

    public string FileNameText => $"File name: {Name}";

    public bool IsFsys => FileType == GameFileType.Fsys;

    public IBrush BackgroundBrush => RowBrush;

    public IBrush TextBrush => IsSelected ? SelectedTextBrush : NormalTextBrush;

    public double Alpha => IsSelected ? 1 : 0.75;
}
