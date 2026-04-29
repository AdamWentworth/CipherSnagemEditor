using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class TypeEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private Bitmap? _typeImage;
    private bool _typeImageLoaded;

    public TypeEntryViewModel(ColosseumTypeData type)
    {
        Type = type;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumTypeData Type { get; }

    public Bitmap? TypeImage
    {
        get
        {
            if (!_typeImageLoaded)
            {
                _typeImage = RuntimeImageAssets.LoadImage("Types", $"type_{Type.Index}.png");
                _typeImageLoaded = true;
            }

            return _typeImage;
        }
    }

    public string RowText => Type.Name;

    public string SearchText => $"{Type.Index} {Type.Name} {Type.CategoryName}";

    public IBrush BackgroundBrush => SolidColorBrush.Parse("#303030");

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

}
