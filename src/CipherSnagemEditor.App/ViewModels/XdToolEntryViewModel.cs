using Avalonia;
using Avalonia.Media;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdToolEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");

    public XdToolEntryViewModel(int index, string sectionTitle, XdToolRow row)
    {
        Index = index;
        SectionTitle = sectionTitle;
        Name = row.Name;
        Value = row.Value;
        Detail = row.Detail;
        SearchText = $"{sectionTitle} {row.Name} {row.Value} {row.Detail}";
        BackgroundBrush = BrushForSection(sectionTitle, index);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public int Index { get; }

    public string SectionTitle { get; }

    public string Name { get; }

    public string Value { get; }

    public string Detail { get; }

    public string SearchText { get; }

    public string RowText => string.IsNullOrWhiteSpace(Value)
        ? Name
        : $"{Name}\n{Value}";

    public string DetailReadout => string.IsNullOrWhiteSpace(Detail) ? "-" : Detail;

    public IBrush BackgroundBrush { get; }

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private static IBrush BrushForSection(string sectionTitle, int index)
    {
        if (sectionTitle.Contains("Shadow", StringComparison.OrdinalIgnoreCase))
        {
            return SolidColorBrush.Parse("#A77AF4");
        }

        if (sectionTitle.Contains("Trainer", StringComparison.OrdinalIgnoreCase))
        {
            return SolidColorBrush.Parse("#86B8F6");
        }

        if (sectionTitle.Contains("Pokespot", StringComparison.OrdinalIgnoreCase))
        {
            return SolidColorBrush.Parse("#70D874");
        }

        if (sectionTitle.Contains("common.rel", StringComparison.OrdinalIgnoreCase)
            || sectionTitle.Contains("Table", StringComparison.OrdinalIgnoreCase))
        {
            return SolidColorBrush.Parse("#D8D8D8");
        }

        if (sectionTitle.Contains("deck", StringComparison.OrdinalIgnoreCase)
            || sectionTitle.Contains("Archive", StringComparison.OrdinalIgnoreCase))
        {
            return index % 2 == 0
                ? SolidColorBrush.Parse("#FFBE00")
                : SolidColorBrush.Parse("#FF7655");
        }

        return index % 2 == 0
            ? SolidColorBrush.Parse("#86B8F6")
            : SolidColorBrush.Parse("#A77AF4");
    }
}
