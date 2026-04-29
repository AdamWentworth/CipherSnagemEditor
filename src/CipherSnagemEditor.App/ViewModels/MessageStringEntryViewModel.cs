using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class MessageStringEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#89B9FF");
    private static readonly IBrush DefaultBackgroundBrush = SolidColorBrush.Parse("#FFFFFF");

    public MessageStringEntryViewModel(ColosseumMessageString message)
    {
        Message = message;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    private bool _isSelected;

    public ColosseumMessageString Message { get; }

    public string IdText => Message.Id.ToString();

    public string IdHexText => Message.IdHex;

    public string Preview => string.IsNullOrWhiteSpace(Message.Text) ? "-" : Message.Text.Replace("\n", " ");

    public string RowText => $"{IdHexText}: {Preview}";

    public string SearchText => Message.Text;

    public IBrush BackgroundBrush => IsSelected ? SelectedBackgroundBrush : DefaultBackgroundBrush;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
