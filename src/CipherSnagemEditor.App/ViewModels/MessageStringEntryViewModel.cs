using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class MessageStringEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");

    public MessageStringEntryViewModel(ColosseumMessageString message)
    {
        Message = message;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumMessageString Message { get; }

    public string IdText => Message.Id.ToString();

    public string IdHexText => Message.IdHex;

    public string Preview => string.IsNullOrWhiteSpace(Message.Text) ? "-" : Message.Text.Replace("\n", " ");

    public string SearchText => $"{Message.Id} {Message.IdHex} {Message.Text}";

    public IBrush BackgroundBrush => Message.Id % 2 == 0
        ? SolidColorBrush.Parse("#FFFFFF")
        : SolidColorBrush.Parse("#DADADA");

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
