using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CipherSnagemEditor.App.Services;
using CipherSnagemEditor.XD;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CipherSnagemEditor.App.ViewModels;

public sealed partial class XdPokespotEntryViewModel : ObservableObject
{
    private static readonly IBrush SelectionBrush = Brushes.Black;
    private static readonly IBrush TransparentBrush = SolidColorBrush.Parse("#00000000");
    private Bitmap? _faceImage;
    private Bitmap? _bodyImage;
    private Bitmap? _typeImage;
    private bool _faceLoaded;
    private bool _bodyLoaded;
    private bool _typeLoaded;

    public XdPokespotEntryViewModel(XdPokespotRecord encounter)
    {
        Encounter = encounter;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeImageOpacity))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderBrush))]
    [NotifyPropertyChangedFor(nameof(SelectionBorderThickness))]
    private bool _isSelected;

    public XdPokespotRecord Encounter { get; }

    public Bitmap? FaceImage
    {
        get
        {
            if (!_faceLoaded)
            {
                _faceImage = RuntimeImageAssets.LoadImage("PokeFace", $"face_{Encounter.SpeciesId:000}.png");
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
                _bodyImage = RuntimeImageAssets.LoadBodyFrames(Encounter.SpeciesId).FirstOrDefault()?.Image;
                _bodyLoaded = true;
            }

            return _bodyImage;
        }
    }

    public Bitmap? TypeImage
    {
        get
        {
            if (!_typeLoaded)
            {
                _typeImage = RuntimeImageAssets.LoadImage("Types", TypeImageName(Encounter.SpotName));
                _typeLoaded = true;
            }

            return _typeImage;
        }
    }

    public string RowText => Encounter.SpeciesName;

    public string SearchText => $"{Encounter.SpotName} {Encounter.Index} {Encounter.SpeciesId} {Encounter.SpeciesName}";

    public double TypeImageOpacity => IsSelected ? 1.0 : 0.75;

    public IBrush FallbackBrush => SolidColorBrush.Parse(Encounter.SpotName switch
    {
        "Rock" => "#C8B070",
        "Oasis" => "#80ACFF",
        "Cave" => "#707080",
        _ => "#D0D0D0"
    });

    public IBrush SelectionBorderBrush => IsSelected ? SelectionBrush : TransparentBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(1) : new Thickness(0);

    private static string TypeImageName(string spot)
        => spot switch
        {
            "Rock" => "type_5.png",
            "Oasis" => "type_11.png",
            "Cave" => "type_17.png",
            _ => "type_0.png"
        };
}
