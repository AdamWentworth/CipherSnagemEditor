using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdShadowPokemonEntryViewModel : ObservableObject
{
    private static readonly IBrush ShadowBrush = SolidColorBrush.Parse("#A77AF4");
    private static readonly IBrush SelectedBrush = SolidColorBrush.Parse("#20F020");
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private Bitmap? _faceImage;
    private Bitmap? _bodyImage;
    private bool _faceLoaded;
    private bool _bodyLoaded;

    public XdShadowPokemonEntryViewModel(XdShadowPokemonRecord shadow)
    {
        Shadow = shadow;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public XdShadowPokemonRecord Shadow { get; }

    public Bitmap? FaceImage
    {
        get
        {
            if (!_faceLoaded)
            {
                _faceImage = RuntimeImageAssets.LoadImage("PokeFace", $"face_{Shadow.SpeciesId:000}.png");
                _faceLoaded = true;
            }

            return _faceImage;
        }
    }

    public Bitmap? BodyImage
    {
        get
        {
            if (!_bodyLoaded)
            {
                _bodyImage = RuntimeImageAssets.LoadBodyFrames(Shadow.SpeciesId).FirstOrDefault()?.Image;
                _bodyLoaded = true;
            }

            return _bodyImage;
        }
    }

    public string RowText => $"{Shadow.Index}: Shadow {Shadow.SpeciesName}{Environment.NewLine}Lv. {Shadow.Level}+";

    public string SearchText => $"{Shadow.Index} {Shadow.SpeciesId} {Shadow.SpeciesName} {Shadow.StoryPokemonIndex} {string.Join(' ', Shadow.MoveNames)}";

    public string MovesText => string.Join(Environment.NewLine, Shadow.MoveNames.Where(move => move != "Move 0"));

    public IBrush BackgroundBrush => IsSelected ? SelectedBrush : ShadowBrush;

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);
}
