using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class IsoFileEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectedTextBrush = SolidColorBrush.Parse("#80ACFF");
    private static readonly IBrush NormalTextBrush = SolidColorBrush.Parse("#000000");
    private static readonly IBrush RowBrush = SolidColorBrush.Parse("#FFFFFF");
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");

    public IsoFileEntryViewModel(GameCubeIsoFileEntry entry)
    {
        Entry = entry;
        FileType = GameFileTypes.FromExtension(entry.Name);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextBrush))]
    [NotifyPropertyChangedFor(nameof(Alpha))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
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

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
