using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class CollisionFileEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#F2B400");
    private static readonly IBrush DefaultBackgroundBrush = SolidColorBrush.Parse("#FFFFFF");

    public CollisionFileEntryViewModel(ColosseumCollisionFile file)
    {
        File = file;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumCollisionFile File { get; }

    public string FileName => File.FileName;

    public string MapName => string.IsNullOrWhiteSpace(File.MapName) ? string.Empty : File.MapName;

    public string RowText => string.IsNullOrWhiteSpace(MapName) ? FileName : $"{FileName}\n{MapName}";

    public string SearchText => $"{File.FileName} {File.MapCode} {File.MapName}";

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBackgroundBrush
        : File.MapCode switch
        {
            "M1" => SolidColorBrush.Parse("#B7EEFF"),
            "M2" => SolidColorBrush.Parse("#FC6848"),
            "M3" => SolidColorBrush.Parse("#5DEB5D"),
            "M4" => SolidColorBrush.Parse("#8A8A8A"),
            "M5" => SolidColorBrush.Parse("#33538A"),
            "M6" => SolidColorBrush.Parse("#89B9FF"),
            "D1" => SolidColorBrush.Parse("#F2B6D2"),
            "D2" => SolidColorBrush.Parse("#FC6848"),
            "D3" => SolidColorBrush.Parse("#FFD080"),
            "D4" => SolidColorBrush.Parse("#C5F08A"),
            "D5" => SolidColorBrush.Parse("#D6B3FF"),
            "D6" => SolidColorBrush.Parse("#A77AF4"),
            "D7" => SolidColorBrush.Parse("#8A5A2C"),
            "S1" => SolidColorBrush.Parse("#FFD080"),
            "S2" => SolidColorBrush.Parse("#FC6848"),
            "S3" => SolidColorBrush.Parse("#CFCFCF"),
            "es" => SolidColorBrush.Parse("#F8F888"),
            _ => DefaultBackgroundBrush
        };

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
