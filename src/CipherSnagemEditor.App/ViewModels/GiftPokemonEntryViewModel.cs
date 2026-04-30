using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.Colosseum.Data;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class GiftPokemonEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private static readonly IBrush SelectedBackgroundBrush = SolidColorBrush.Parse("#F7B409FF");
    private Bitmap? _faceImage;
    private bool _faceImageLoaded;

    public GiftPokemonEntryViewModel(ColosseumGiftPokemon gift, int rowIndex)
    {
        Gift = gift;
        RowIndex = rowIndex;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public ColosseumGiftPokemon Gift { get; }

    public int RowIndex { get; }

    public Bitmap? FaceImage
    {
        get
        {
            if (!_faceImageLoaded)
            {
                _faceImage = RuntimeImageAssets.LoadImage("PokeFace", $"face_{Gift.SpeciesId:000}.png");
                _faceImageLoaded = true;
            }

            return _faceImage;
        }
    }

    public string RowText => $"{Gift.SpeciesName} Lv. {Gift.Level}\n{Gift.GiftType}";

    public string SearchText => $"{Gift.RowId} {Gift.SpeciesName} {Gift.GiftType}";

    public IBrush BackgroundBrush => IsSelected
        ? SelectedBackgroundBrush
        : Gift.RowId == RowIndex && Gift.RowId <= 14
            ? SolidColorBrush.Parse(RowIndex switch
            {
                0 => "#7B5735FF",
                1 or 2 => "#80ACFFFF",
                3 or 4 => "#F6BC00FF",
                5 or 6 => "#20F020FF",
                7 => "#A77AF4FF",
                8 => "#FFC0CBFF",
                9 or 10 or 11 => "#858585FF",
                _ => "#FC6848FF"
            })
        : SolidColorBrush.Parse(RowIndex switch
        {
            0 or 1 => "#80ACFFFF",
            2 or 3 => "#FC6848FF",
            _ => "#A8E79CFF"
        });

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

}
