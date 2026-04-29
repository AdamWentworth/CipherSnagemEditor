using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class VertexFilterFileEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#F2B400");
    private static readonly IBrush DefaultBackgroundBrush = SolidColorBrush.Parse("#FFFFFF");

    public VertexFilterFileEntryViewModel(ColosseumVertexFilterFile file)
    {
        File = file;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumVertexFilterFile File { get; }

    public string FileName => File.FileName;

    public string RowText => File.RelativePath;

    public string SearchText => $"{File.FileName} {File.RelativePath}";

    public IBrush BackgroundBrush => IsSelected ? SelectedBackgroundBrush : DefaultBackgroundBrush;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
