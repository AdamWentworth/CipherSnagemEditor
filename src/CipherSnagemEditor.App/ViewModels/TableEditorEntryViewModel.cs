using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TableEditorEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");

    public TableEditorEntryViewModel(
        string name,
        string category,
        string searchText,
        string details,
        string backgroundColour)
    {
        Name = name;
        Category = category;
        SearchText = searchText;
        Details = details;
        BackgroundBrush = SolidColorBrush.Parse(backgroundColour);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public string Name { get; }

    public string Category { get; }

    public string SearchText { get; }

    public string Details { get; }

    public IBrush BackgroundBrush { get; }

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
