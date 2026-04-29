using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class MoveEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = SolidColorBrush.Parse("#000000");
    private static readonly IBrush TransparentSelectionBrush = SolidColorBrush.Parse("#00000000");
    private Bitmap? _typeImage;
    private bool _typeImageLoaded;

    public MoveEntryViewModel(ColosseumMove move)
    {
        Move = move;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumMove Move { get; }

    public Bitmap? TypeImage
    {
        get
        {
            if (!_typeImageLoaded)
            {
                var fileName = Move.IsShadow ? "type_shadow.png" : $"type_{Move.TypeId}.png";
                _typeImage = RuntimeImageAssets.LoadImage("Types", fileName);
                _typeImageLoaded = true;
            }

            return _typeImage;
        }
    }

    public string RowText => Move.Name;

    public string SearchText => $"{Move.Index} {Move.Name} {Move.TypeName} {Move.CategoryName} {Move.EffectName}";

    public IBrush BackgroundBrush => TypeFallbackBrush(Move.IsShadow ? 18 : Move.TypeId);

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentSelectionBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private static IBrush TypeFallbackBrush(int typeId)
        => SolidColorBrush.Parse(typeId switch
        {
            0 => "#D0D0D0",
            1 => "#E0A060",
            2 => "#B8D8FF",
            3 => "#E080F8",
            4 => "#E8C878",
            5 => "#C8B070",
            6 => "#A8E868",
            7 => "#A070FF",
            8 => "#B0B0C0",
            9 => "#C0C0C8",
            10 => "#FC6848",
            11 => "#80ACFF",
            12 => "#20F020",
            13 => "#F8F888",
            14 => "#FF90C8",
            15 => "#B8F0FF",
            16 => "#B090F8",
            17 => "#707080",
            18 => "#A77AF4",
            _ => "#80ACFF"
        });

}
